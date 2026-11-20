using Sirenix.OdinInspector;
using UnityEngine;

public class HumanoidEntityIK : MonoBehaviour
{
	[System.Serializable]
	public class BodyPartsTransforms : SerializableDictionary<CharacterBodyPart, Transform> { };

	public enum CharacterBodyPart
	{
		RightHand,
		LeftHand,
		Spine,
		RightFoot,
		LeftFoot,
		ArmStack,
	}

	public const string LeftGripLayerName = "LeftHandGrip";
	public const string RightGripLayerName = "RightHandGrip";
	private const float c_gripSpeed = 8f;
	private const float c_aimBlendSpeed = 6f;
	private const float c_recoilImpulse = 1.75f;
	private const float c_recoilStep = 1f / 120f;

	public BodyPartsTransforms bodypartsTransform = new BodyPartsTransforms();
	public Animator animator;

	public Transform handGrabSocket;
	public Transform offHandGrabSocket;

	[Title("Shot Recoil")]
	[SerializeField] private float m_recoilBodyPitch = 6f;
	[SerializeField] private float m_recoilBodyTwist = 4f;
	[SerializeField] private float m_recoilArmLift = 10f;
	[SerializeField, Min(.5f)] private float m_recoilFrequency = 4.5f;
	[SerializeField, Range(.05f, 1f)] private float m_recoilDamping = .45f;
	[SerializeField, Min(0f)] private float m_maxRecoil = 3f;

	public float GripAmount { get; set; } = 1f;

	private Quaternion m_defaultHandPivotRotation;
	private Quaternion m_defaultOffHandPivotRotation;

	private Pose m_renderedMainSocket;
	private Pose m_renderedOffSocket;
	private bool m_hasRenderedSockets;

	private Transform m_aimingSocket;
	private Transform m_aimingHeld;
	private AvatarIKGoal m_aimGoal = AvatarIKGoal.RightHand;
	private Vector3 m_aimTarget;
	private float m_aimWeight;
	private bool m_isAiming;

	private float m_recoil;
	private float m_recoilSpeed;
	private float m_recoilSide;
	private Transform m_recoilArm;
	private Transform m_recoilChest;
	private readonly RecoilPose m_chestPose = new();
	private readonly RecoilPose m_armPose = new();

	private int m_leftGripLayer = -2;
	private int m_rightGripLayer = -2;
	private float m_leftGrip;
	private float m_rightGrip;

	private class RecoilPose
	{
		public Transform bone;
		public Quaternion animated;
		public Quaternion written;
	}

	private void Awake ()
	{
		if (offHandGrabSocket == null)
			CreateOffHandSocket();

		m_defaultHandPivotRotation = handGrabSocket.localRotation;
		if (offHandGrabSocket != null)
			m_defaultOffHandPivotRotation = offHandGrabSocket.localRotation;
	}

	private void Reset ()
	{
		SetupBodyparts();
		CreateHandSocket();
	}

	private void Update ()
	{
		UpdateGrip(Time.deltaTime);
		UpdateAimWeight(Time.deltaTime);
	}

	private void LateUpdate ()
	{
		AimSocket();
		UpdateRecoil(Time.deltaTime);
		CaptureRenderedSockets();
	}

	private void OnDisable ()
	{
		m_hasRenderedSockets = false;
	}

	private void OnAnimatorIK ( int _layerIndex )
	{
		SetHandGoal(AvatarIKGoal.RightHand);
		SetHandGoal(AvatarIKGoal.LeftHand);
	}

	#region Sockets

	public Transform GetTransform ( CharacterBodyPart _characterBodyPart )
	{
		return bodypartsTransform.TryGetValue(_characterBodyPart, out Transform part) ? part : null;
	}

	public Transform GetGrabSocket ( int _armIndex )
	{
		return _armIndex % 2 == 1 && offHandGrabSocket != null ? offHandGrabSocket : handGrabSocket;
	}

	public bool IsHeldInLeftHand ( Transform _item )
	{
		return GetIKGoalOf(_item) == AvatarIKGoal.LeftHand;
	}

