using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class HumanoidEntityIK : MonoBehaviour
{
	[System.Serializable]
	public class BodyPartsTransforms : SerializableDictionary<CharacterBodyPart, Transform> { };

	public BodyPartsTransforms bodypartsTransform = new BodyPartsTransforms();
	public Animator animator;

	public Transform handGrabSocket;
	public Transform offHandGrabSocket;

	//public Transform rightHandTarget;
	//public Transform leftHandtarget;
	public Vector3? rightHandTarget;
	public Vector3? leftHandTarget;

	[Title("Shot Recoil")]
	[SerializeField] private float m_recoilBodyPitch = 6f;
	[SerializeField] private float m_recoilBodyTwist = 4f;
	[SerializeField] private float m_recoilArmLift = 10f;
	[SerializeField, Min(.5f)] private float m_recoilFrequency = 4.5f;
	[SerializeField, Range(.05f, 1f)] private float m_recoilDamping = .45f;
	[SerializeField, Min(0f)] private float m_maxRecoil = 3f;

	protected Quaternion m_defaultHandPivotRotation;
	private Quaternion m_defaultOffHandPivotRotation;
	private Transform m_aimingSocket;
	private Vector3? m_aimedPosition;
	private Entity m_aimedEntity;

	private const float RecoilImpulse = 1.75f;
	private const float RecoilStep = 1f / 120f;
	private float m_recoil;
	private float m_recoilSpeed;
	private float m_recoilSide;
	private Transform m_recoilArm;
	private Transform m_recoilChest;
	private readonly RecoilPose m_chestPose = new();
	private readonly RecoilPose m_armPose = new();

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

	public Transform GetGrabSocket ( int _armIndex )
	{
		return _armIndex % 2 == 1 && offHandGrabSocket != null ? offHandGrabSocket : handGrabSocket;
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
		if (animator != null && animator.isHuman)
		{
			Transform leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
			Transform rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
			if (leftArm != null && rightArm != null && (rightArm.position - leftArm.position).sqrMagnitude > .0001f)
				return (rightArm.position - leftArm.position).normalized;
		}

		Vector3 betweenHands = _offHand.position - _mainHand.position;
		return betweenHands.sqrMagnitude > .0001f ? betweenHands.normalized : transform.right;
	}

	[Button()]
	private void SetupBodyparts ()
	{
		animator = GetComponent<Animator>();
		bodypartsTransform.Add(CharacterBodyPart.RightHand, FindObjectContainingName("hand.right", transform));
		bodypartsTransform.Add(CharacterBodyPart.LeftHand, FindObjectContainingName("hand.left", transform));
		bodypartsTransform.Add(CharacterBodyPart.Spine, FindObjectContainingName("spine1", transform));
		bodypartsTransform.Add(CharacterBodyPart.RightFoot, FindObjectContainingName("foot.right", transform));
		bodypartsTransform.Add(CharacterBodyPart.LeftFoot, FindObjectContainingName("foot.left", transform));
	}

	private void Reset ()
	{
		SetupBodyparts();
		CreateHandSocket();
	}

	private Transform FindObjectContainingName ( string _name, Transform _transformFrom )
	{
		_name = _name.ToLower();
		foreach (Transform child in _transformFrom)
		{
			if (child.name.ToLower().Contains(_name))
			{
				return child;
			}
			Transform t = FindObjectContainingName(_name, child);
			if (t != null)
			{
				return t;
			}
		}
		return null;
	}

	public Transform GetTransform ( CharacterBodyPart _characterBodyPart )
	{
		if (bodypartsTransform.ContainsKey(_characterBodyPart))
		{
			return (bodypartsTransform[_characterBodyPart]);
		}
		return null;
	}

	public enum CharacterBodyPart
	{
		RightHand,
		LeftHand,
		Spine,
		RightFoot,
		LeftFoot,
		ArmStack,
	}

	private void OnAnimatorIK ( int _layerIndex )
	{
		if (rightHandTarget != null && rightHandTarget.HasValue)
		{
			animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
			animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.Value);
		}
		else
		{
			animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
			//animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
		}
		if (leftHandTarget != null && leftHandTarget.HasValue)
		{
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.Value);
			/*animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandtarget.rotation);*/
		}
		else
		{
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
			//animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
		}
	}

	public bool IsHeldInLeftHand ( Transform _item )
	{
		return GetIKGoalOf(_item) == AvatarIKGoal.LeftHand;
	}

	public void Recoil ( float _strength, Transform _weapon, bool _isTwoHanded )
	{
		if (_strength <= 0f || !isActiveAndEnabled)
			return;

		bool isLeftHand = IsHeldInLeftHand(_weapon);
		m_recoilSide = _isTwoHanded ? 0f : isLeftHand ? -1f : 1f;
		m_recoilArm = _isTwoHanded ? null : GetHumanBone(isLeftHand ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
		m_recoilSpeed += _strength * RecoilImpulse * 2f * Mathf.PI * m_recoilFrequency;
	}

	private void LateUpdate ()
	{
		AimSocket();
		UpdateRecoil(Time.deltaTime);
	}

	private void AimSocket ()
	{
		if (m_aimingSocket == null)
			return;

		if (m_aimedEntity != null)
			m_aimingSocket.LookAt(m_aimedEntity.transform.position + Vector3.up);
		else if (m_aimedPosition.HasValue)
			m_aimingSocket.LookAt(m_aimedPosition.Value);
	}

	private void UpdateRecoil ( float _deltaTime )
	{
		if (m_recoil == 0f && m_recoilSpeed == 0f)
			return;

		float omega = 2f * Mathf.PI * m_recoilFrequency;
		for (float remaining = Mathf.Min(_deltaTime, .1f); remaining > 0f; remaining -= RecoilStep)
		{
			float step = Mathf.Min(RecoilStep, remaining);
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

	private Transform GetHumanBone ( HumanBodyBones _bone )
	{
		return animator != null && animator.isHuman ? animator.GetBoneTransform(_bone) : null;
	}

	public void Aim ( Vector3 _position )
	{
		Aim(handGrabSocket, _position);
	}

	public void Aim ( Transform _socket, Vector3 _position )
	{
		if (!isActiveAndEnabled)
			return;

		Transform socket = _socket != null ? _socket : handGrabSocket;
		if (m_aimingSocket != null && m_aimingSocket != socket)
			ResetSocketRotation(m_aimingSocket);

		m_aimingSocket = socket;
		m_aimedPosition = _position;
		m_aimedEntity = null;
		bool isLeftHand = GetIKGoalOf(socket) == AvatarIKGoal.LeftHand;
		leftHandTarget = isLeftHand ? _position : null;
		rightHandTarget = isLeftHand ? null : _position;
	}

	public void Aim ( Entity _target )
	{
		if (!isActiveAndEnabled)
			return;

		m_aimingSocket = handGrabSocket;
		m_aimedEntity = _target;
		m_aimedPosition = null;
	}

	public virtual void ReleaseAim ()
	{
		m_aimedPosition = null;
		m_aimedEntity = null;
		rightHandTarget = null;
		leftHandTarget = null;
		m_aimingSocket = null;
		ResetSocketRotation(handGrabSocket);
		ResetSocketRotation(offHandGrabSocket);
	}

	private void ResetSocketRotation ( Transform _socket )
	{
		if (_socket != null)
			_socket.localRotation = _socket == offHandGrabSocket ? m_defaultOffHandPivotRotation : m_defaultHandPivotRotation;
	}

	[Button, HideIf("handGrabSocket")]
	public void CreateHandSocket ()
	{
		if (handGrabSocket)
			return;

		GameObject socket = new()
		{
			name = "GrabSocket"
		};

		socket.transform.parent = GetTransform(CharacterBodyPart.RightHand);
		socket.transform.localScale = Vector3.one;
		socket.transform.localPosition = Vector3.zero;
		socket.transform.localRotation = Quaternion.Euler(-90, 0, 90);

		handGrabSocket = socket.transform;
	}
}