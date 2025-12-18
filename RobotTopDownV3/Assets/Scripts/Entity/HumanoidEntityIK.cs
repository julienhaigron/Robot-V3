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

	public Transform rightHandTarget;
	public Transform leftHandtarget;

	protected Quaternion m_defaultHandPivotRotation;

	private Coroutine m_aimCoroutine;
	private Tween m_recolTween;

	private void Awake ()
	{
		m_defaultHandPivotRotation = handGrabSocket.localRotation;
	}

	[ContextMenu("Setup Bodyparts")]
	private void SetupBodyparts ()
	{
		animator = GetComponent<Animator>();
		bodypartsTransform.Add(CharacterBodyPart.RightHand, FindObjectContainingName("RightHand", transform));
		bodypartsTransform.Add(CharacterBodyPart.LeftHand, FindObjectContainingName("LeftHand", transform));
		bodypartsTransform.Add(CharacterBodyPart.Spine, FindObjectContainingName("Spine", transform));
		bodypartsTransform.Add(CharacterBodyPart.RightFoot, FindObjectContainingName("RightFoot", transform));
		bodypartsTransform.Add(CharacterBodyPart.LeftFoot, FindObjectContainingName("LeftFoot", transform));
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
		if (rightHandTarget != null)
		{
			animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
			animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
		}
		else
		{
			animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
			animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
		}
		if (leftHandtarget != null)
		{
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandtarget.position);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandtarget.rotation);
		}
		else
		{
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
		}
	}

	public void OnFire ()
	{
		m_recolTween.Kill();
		m_recolTween = bodypartsTransform[CharacterBodyPart.RightHand].DOPunchRotation(Vector3.right * 5f, .2f, 0, 0f);
	}

	public void Aim ( Vector3 _position )
	{
		if (m_aimCoroutine != null)
			StopCoroutine(m_aimCoroutine);

		m_aimCoroutine = StartCoroutine(AimPositionCR(_position));
	}

	public void Aim ( Entity _target )
	{
		if (m_aimCoroutine != null)
			StopCoroutine(m_aimCoroutine);

		m_aimCoroutine = StartCoroutine(AimEntityCR(_target));
	}

	IEnumerator AimEntityCR ( Entity _target )
	{
		while (true)
		{
			handGrabSocket.LookAt(_target.transform.position + Vector3.up);
			yield return null;
		}
	}

	IEnumerator AimPositionCR ( Vector3 _position )
	{
		while (true)
		{
			handGrabSocket.LookAt(_position);
			yield return null;
		}
	}

	public virtual void ReleaseAim ()
	{
		if (m_aimCoroutine != null)
			StopCoroutine(m_aimCoroutine);

		handGrabSocket.localRotation = m_defaultHandPivotRotation;
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

	/*[Button, ShowIf("handGrabSocket")]
	private void FixHandGrabSocketPosition ()
	{
		handGrabSocket.localPosition = new Vector3(0.000280000007f, 0.00132000004f, 0.000859999971f);
	}*/
}