	public Pose RenderedPoseOf ( Transform _point )
	{
		Transform socket = m_hasRenderedSockets ? HoldingSocketOf(_point) : null;
		if (socket == null)
			return new Pose(_point.position, _point.rotation);

		Pose rendered = socket == offHandGrabSocket ? m_renderedOffSocket : m_renderedMainSocket;
		Quaternion toRendered = rendered.rotation * Quaternion.Inverse(socket.rotation);
		return new Pose(rendered.position + toRendered * (_point.position - socket.position), toRendered * _point.rotation);
	}

	private Transform HoldingSocketOf ( Transform _point )
	{
		for (Transform current = _point; current != null; current = current.parent)
		{
			if (current == handGrabSocket || current == offHandGrabSocket)
				return current;
		}

		return null;
	}

	private void CaptureRenderedSockets ()
	{
		if (handGrabSocket != null)
			m_renderedMainSocket = new Pose(handGrabSocket.position, handGrabSocket.rotation);
		if (offHandGrabSocket != null)
			m_renderedOffSocket = new Pose(offHandGrabSocket.position, offHandGrabSocket.rotation);
		m_hasRenderedSockets = true;
	}

	public void FitInHand ( Transform _held )
	{
		if (_held == null || offHandGrabSocket == null || offHandGrabSocket == handGrabSocket || _held.parent != offHandGrabSocket)
			return;

		Vector3 position = _held.localPosition;
		Quaternion rotation = _held.localRotation;
		MirrorPose(ref position, ref rotation);
		_held.SetLocalPositionAndRotation(position, rotation);
	}

	public static void MirrorPose ( ref Vector3 _position, ref Quaternion _rotation )
	{
		_position = new Vector3(-_position.x, _position.y, _position.z);
		_rotation = new Quaternion(_rotation.x, -_rotation.y, -_rotation.z, _rotation.w);
	}

	private AvatarIKGoal GetIKGoalOf ( Transform _socket )
	{
		Transform leftHand = GetTransform(CharacterBodyPart.LeftHand);
		return _socket != null && leftHand != null && _socket.IsChildOf(leftHand) ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
	}

	[Button, HideIf("offHandGrabSocket")]
	public void CreateOffHandSocket ()
	{
		if (offHandGrabSocket != null || handGrabSocket == null || handGrabSocket.parent == null)
			return;

		Transform mainHand = handGrabSocket.parent;
		Transform leftHand = GetTransform(CharacterBodyPart.LeftHand);
		Transform rightHand = GetTransform(CharacterBodyPart.RightHand);
		Transform offHand = mainHand == leftHand ? rightHand : leftHand;
		if (offHand == null || offHand == mainHand)
			return;

		Vector3 mirrorNormal = GetMirrorNormal(mainHand, offHand);
		GameObject socket = new() { name = "OffHandGrabSocket" };
		socket.transform.SetParent(offHand, false);
		socket.transform.localScale = handGrabSocket.localScale;
		socket.transform.position = offHand.position + Vector3.Reflect(handGrabSocket.position - mainHand.position, mirrorNormal);
		socket.transform.rotation = Quaternion.LookRotation(Vector3.Reflect(handGrabSocket.forward, mirrorNormal), Vector3.Reflect(handGrabSocket.up, mirrorNormal));
		offHandGrabSocket = socket.transform;
	}

	private Vector3 GetMirrorNormal ( Transform _mainHand, Transform _offHand )
	{
		Transform leftArm = GetHumanBone(HumanBodyBones.LeftUpperArm);
		Transform rightArm = GetHumanBone(HumanBodyBones.RightUpperArm);
		if (leftArm != null && rightArm != null && (rightArm.position - leftArm.position).sqrMagnitude > .0001f)
			return (rightArm.position - leftArm.position).normalized;

		Vector3 betweenHands = _offHand.position - _mainHand.position;
		return betweenHands.sqrMagnitude > .0001f ? betweenHands.normalized : transform.right;
	}

	[Button, HideIf("handGrabSocket")]
	public void CreateHandSocket ()
	{
		if (handGrabSocket)
			return;

		GameObject socket = new() { name = "GrabSocket" };
		socket.transform.parent = GetTransform(CharacterBodyPart.RightHand);
		socket.transform.localScale = Vector3.one;
		socket.transform.localPosition = Vector3.zero;
		socket.transform.localRotation = Quaternion.Euler(-90, 0, 90);
		handGrabSocket = socket.transform;
	}

