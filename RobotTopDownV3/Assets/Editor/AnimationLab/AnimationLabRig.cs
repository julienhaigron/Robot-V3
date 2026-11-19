using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FIMSpace.FProceduralAnimation;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public class AnimationLabRig
{
	public const int MainHand = 0;
	public const int OffHand = 1;

	private static readonly HashSet<Type> s_visualTypes = new() { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer) };

	public GameObject Root { get; private set; }
	public Transform Visual { get; private set; }
	public Animator Animator { get; private set; }
	public HumanoidEntityIK IK { get; private set; }
	public LegsAnimator Legs { get; private set; }
	public int TopLayer { get; private set; }

	public readonly GameObject[] held = new GameObject[2];
	public readonly AnimationLabAssets.HeldOption[] heldOptions = new AnimationLabAssets.HeldOption[2];

	private int m_defaultTopState;
	private Sequence m_walk;
	private HashSet<string> m_parameters;

	public bool IsAlive => Root != null && Animator != null && IK != null;
	public bool IsWalking => m_walk != null && m_walk.IsActive();

	public static AnimationLabRig Build ( GameObject _unitPrefab )
	{
		GameObject holder = new("Animation Lab Holder");
		holder.SetActive(false);
		try
		{
			GameObject unit = Object.Instantiate(_unitPrefab, holder.transform);
			unit.name = _unitPrefab.name + " (Lab)";
			HumanoidEntityIK ik = unit.GetComponentInChildren<HumanoidEntityIK>(true);
			if (ik == null)
				throw new InvalidOperationException(_unitPrefab.name + " n'a pas de HumanoidEntityIK : ce n'est pas une unité humanoïde.");

			StripBehavioursOutside(unit, ik.transform);
			unit.transform.SetParent(null, false);
			unit.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

			AnimationLabRig rig = new()
			{
				Root = unit,
				IK = ik,
				Visual = ik.transform.parent != null ? ik.transform.parent : ik.transform,
				Animator = ik.animator != null ? ik.animator : ik.GetComponent<Animator>(),
				Legs = ik.GetComponent<LegsAnimator>()
			};

			if (rig.Animator == null)
				throw new InvalidOperationException(_unitPrefab.name + " n'a pas d'Animator sur son skin.");

			rig.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			rig.m_parameters = new HashSet<string>(rig.Animator.parameters.Select(parameter => parameter.name));
			rig.TopLayer = rig.Animator.GetLayerIndex(AnimationLabAssets.AttackLayer);
			if (rig.TopLayer < 0)
				rig.TopLayer = rig.Animator.layerCount - 1;

			AnimatorController controller = rig.Animator.runtimeAnimatorController as AnimatorController;
			AnimatorStateMachine machine = controller != null && rig.TopLayer < controller.layers.Length ? controller.layers[rig.TopLayer].stateMachine : null;
			rig.m_defaultTopState = machine != null && machine.defaultState != null ? machine.defaultState.nameHash : 0;
			return rig;
		}
		finally
		{
			Object.Destroy(holder);
		}
	}

	public void Teardown ()
	{
		StopWalk();
		if (Root != null)
			Object.Destroy(Root);
		Root = null;
	}

	public Transform SocketFor ( int _hand )
	{
		return _hand == OffHand && IK.offHandGrabSocket != null ? IK.offHandGrabSocket : IK.handGrabSocket;
	}

	public bool IsHeldInLeftHand ( int _hand )
	{
		return held[_hand] != null ? IK.IsHeldInLeftHand(held[_hand].transform) : IK.IsHeldInLeftHand(SocketFor(_hand));
	}

	public void Equip ( int _hand, AnimationLabAssets.HeldOption _option )
	{
		if (held[_hand] != null)
			Object.Destroy(held[_hand]);

		held[_hand] = null;
		heldOptions[_hand] = _option;
		if (_option == null || _option.prefab == null)
			return;

		GameObject holder = new("Animation Lab Held Holder");
		holder.SetActive(false);
		try
		{
			GameObject instance = Object.Instantiate(_option.prefab, holder.transform);
			instance.name = _option.prefab.name + " (Lab)";
			StripToVisual(instance);
			instance.transform.SetParent(SocketFor(_hand), false);
			instance.transform.localScale = Vector3.one;
			IK.FitInHand(instance.transform);
			held[_hand] = instance;
		}
		finally
		{
			Object.Destroy(holder);
		}
	}

	public void Trigger ( string _trigger, bool _leftHand )
	{
		Animator.speed = 1f;
		if (m_parameters.Contains(AnimationLabAssets.HandParameter))
			Animator.SetBool(AnimationLabAssets.HandParameter, _leftHand);
		if (m_parameters.Contains(_trigger))
			Animator.SetTrigger(_trigger);
	}

	public void Scrub ( AnimatorState _state, float _normalizedTime )
	{
		if (_state == null)
			return;

		Animator.speed = 0f;
		Animator.Play(_state.nameHash, TopLayer, Mathf.Clamp01(_normalizedTime));
		Animator.Update(0f);
	}

	public void Release ()
	{
		Animator.speed = 1f;
		if (m_defaultTopState != 0)
			Animator.Play(m_defaultTopState, TopLayer, 0f);
	}

	public bool IsTopIdle ()
	{
		return Animator.GetCurrentAnimatorStateInfo(TopLayer).shortNameHash == m_defaultTopState && !Animator.IsInTransition(TopLayer);
	}

	public bool TryGetPlayingTime ( AnimatorState _state, out float _normalizedTime )
	{
		AnimatorStateInfo info = Animator.GetCurrentAnimatorStateInfo(TopLayer);
		_normalizedTime = info.normalizedTime;
		return _state != null && info.shortNameHash == _state.nameHash;
	}

	public void Walk ( int _tiles, float _tileDuration, float _turnDuration, bool _backAndForth )
	{
		StopWalk();
		Vector3 start = Root.transform.position;
		Vector3 direction = Visual.forward;
		direction.y = 0f;
		direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.forward;
		float step = 2f * Tile.innerRadius;

		m_walk = DOTween.Sequence();
		AppendMove(m_walk, start, direction, step, _tiles, _tileDuration);
		if (_backAndForth)
		{
			AppendTurn(m_walk, _turnDuration);
			AppendMove(m_walk, start + direction * step * _tiles, -direction, step, _tiles, _tileDuration);
			AppendTurn(m_walk, _turnDuration);
			m_walk.SetLoops(-1, LoopType.Restart);
		}

		m_walk.OnKill(() => SetMoving(false));
	}

	public void StopWalk ()
	{
		if (m_walk != null && m_walk.IsActive())
			m_walk.Kill();
		m_walk = null;
		if (Animator != null)
			SetMoving(false);
	}

	public void ResetPosition ()
	{
		StopWalk();
		Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
		Visual.localRotation = Quaternion.identity;
	}

	private void AppendMove ( Sequence _sequence, Vector3 _from, Vector3 _direction, float _step, int _tiles, float _tileDuration )
	{
		_sequence.AppendCallback(() => SetMoving(true));
		for (int i = 1; i <= _tiles; i++)
			_sequence.Append(Root.transform.DOMove(_from + _direction * _step * i, _tileDuration).SetEase(Ease.Linear));
		_sequence.AppendCallback(() => SetMoving(false));
	}

	private void AppendTurn ( Sequence _sequence, float _duration )
	{
		_sequence.Append(Visual.DOLocalRotate(new Vector3(0f, 180f, 0f), Mathf.Max(.01f, _duration), RotateMode.LocalAxisAdd));
	}

	private void SetMoving ( bool _isMoving )
	{
		if (Animator != null && m_parameters != null && m_parameters.Contains(AnimationLabAssets.MovingParameter))
			Animator.SetBool(AnimationLabAssets.MovingParameter, _isMoving);
	}

	private static void StripBehavioursOutside ( GameObject _root, Transform _keep )
	{
		for (int pass = 0; pass < 4; pass++)
		{
			bool isBlocked = false;
			bool hasRemoved = false;
			foreach (MonoBehaviour behaviour in _root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
			{
				if (behaviour == null || behaviour.transform.IsChildOf(_keep))
					continue;

				if (TryDestroy(behaviour))
					hasRemoved = true;
				else
					isBlocked = true;
			}

			if (!isBlocked || !hasRemoved)
				return;
		}
	}

	private static void StripToVisual ( GameObject _root )
	{
		for (int pass = 0; pass < 4; pass++)
		{
			bool isBlocked = false;
			bool hasRemoved = false;
			foreach (Component component in _root.GetComponentsInChildren<Component>(true).Reverse())
			{
				if (component == null || s_visualTypes.Contains(component.GetType()))
					continue;

				if (TryDestroy(component))
					hasRemoved = true;
				else
					isBlocked = true;
			}

			if (!isBlocked || !hasRemoved)
				return;
		}
	}

	private static bool TryDestroy ( Component _component )
	{
		try
		{
			Object.DestroyImmediate(_component);
		}
		catch (Exception)
		{
			return false;
		}

		return _component == null;
	}
}
