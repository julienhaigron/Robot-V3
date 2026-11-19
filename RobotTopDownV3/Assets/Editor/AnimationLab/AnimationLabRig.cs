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

	public enum SegmentKind { Preparation, Action, Recovery }

	public struct Segment
	{
		public SegmentKind kind;
		public float start;
		public float end;
	}

	public struct PlayRequest
	{
		public EntityActionData action;
		public string key;
		public AttackAnimationPlayer.Timing timing;
		public bool isLeftHand;
		public int preparationTicks;
		public int performTicks;
		public int cooldownTicks;
		public float tickDuration;
		public float strikeTail;
	}

	private static readonly HashSet<Type> s_visualTypes = new() { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer) };

	public GameObject Root { get; private set; }
	public Transform Visual { get; private set; }
	public Animator Animator { get; private set; }
	public HumanoidEntityIK IK { get; private set; }
	public LegsAnimator Legs { get; private set; }
	public int TopLayer { get; private set; }
	public EntityAnimationProfile Profile { get; private set; }
	public AttackAnimationPlayer Player { get; private set; }
	public PlayRequest? Request { get; set; }
	public float PlanStart { get; private set; } = -1f;

	public readonly GameObject[] held = new GameObject[2];
	public readonly AnimationLabAssets.HeldOption[] heldOptions = new AnimationLabAssets.HeldOption[2];

	private int m_defaultTopState;
	private Sequence m_walk;
	private Sequence m_sequence;
	private List<Segment> m_plan = new();
	private readonly Dictionary<EntityActionData, (float speed, float intensity)> m_tunings = new();
	private readonly Dictionary<string, float> m_clipSpeedScales = new();
	private (float speed, float intensity)? m_defaults;

	public bool IsAlive => Root != null && Animator != null && IK != null;
	public bool IsWalking => m_walk != null && m_walk.IsActive();
	public bool IsSequencePlaying => m_sequence != null && m_sequence.IsActive();
	public IReadOnlyList<Segment> Plan => m_plan;

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
				Legs = ik.GetComponent<LegsAnimator>(),
				Profile = AnimationLabAssets.FindProfile(_unitPrefab)
			};

			if (rig.Animator == null)
				throw new InvalidOperationException(_unitPrefab.name + " n'a pas d'Animator sur son skin.");

			rig.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			rig.Player = new AttackAnimationPlayer(rig.Animator, rig.PlayerTuning);
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
		StopSequence(false);
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

	#region Tuning

	public void SetProfile ( EntityAnimationProfile _profile )
	{
		Profile = _profile;
	}

	public (float speed, float intensity) DefaultTuning => m_defaults ?? (Profile != null ? (Profile.attackSpeed, Profile.attackIntensity) : (1f, 1f));

	public bool AreDefaultsSaved => m_defaults == null || Profile == null
		|| Mathf.Abs(m_defaults.Value.speed - Profile.attackSpeed) < .005f && Mathf.Abs(m_defaults.Value.intensity - Profile.attackIntensity) < .005f;

	public void SetDefaults ( float _speed, float _intensity )
	{
		m_defaults = (_speed, _intensity);
	}

	public void ForgetDefaults ()
	{
		m_defaults = null;
	}

	public EntityAnimationProfile.TuningSource SourceFor ( EntityActionData _action, string _trigger )
	{
		return Profile != null ? Profile.SourceFor(_action, _trigger) : EntityAnimationProfile.TuningSource.Default;
	}

	public (float speed, float intensity) SavedTuningFor ( EntityActionData _action, string _trigger )
	{
		EntityAnimationProfile.AttackTuning entry = Profile != null ? Profile.Find(_action, _trigger) : null;
		return entry != null ? (entry.speed, entry.intensity) : DefaultTuning;
	}

	public (float speed, float intensity) TuningFor ( EntityActionData _action, string _trigger )
	{
		return _action != null && m_tunings.TryGetValue(_action, out (float speed, float intensity) tuning) ? tuning : SavedTuningFor(_action, _trigger);
	}

	public bool IsTuningSaved ( EntityActionData _action, string _trigger )
	{
		(float speed, float intensity) saved = SavedTuningFor(_action, _trigger);
		return _action == null || !m_tunings.TryGetValue(_action, out (float speed, float intensity) live)
			|| Mathf.Abs(live.speed - saved.speed) < .005f && Mathf.Abs(live.intensity - saved.intensity) < .005f;
	}

	public void SetTuning ( EntityActionData _action, float _speed, float _intensity )
	{
		if (_action == null)
			return;

		m_tunings[_action] = (_speed, _intensity);
		ApplyTuning(_action, _action.performAnimationKey);
	}

	public void ForgetTuning ( EntityActionData _action )
	{
		if (_action == null)
			return;

		m_tunings.Remove(_action);
		ApplyTuning(_action, _action.performAnimationKey);
	}

	public void SetClipSpeedScale ( string _trigger, float _scale )
	{
		if (string.IsNullOrEmpty(_trigger))
			return;

		if (Mathf.Abs(_scale - 1f) < .0001f)
			m_clipSpeedScales.Remove(_trigger);
		else
			m_clipSpeedScales[_trigger] = _scale;
	}

	public void ApplyTuning ( EntityActionData _action, string _trigger )
	{
		if (!Player.IsHolding)
			Player.ApplyTuning(_action, _trigger);
	}

	private AttackAnimationPlayer.Tuning PlayerTuning ( EntityActionData _action, string _trigger )
	{
		(float speed, float intensity) tuning = TuningFor(_action, _trigger);
		float scale = !string.IsNullOrEmpty(_trigger) && m_clipSpeedScales.TryGetValue(_trigger, out float value) ? value : 1f;
		return new AttackAnimationPlayer.Tuning(tuning.speed * scale, tuning.intensity);
	}

	#endregion

	#region Playback

	public List<Segment> PlanFor ( PlayRequest _request )
	{
		List<Segment> plan = new();
		float speed = Mathf.Max(.01f, PlayerTuning(_request.action, _request.key).speed);
		bool windsUp = _request.preparationTicks > 0 && _request.timing.WindsUp;
		float time = 0f;
		if (_request.preparationTicks > 0)
		{
			time = _request.preparationTicks * _request.tickDuration;
			plan.Add(new Segment { kind = SegmentKind.Preparation, start = 0f, end = time });
		}

		int performTicks = Mathf.Max(1, _request.performTicks);
		for (int i = 0; i < performTicks; i++)
		{
			float impact = Mathf.Max(0f, _request.timing.hitDelay - (i == 0 && windsUp ? _request.timing.holdDelay : 0f)) / speed;
			plan.Add(new Segment { kind = SegmentKind.Action, start = time, end = time + impact + _request.strikeTail });
			time += impact + _request.strikeTail;
		}

		if (_request.cooldownTicks > 0)
			plan.Add(new Segment { kind = SegmentKind.Recovery, start = time, end = time + _request.cooldownTicks * _request.tickDuration });
		return plan;
	}

	public void Play ( PlayRequest _request )
	{
		StopSequence(false);
		Animator.speed = 1f;
		object owner = new();
		m_plan = PlanFor(_request);
		m_sequence = DOTween.Sequence();
		foreach (Segment segment in m_plan)
		{
			switch (segment.kind)
			{
				case SegmentKind.Preparation:
					m_sequence.InsertCallback(segment.start, () => Player.StartPreparation(owner, _request.action, _request.key, _request.isLeftHand, _request.timing, segment.end - segment.start));
					break;
				case SegmentKind.Action:
					bool holdsRecovery = _request.cooldownTicks > 0 && segment.end >= m_plan.Where(candidate => candidate.kind == SegmentKind.Action).Max(candidate => candidate.end);
					m_sequence.InsertCallback(segment.start, () => Player.Strike(owner, _request.action, _request.key, _request.isLeftHand, _request.timing, holdsRecovery, out _));
					break;
			}
		}

		m_sequence.InsertCallback(m_plan[^1].end, () => Player.EndAction(owner));
		PlanStart = Time.time;
	}

	public void StopSequence ( bool _returnToIdle )
	{
		if (m_sequence != null && m_sequence.IsActive())
			m_sequence.Kill();
		m_sequence = null;
		PlanStart = -1f;
		if (Player == null)
			return;

		if (_returnToIdle)
			Player.Cancel();
		else
			Player.Stop();
	}

	public void Scrub ( AnimatorState _state, float _normalizedTime )
	{
		if (_state == null)
			return;

		StopSequence(false);
		Animator.speed = 0f;
		Animator.Play(_state.nameHash, TopLayer, Mathf.Clamp01(_normalizedTime));
		Animator.Update(0f);
	}

	public void Release ()
	{
		StopSequence(false);
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

	public float FindWindUpPeak ( AnimatorState _state, float _from, float _to, bool _isLeftHand )
	{
		Transform hand = Animator.GetBoneTransform(_isLeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
		if (hand == null || _state == null || _to <= _from)
			return _from;

		const int samples = 60;
		Scrub(_state, _to);
		Vector3 impact = Visual.InverseTransformPoint(hand.position);
		float best = _from;
		float bestDistance = -1f;
		for (int i = 0; i <= samples; i++)
		{
			float time = Mathf.Lerp(_from, _to, i / (float)samples);
			Scrub(_state, time);
			float distance = (Visual.InverseTransformPoint(hand.position) - impact).sqrMagnitude;
			if (distance > bestDistance)
			{
				bestDistance = distance;
				best = time;
			}
		}

		return best;
	}

	#endregion

	#region Walk

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
		if (Animator != null && Player != null && Player.HasParameter(AnimationLabAssets.MovingParameter))
			Animator.SetBool(AnimationLabAssets.MovingParameter, _isMoving);
	}

	#endregion

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