	[Button]
	private void SetupBodyparts ()
	{
		animator = GetComponent<Animator>();
		bodypartsTransform.Add(CharacterBodyPart.RightHand, FindObjectContainingName("hand.right", transform));
		bodypartsTransform.Add(CharacterBodyPart.LeftHand, FindObjectContainingName("hand.left", transform));
		bodypartsTransform.Add(CharacterBodyPart.Spine, FindObjectContainingName("spine1", transform));
		bodypartsTransform.Add(CharacterBodyPart.RightFoot, FindObjectContainingName("foot.right", transform));
		bodypartsTransform.Add(CharacterBodyPart.LeftFoot, FindObjectContainingName("foot.left", transform));
	}

	private static Transform FindObjectContainingName ( string _name, Transform _from )
	{
		_name = _name.ToLower();
		foreach (Transform child in _from)
		{
			if (child.name.ToLower().Contains(_name))
				return child;

			Transform found = FindObjectContainingName(_name, child);
			if (found != null)
				return found;
		}

		return null;
	}

	private Transform GetHumanBone ( HumanBodyBones _bone )
	{
		return animator != null && animator.isHuman ? animator.GetBoneTransform(_bone) : null;
	}

	#endregion

	#region Aim

	public void Aim ( Transform _socket, Vector3 _position, Transform _held = null )
	{
		if (!isActiveAndEnabled)
			return;

		Transform socket = _socket != null ? _socket : handGrabSocket;
		if (m_aimingSocket != null && m_aimingSocket != socket)
			ResetSocketRotation(m_aimingSocket);

		m_aimingSocket = socket;
		m_aimingHeld = _held;
		m_aimGoal = GetIKGoalOf(socket);
		m_aimTarget = _position;
		m_isAiming = true;
	}

	public void ReleaseAim ()
	{
		m_isAiming = false;
		if (m_aimWeight > 0f && isActiveAndEnabled)
			return;

		m_aimWeight = 0f;
		FinishRelease();
	}

	private void UpdateAimWeight ( float _deltaTime )
	{
		if (!m_isAiming && m_aimWeight <= 0f)
			return;

		m_aimWeight = Mathf.MoveTowards(m_aimWeight, m_isAiming ? 1f : 0f, c_aimBlendSpeed * _deltaTime);
		if (!m_isAiming && m_aimWeight <= 0f)
			FinishRelease();
	}

	private void SetHandGoal ( AvatarIKGoal _goal )
	{
		bool isAimed = m_aimWeight > 0f && _goal == m_aimGoal;
		animator.SetIKPositionWeight(_goal, isAimed ? m_aimWeight : 0f);
		if (isAimed)
			animator.SetIKPosition(_goal, m_aimTarget);
	}

	private void AimSocket ()
	{
		if (m_aimingSocket == null)
			return;

		Vector3 direction = m_aimTarget - m_aimingSocket.position;
		if (direction.sqrMagnitude < .000001f)
			return;

		Quaternion look = Quaternion.LookRotation(direction);
		Quaternion aimed = m_aimingHeld != null && m_aimingHeld.parent == m_aimingSocket ? look * Quaternion.Inverse(m_aimingHeld.localRotation) : look;
		Quaternion rest = m_aimingSocket.parent != null ? m_aimingSocket.parent.rotation * DefaultRotationOf(m_aimingSocket) : aimed;
		m_aimingSocket.rotation = Quaternion.Slerp(rest, aimed, m_aimWeight);
	}

	private void FinishRelease ()
	{
		m_aimingSocket = null;
		m_aimingHeld = null;
		ResetSocketRotation(handGrabSocket);
		ResetSocketRotation(offHandGrabSocket);
	}

	private Quaternion DefaultRotationOf ( Transform _socket )
	{
		return _socket == offHandGrabSocket ? m_defaultOffHandPivotRotation : m_defaultHandPivotRotation;
	}

	private void ResetSocketRotation ( Transform _socket )
	{
		if (_socket != null)
			_socket.localRotation = DefaultRotationOf(_socket);
	}

	#endregion

	#region Grip

