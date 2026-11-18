using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using DG.Tweening;

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

	protected Quaternion m_defaultHandPivotRotation;
	private Quaternion m_defaultOffHandPivotRotation;
	private Transform m_aimingSocket;

	private Coroutine m_aimCoroutine;
	private Tween m_recolTween;

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

	public void OnFire ()
	{
		Transform hand = m_aimingSocket != null && m_aimingSocket.parent != null
			? m_aimingSocket.parent : GetTransform(CharacterBodyPart.RightHand);
		if (hand == null)
			return;

		m_recolTween.Kill();
		m_recolTween = hand.DOPunchRotation(Vector3.right * 5f, .2f, 0, 0f);
	}

	public void Aim ( Vector3 _position )
	{
		Aim(handGrabSocket, _position);
	}

	public void Aim ( Transform _socket, Vector3 _position )
	{
		if (!TryStopAimCoroutine())
			return;

		Transform socket = _socket != null ? _socket : handGrabSocket;
		if (m_aimingSocket != null && m_aimingSocket != socket)
			ResetSocketRotation(m_aimingSocket);

		m_aimingSocket = socket;
		bool isLeftHand = GetIKGoalOf(socket) == AvatarIKGoal.LeftHand;
		leftHandTarget = isLeftHand ? _position : null;
		rightHandTarget = isLeftHand ? null : _position;

		m_aimCoroutine = StartCoroutine(AimPositionCR(m_aimingSocket, _position));
	}

	public void Aim ( Entity _target )
	{
		if (!TryStopAimCoroutine())
			return;

		m_aimingSocket = handGrabSocket;
		m_aimCoroutine = StartCoroutine(AimEntityCR(m_aimingSocket, _target));
	}

	private bool TryStopAimCoroutine ()
	{
		if (m_aimCoroutine != null)
			StopCoroutine(m_aimCoroutine);

		m_aimCoroutine = null;
		return isActiveAndEnabled;
	}

	IEnumerator AimEntityCR ( Transform _socket, Entity _target )
	{
		while (true)
		{
			_socket.LookAt(_target.transform.position + Vector3.up);
			yield return null;
		}
	}

	IEnumerator AimPositionCR ( Transform _socket, Vector3 _position )
	{
		while (true)
		{
			_socket.LookAt(_position);
			yield return null;
		}
	}

	public virtual void ReleaseAim ()
	{
		TryStopAimCoroutine();

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