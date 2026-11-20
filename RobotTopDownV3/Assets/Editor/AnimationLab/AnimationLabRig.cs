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
		public string label;
	}

	public class Plan
	{
		public readonly List<Segment> segments = new();
		public readonly List<float> marks = new();
		public readonly List<(float time, Action cue)> cues = new();
		public bool isAiming;

		public float Duration => segments.Count > 0 ? segments[^1].end : 0f;

		public float Add ( SegmentKind _kind, float _start, float _duration, string _label )
		{
			segments.Add(new Segment { kind = _kind, start = _start, end = _start + _duration, label = _label });
			return _start + _duration;
		}

		public void At ( float _time, Action _cue )
		{
			cues.Add((_time, _cue));
		}
	}

	public struct AttackRequest
	{
		public EntityActionData action;
		public string key;
		public AttackAnimationPlayer.Timing timing;
		public bool isLeftHand;
		public int hand;
		public int preparationTicks;
		public int performTicks;
		public int cooldownTicks;
		public float tickDuration;
		public float strikeTail;
	}

	public struct ShotRequest
	{
		public EntityActionData action;
		public string aimKey;
		public int hand;
		public int preparationTicks;
		public int performTicks;
		public int cooldownTicks;
		public float tickDuration;
		public bool aimsDuringPreparation;
		public string preparationKey;
		public bool holdsAimDuringCooldown;
		public float aimDuration;
		public float bulletInterval;
		public float shootCooldown;
		public float strikeTail;
	}

	private static readonly HashSet<Type> s_visualTypes = new() { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer), typeof(ParticleSystem), typeof(ParticleSystemRenderer), typeof(TrailRenderer), typeof(LineRenderer) };

	public GameObject Root { get; private set; }
	public Transform Visual { get; private set; }
	public Animator Animator { get; private set; }
	public HumanoidEntityIK IK { get; private set; }
	public LegsAnimator Legs { get; private set; }
	public int TopLayer { get; private set; }
	public EntityAnimationProfile Profile { get; private set; }
	public AttackAnimationPlayer Player { get; private set; }
	public GuardAnimationPlayer Guard { get; private set; }
	public LocomotionAnimationPlayer Locomotion { get; private set; }
	public AnimationLabFeedback Feedback { get; private set; }
	public float CenterHeight { get; private set; } = 1f;
	public Func<Plan> Request { get; set; }
	public Plan CurrentPlan { get; private set; }
	public float PlanStart { get; private set; } = -1f;

	public readonly GameObject[] held = new GameObject[2];
	public readonly AnimationLabAssets.HeldOption[] heldOptions = new AnimationLabAssets.HeldOption[2];
	public readonly AnimationLabFeedback.WeaponVisual[] weaponVisuals = new AnimationLabFeedback.WeaponVisual[2];

	private int m_defaultTopState;
	private Sequence m_walk;
	private Sequence m_sequence;
	private bool m_isAiming;
	private readonly Dictionary<EntityActionData, (float speed, float intensity)> m_tunings = new();
	private readonly Dictionary<string, float> m_clipSpeedScales = new();
	private (float speed, float intensity)? m_defaults;
	private bool m_isMoving;
	private string m_moveStyle;
	private bool m_isFlying;
	private string m_guardKey;
	private bool m_isGuardLeft;
	private float m_guardTorso;

	public bool IsAlive => Root != null && Animator != null && IK != null;
	public bool IsWalking => m_walk != null && m_walk.IsActive();
	public bool IsSequencePlaying => m_sequence != null && m_sequence.IsActive();
	public float PlayTime => IsSequencePlaying ? Time.time - PlanStart : -1f;

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
			rig.Guard = new GuardAnimationPlayer(rig.Animator);
			rig.Locomotion = new LocomotionAnimationPlayer(rig.Animator);
			rig.Feedback = new AnimationLabFeedback(rig);
			Transform hips = rig.Animator.isHuman ? rig.Animator.GetBoneTransform(HumanBodyBones.Hips) : null;
			if (hips != null)
				rig.CenterHeight = Mathf.Max(.3f, hips.position.y - unit.transform.position.y);
			ik.GripAmount = rig.Profile != null ? rig.Profile.gripAmount : 1f;
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
		Feedback?.Clear();
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
		weaponVisuals[_hand] = null;
		heldOptions[_hand] = _option;
		ResetGuardFromHeld();
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
			weaponVisuals[_hand] = AnimationLabFeedback.Describe(_option.prefab, instance);
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

	public void SetTuning ( EntityActionData _action, string _trigger, float _speed, float _intensity )
	{
		if (_action == null)
			return;

		m_tunings[_action] = (_speed, _intensity);
		ApplyTuning(_action, _trigger);
	}

	public void ForgetTuning ( EntityActionData _action, string _trigger )
	{
		if (_action == null)
			return;

		m_tunings.Remove(_action);
		ApplyTuning(_action, _trigger);
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

	public Plan AttackPlan ( AttackRequest _request )
	{
		Plan plan = new();
		object owner = new();
		float speed = Mathf.Max(.01f, PlayerTuning(_request.action, _request.key).speed);
		bool windsUp = _request.preparationTicks > 0 && _request.timing.WindsUp;
		float time = 0f;
		if (_request.preparationTicks > 0)
		{
			float preparation = _request.preparationTicks * _request.tickDuration;
			plan.At(0f, () => Player.StartPreparation(owner, _request.action, _request.key, _request.isLeftHand, _request.timing, preparation));
			time = plan.Add(SegmentKind.Preparation, 0f, preparation, "Préparation");
		}

		int performTicks = Mathf.Max(1, _request.performTicks);
		for (int i = 0; i < performTicks; i++)
		{
			float impact = Mathf.Max(0f, _request.timing.hitDelay - (i == 0 && windsUp ? _request.timing.holdDelay : 0f)) / speed;
			bool holdsRecovery = _request.cooldownTicks > 0 && i == performTicks - 1;
			plan.At(time, () =>
			{
				Player.Strike(owner, _request.action, _request.key, _request.isLeftHand, _request.timing, holdsRecovery, out float impactDelay);
				Feedback.PlayLater(impactDelay, _request.action, weaponVisuals[_request.hand]);
			});
			plan.marks.Add(time + impact);
			time = plan.Add(SegmentKind.Action, time, impact + _request.strikeTail, "Action");
		}

		if (_request.cooldownTicks > 0)
			time = plan.Add(SegmentKind.Recovery, time, _request.cooldownTicks * _request.tickDuration, "Récupération");
		plan.At(time, () => Player.EndAction(owner));
		return plan;
	}

	public Plan ShotPlan ( ShotRequest _request )
	{
		Plan plan = new() { isAiming = true };
		object owner = new();
		EntityActionData action = _request.action;
		AnimationLabFeedback.WeaponVisual visual = weaponVisuals[_request.hand];
		bool isSpecial = action.type == EntityActionData.ActionType.Special;
		bool isBulletShot = !isSpecial && visual != null && visual.isBulletWeapon && AnimationLabFeedback.IsSingleTargetShot(action);
		float time = 0f;
		if (_request.preparationTicks > 0)
		{
			float preparation = _request.preparationTicks * _request.tickDuration;
			if (!isSpecial && _request.aimsDuringPreparation)
				plan.At(0f, () => AimAt(_request.hand, _request.aimKey, action));
			else if (!isSpecial && !string.IsNullOrEmpty(_request.preparationKey))
				plan.At(0f, () => Player.StartPreparation(owner, action, null, IsHeldInLeftHand(_request.hand), new AttackAnimationPlayer.Timing { preparation = EntityActionData.PreparationAnimationMode.Trigger, preparationKey = _request.preparationKey }, preparation));
			time = plan.Add(SegmentKind.Preparation, 0f, preparation, !isSpecial && _request.aimsDuringPreparation ? "Visée" : "Préparation");
		}

		int performTicks = Mathf.Max(1, _request.performTicks);
		for (int tick = 0; tick < performTicks; tick++)
		{
			float start = time;
			float aimDuration = isSpecial ? SpecialAction.WeaponAimDuration : _request.aimDuration;
			plan.At(start, () => AimAt(_request.hand, _request.aimKey, action));
			float fire = start + aimDuration;
			float volleyEnd;
			if (isSpecial)
			{
				plan.At(fire, () => Feedback.PlaySpecial(action, visual));
				plan.marks.Add(fire);
				volleyEnd = fire + SpecialAction.WeaponAimHold;
			}
			else if (isBulletShot)
			{
				int bullets = Mathf.Max(1, action.hitAmount);
				for (int i = 0; i < bullets; i++)
				{
					float shot = fire + i * _request.bulletInterval;
					plan.At(shot, () => Feedback.PlayShot(action, visual));
					plan.marks.Add(shot);
				}

				volleyEnd = fire + bullets * _request.bulletInterval;
			}
			else
			{
				plan.At(fire, () => Feedback.PlayStrike(action, visual));
				plan.marks.Add(fire);
				volleyEnd = fire + _request.strikeTail;
			}

			bool keepsAim = !isSpecial && EntityActionData.KeepsAimAfterVolley(tick == performTicks - 1, _request.cooldownTicks, _request.holdsAimDuringCooldown);
			if (!keepsAim)
				plan.At(volleyEnd, ReleaseAim);
			time = plan.Add(SegmentKind.Action, start, volleyEnd - start + (isSpecial ? 0f : _request.shootCooldown), isSpecial ? "Lancer" : "Tir");
		}

		if (_request.cooldownTicks > 0)
			time = plan.Add(SegmentKind.Recovery, time, _request.cooldownTicks * _request.tickDuration, !isSpecial && _request.holdsAimDuringCooldown ? "Visée tenue" : "Récupération");
		plan.At(time, () =>
		{
			ReleaseAim();
			Player.EndAction(owner);
		});
		return plan;
	}

	public void Play ( Plan _plan )
	{
		KillSequence();
		Player.Stop();
		if (_plan == null || !_plan.isAiming)
			ReleaseAim();
		if (_plan == null)
			return;

		Animator.speed = 1f;
		CurrentPlan = _plan;
		m_sequence = DOTween.Sequence();
		foreach ((float time, Action cue) in _plan.cues.OrderBy(cue => cue.time))
			m_sequence.InsertCallback(time, () => cue());
		m_sequence.AppendInterval(.01f);
		PlanStart = Time.time;
	}

	public void AimAt ( int _hand, string _aimKey, EntityActionData _action )
	{
		Transform heldItem = held[_hand] != null ? held[_hand].transform : null;
		IK.Aim(SocketFor(_hand), Feedback.TargetPoint(_action), heldItem);
		Player.PlayAim(_action, _aimKey, IsHeldInLeftHand(_hand));
		m_isAiming = true;
	}

	public void ReleaseAim ()
	{
		if (!m_isAiming)
			return;

		Player.ReleaseAim();
		IK.ReleaseAim();
		m_isAiming = false;
	}

	private void KillSequence ()
	{
		if (m_sequence != null && m_sequence.IsActive())
			m_sequence.Kill();
		m_sequence = null;
		PlanStart = -1f;
	}

	public void StopSequence ( bool _returnToIdle )
	{
		KillSequence();
		if (Player == null)
			return;

		ReleaseAim();
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

	#region Guard

	public void SetGuard ( string _key, bool _isLeftHand, float _torsoYaw )
	{
		m_guardKey = _key;
		m_isGuardLeft = _isLeftHand;
		m_guardTorso = _torsoYaw;
	}

	public void ResetGuardFromHeld ()
	{
		for (int hand = MainHand; hand <= OffHand; hand++)
		{
			if (heldOptions[hand]?.data is WeaponEquipmentData weapon && !string.IsNullOrEmpty(weapon.guardAnimation))
			{
				SetGuard(weapon.guardAnimation, IsHeldInLeftHand(hand), weapon.guardTorsoYaw);
				return;
			}
		}

		SetGuard(null, false, 0f);
	}

	public void TickAnimators ( float _deltaTime )
	{
		Guard.Set(m_isMoving ? null : m_guardKey, m_isGuardLeft, m_guardTorso);
		Guard.Update(_deltaTime);
		Locomotion.Set(m_isMoving, m_moveStyle, m_isFlying);
		Locomotion.Update();
	}

	public float FrontTorsoYaw ()
	{
		Transform left = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
		Transform right = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
		if (left == null || right == null || !Guard.HasGuard || !Player.HasParameter(GuardAnimationPlayer.TorsoParameter))
			return m_guardTorso;

		float weight = Animator.GetLayerWeight(TopLayer);
		float twist = Animator.GetFloat(GuardAnimationPlayer.TorsoParameter);
		Animator.SetLayerWeight(TopLayer, 0f);
		float raw = ShouldersYaw(0f, left, right);
		float probe = Mathf.Clamp(-raw / GuardAnimationPlayer.TorsoRange, -1f, 1f);
		float gain = Mathf.Abs(probe) > .01f ? (ShouldersYaw(probe, left, right) - raw) / (probe * GuardAnimationPlayer.TorsoRange) : 1f;
		Animator.SetFloat(GuardAnimationPlayer.TorsoParameter, twist);
		Animator.SetLayerWeight(TopLayer, weight);
		Animator.Update(0f);

		float yaw = Mathf.Clamp(-raw / Mathf.Max(.2f, gain), -GuardAnimationPlayer.TorsoRange, GuardAnimationPlayer.TorsoRange);
		return m_isGuardLeft ? -yaw : yaw;
	}

	private float ShouldersYaw ( float _twist, Transform _left, Transform _right )
	{
		Animator.SetFloat(GuardAnimationPlayer.TorsoParameter, _twist);
		Animator.Update(0f);
		Vector3 local = Visual.InverseTransformDirection(_right.position - _left.position);
		return Mathf.Atan2(-local.z, local.x) * Mathf.Rad2Deg;
	}

	#endregion

	#region Movement

	public void Move ( string _style, bool _isFlying, int _tiles, float _tileDuration, float _turnDuration, bool _backAndForth )
	{
		StopWalk();
		m_moveStyle = _style;
		m_isFlying = _isFlying;
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

		m_walk.OnKill(() => m_isMoving = false);
	}

	public void SetFlying ( bool _isFlying )
	{
		m_isFlying = _isFlying;
	}

	public void StopWalk ()
	{
		if (m_walk != null && m_walk.IsActive())
			m_walk.Kill();
		m_walk = null;
		m_isMoving = false;
	}

	public void ResetPosition ()
	{
		StopWalk();
		Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
		Visual.localRotation = Quaternion.identity;
	}

	private void AppendMove ( Sequence _sequence, Vector3 _from, Vector3 _direction, float _step, int _tiles, float _tileDuration )
	{
		_sequence.AppendCallback(() => m_isMoving = true);
		for (int i = 1; i <= _tiles; i++)
			_sequence.Append(Root.transform.DOMove(_from + _direction * _step * i, _tileDuration).SetEase(Ease.Linear));
		_sequence.AppendCallback(() => m_isMoving = false);
	}

	private void AppendTurn ( Sequence _sequence, float _duration )
	{
		_sequence.Append(Visual.DOLocalRotate(new Vector3(0f, 180f, 0f), Mathf.Max(.01f, _duration), RotateMode.LocalAxisAdd));
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

	public static void StripToVisual ( GameObject _root )
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