	private void UpdateGrip ( float _deltaTime )
	{
		if (animator == null || animator.runtimeAnimatorController == null)
			return;

		if (m_leftGripLayer == -2)
		{
			m_leftGripLayer = animator.GetLayerIndex(LeftGripLayerName);
			m_rightGripLayer = animator.GetLayerIndex(RightGripLayerName);
		}

		if (m_leftGripLayer < 0 && m_rightGripLayer < 0)
			return;

		bool isLeftHolding = false;
		bool isRightHolding = false;
		MarkHolding(handGrabSocket, ref isLeftHolding, ref isRightHolding);
		if (offHandGrabSocket != handGrabSocket)
			MarkHolding(offHandGrabSocket, ref isLeftHolding, ref isRightHolding);

		float step = c_gripSpeed * _deltaTime;
		m_leftGrip = Mathf.MoveTowards(m_leftGrip, isLeftHolding ? GripAmount : 0f, step);
		m_rightGrip = Mathf.MoveTowards(m_rightGrip, isRightHolding ? GripAmount : 0f, step);
		if (m_leftGripLayer >= 0)
			animator.SetLayerWeight(m_leftGripLayer, m_leftGrip);
		if (m_rightGripLayer >= 0)
			animator.SetLayerWeight(m_rightGripLayer, m_rightGrip);
	}

	private void MarkHolding ( Transform _socket, ref bool _isLeftHolding, ref bool _isRightHolding )
	{
		if (_socket == null || !HasActiveChild(_socket))
			return;

		if (GetIKGoalOf(_socket) == AvatarIKGoal.LeftHand)
			_isLeftHolding = true;
		else
			_isRightHolding = true;
	}

	private static bool HasActiveChild ( Transform _parent )
	{
		for (int i = 0; i < _parent.childCount; i++)
		{
			if (_parent.GetChild(i).gameObject.activeSelf)
				return true;
		}

		return false;
	}

	#endregion

	#region Recoil

	public void Recoil ( float _strength, Transform _weapon, bool _isTwoHanded )
	{
		if (_strength <= 0f || !isActiveAndEnabled)
			return;

		bool isLeftHand = IsHeldInLeftHand(_weapon);
		m_recoilSide = _isTwoHanded ? 0f : isLeftHand ? -1f : 1f;
		m_recoilArm = _isTwoHanded ? null : GetHumanBone(isLeftHand ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
		m_recoilSpeed += _strength * c_recoilImpulse * 2f * Mathf.PI * m_recoilFrequency;
	}

	private void UpdateRecoil ( float _deltaTime )
	{
		if (m_recoil == 0f && m_recoilSpeed == 0f)
			return;

		float omega = 2f * Mathf.PI * m_recoilFrequency;
		for (float remaining = Mathf.Min(_deltaTime, .1f); remaining > 0f; remaining -= c_recoilStep)
		{
			float step = Mathf.Min(c_recoilStep, remaining);
			m_recoilSpeed += (-omega * omega * m_recoil - 2f * m_recoilDamping * omega * m_recoilSpeed) * step;
			m_recoil = Mathf.Clamp(m_recoil + m_recoilSpeed * step, -m_maxRecoil, m_maxRecoil);
		}

		if (Mathf.Abs(m_recoil) < .001f && Mathf.Abs(m_recoilSpeed) < .01f)
		{
			m_recoil = 0f;
			m_recoilSpeed = 0f;
			return;
		}

		if (m_recoilChest == null)
			m_recoilChest = GetHumanBone(HumanBodyBones.Chest) ?? GetHumanBone(HumanBodyBones.Spine) ?? GetTransform(CharacterBodyPart.Spine);

		Vector3 side = transform.right;
		ApplyRecoil(m_chestPose, m_recoilChest, Quaternion.AngleAxis(m_recoilSide * m_recoilBodyTwist * m_recoil, transform.up) * Quaternion.AngleAxis(-m_recoilBodyPitch * m_recoil, side));
		ApplyRecoil(m_armPose, m_recoilArm, Quaternion.AngleAxis(-m_recoilArmLift * m_recoil, side));
	}

	private static void ApplyRecoil ( RecoilPose _pose, Transform _bone, Quaternion _delta )
	{
		if (_bone == null)
			return;

		if (_pose.bone == _bone && _bone.localRotation == _pose.written)
			_bone.localRotation = _pose.animated;

		_pose.bone = _bone;
		_pose.animated = _bone.localRotation;
		_bone.rotation = _delta * _bone.rotation;
		_pose.written = _bone.localRotation;
	}

	#endregion
}
