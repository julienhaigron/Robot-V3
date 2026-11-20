using System;
using DG.Tweening;
using UnityEngine;

public class AttackAnimationPlayer : AnimatorLayerDriver
{
	public const string LeftHandParameter = "LeftHandAttack";
	public const string SpeedParameter = "AttackSpeed";
	public const string LayerName = "Top";
	public const string IdleStateName = "Default";
	public const string AimStatePrefix = "Aim ";
	private const float c_cancelFade = .25f;
	private const float c_aimFadeIn = .12f;
	private const float c_aimFadeOut = .3f;
	private const float c_maxWindUpBoost = 1.5f;
	private const float c_minWindUp = .05f;

	public struct Tuning
	{
		public float speed;
		public float intensity;

		public Tuning ( float _speed, float _intensity )
		{
			speed = _speed;
			intensity = _intensity;
		}
	}

	public struct Timing
	{
		public float hitDelay;
		public EntityActionData.PreparationAnimationMode preparation;
		public string preparationKey;
		public float holdDelay;
		public float windUpRatio;
		public EntityActionData.RecoveryAnimationMode recovery;
		public string recoveryKey;
		public float recoveryDelay;

		public bool WindsUp => preparation == EntityActionData.PreparationAnimationMode.WindUp && holdDelay > 0f;

		public static Timing From ( EntityActionData _data )
		{
			return new Timing
			{
				hitDelay = _data.performAnimationHitDelay,
				preparation = _data.preparationAnimationMode,
				preparationKey = _data.preparationAnimationKey,
				holdDelay = _data.preparationHoldDelay,
				windUpRatio = _data.preparationWindUpRatio,
				recovery = _data.recoveryAnimationMode,
				recoveryKey = _data.afterPerformAnimationKey,
				recoveryDelay = _data.recoveryHoldDelay
			};
		}
	}

	private enum Phase { None, WindUp, Preparation, Strike, Recovery }

	private readonly Func<EntityActionData, string, Tuning> m_tuningFor;
	private readonly int m_idleState = Animator.StringToHash(IdleStateName);
	private bool? m_hasIdleState;

	private Phase m_phase;
	private object m_owner;
	private EntityActionData m_data;
	private string m_key;
	private Timing m_timing;
	private float m_speed = 1f;
	private float m_windUpSpeed;
	private float m_windUpDuration;
	private Tween m_ramp;
	private Tween m_pending;
	private int m_aimState;

	public object Owner => m_owner;
	public bool IsAiming => m_aimState != 0;
	public bool IsHolding => m_phase is Phase.WindUp or Phase.Preparation or Phase.Recovery;

	public AttackAnimationPlayer ( Animator _animator, Func<EntityActionData, string, Tuning> _tuningFor ) : base(_animator, LayerName)
	{
		m_tuningFor = _tuningFor;
	}

	public static string AimStateName ( string _key, bool _isLeftHand )
	{
		return HandStateName(AimStatePrefix, _key, _isLeftHand);
	}

	public Tuning ApplyTuning ( EntityActionData _data, string _key )
	{
		Tuning tuning = m_tuningFor != null ? m_tuningFor(_data, _key) : new Tuning(1f, 1f);
		if (HasParameter(SpeedParameter))
			SetSpeed(tuning.speed);
		else
			tuning.speed = 1f;

		if (Layer >= 0)
			m_animator.SetLayerWeight(Layer, tuning.intensity);
		return tuning;
	}

	public bool Play ( EntityActionData _data, string _key, bool _isLeftHand )
	{
		if (m_animator == null || string.IsNullOrEmpty(_key))
			return false;

		ApplyTuning(_data, _key);
		Fire(_key, _isLeftHand);
		return true;
	}

	public void OnActionStarted ( object _owner )
	{
		if (m_owner != null && m_owner != _owner)
			Cancel();
	}

	public bool StartPreparation ( object _owner, EntityActionData _data, string _key, bool _isLeftHand, Timing _timing, float _seconds )
	{
		Stop();
		if (m_animator == null)
			return false;

		if (_timing.WindsUp && HasParameter(_key) && HasParameter(SpeedParameter))
		{
			Tuning tuning = ApplyTuning(_data, _key);
			Begin(_owner, _data, _key, _timing, tuning.speed, Phase.WindUp);
			float duration = Mathf.Max(c_minWindUp, _seconds * Mathf.Clamp(_timing.windUpRatio, .1f, 1f));
			m_windUpSpeed = Mathf.Min(2f * _timing.holdDelay / duration, c_maxWindUpBoost * Mathf.Max(.01f, tuning.speed));
			m_windUpDuration = 2f * _timing.holdDelay / m_windUpSpeed;
			SetSpeed(m_windUpSpeed);
			Fire(_key, _isLeftHand);
			m_ramp = DOVirtual.Float(m_windUpSpeed, 0f, m_windUpDuration, SetSpeed).SetEase(Ease.Linear);
			return true;
		}

		if (_timing.preparation == EntityActionData.PreparationAnimationMode.Trigger && HasParameter(_timing.preparationKey))
		{
			Tuning tuning = ApplyTuning(_data, _timing.preparationKey);
			Begin(_owner, _data, _key, _timing, tuning.speed, Phase.Preparation);
			Fire(_timing.preparationKey, _isLeftHand);
			return true;
		}

		return false;
	}

	public bool Strike ( object _owner, EntityActionData _data, string _key, bool _isLeftHand, Timing _timing, bool _holdsRecovery, out float _impactDelay )
	{
		_impactDelay = 0f;
		if (m_animator == null || string.IsNullOrEmpty(_key))
			return false;

		bool isResuming = m_phase == Phase.WindUp && m_owner == _owner && m_key == _key;
		float position = isResuming ? WindUpPosition() : 0f;
		KillTweens();
		Tuning tuning = ApplyTuning(_data, _key);
		if (!isResuming || IsTopIdle())
			Fire(_key, _isLeftHand);

		Begin(_owner, _data, _key, _timing, tuning.speed, Phase.Strike);
		float speed = Mathf.Max(.01f, tuning.speed);
		_impactDelay = Mathf.Max(0f, _timing.hitDelay - position) / speed;
		if (_holdsRecovery)
			ScheduleRecovery(_isLeftHand, position, speed, _impactDelay);
		return true;
	}

	public void EndAction ( object _owner )
	{
		if (_owner == null || m_owner != _owner)
			return;

		if (m_phase == Phase.Strike)
		{
			KillTweens();
			Clear();
		}
		else if (m_phase == Phase.Recovery && m_timing.recovery == EntityActionData.RecoveryAnimationMode.Hold)
			Stop();
		else
			Cancel();
	}

	public void Cancel ()
	{
		bool isHolding = IsHolding;
		Stop();
		if (isHolding && IsPlayable && HasIdleState())
			m_animator.CrossFadeInFixedTime(m_idleState, c_cancelFade, Layer);
	}

	public void Stop ()
	{
		bool isHolding = IsHolding;
		KillTweens();
		if (isHolding)
			SetSpeed(m_speed);
		Clear();
	}

	public bool IsTopIdle ()
	{
		return IsPlayable && HasIdleState() && m_animator.GetCurrentAnimatorStateInfo(Layer).shortNameHash == m_idleState && !m_animator.IsInTransition(Layer);
	}

	public bool HasAimState ( string _key, bool _isLeftHand )
	{
		return !string.IsNullOrEmpty(_key) && HasState(AimStateName(_key, _isLeftHand));
	}

	public bool PlayAim ( EntityActionData _data, string _key, bool _isLeftHand )
	{
		if (!IsPlayable || !HasAimState(_key, _isLeftHand))
			return false;

		int state = Animator.StringToHash(AimStateName(_key, _isLeftHand));
		Stop();
		ApplyTuning(_data, AimStatePrefix + _key);
		CrossFadeTo(state, c_aimFadeIn);
		m_aimState = state;
		return true;
	}

	public void ReleaseAim ()
	{
		int aimed = m_aimState;
		m_aimState = 0;
		if (aimed != 0 && IsPlayable && HasIdleState() && PlayingState() == aimed)
			m_animator.CrossFadeInFixedTime(m_idleState, c_aimFadeOut, Layer);
	}

	private void ScheduleRecovery ( bool _isLeftHand, float _position, float _speed, float _impactDelay )
	{
		if (m_timing.recovery == EntityActionData.RecoveryAnimationMode.Hold && HasParameter(SpeedParameter))
		{
			float distance = m_timing.recoveryDelay - Mathf.Max(m_timing.hitDelay, _position);
			if (distance <= 0f)
				return;

			float duration = 2f * distance / _speed;
			m_pending = DOVirtual.DelayedCall(_impactDelay, () =>
			{
				m_pending = null;
				m_phase = Phase.Recovery;
				m_ramp = DOVirtual.Float(_speed, 0f, duration, SetSpeed).SetEase(Ease.Linear);
			}, false);
			return;
		}

		if (m_timing.recovery == EntityActionData.RecoveryAnimationMode.Trigger && HasParameter(m_timing.recoveryKey))
		{
			EntityActionData data = m_data;
			string key = m_timing.recoveryKey;
			m_pending = DOVirtual.DelayedCall(Mathf.Max(0f, m_timing.recoveryDelay - _position) / _speed, () =>
			{
				m_pending = null;
				m_phase = Phase.Recovery;
				ApplyTuning(data, key);
				Fire(key, _isLeftHand);
			}, false);
		}
	}

	private float WindUpPosition ()
	{
		if (m_ramp == null || !m_ramp.IsActive())
			return m_timing.holdDelay;

		float elapsed = Mathf.Min(m_ramp.Elapsed(), m_windUpDuration);
		return Mathf.Min(m_timing.holdDelay, m_windUpSpeed * elapsed - m_windUpSpeed * elapsed * elapsed / (2f * m_windUpDuration));
	}

	private bool HasIdleState ()
	{
		if (m_hasIdleState == null && Layer >= 0)
			m_hasIdleState = m_animator.HasState(Layer, m_idleState);
		return m_hasIdleState == true;
	}

	private void Fire ( string _key, bool _isLeftHand )
	{
		if (HasParameter(LeftHandParameter))
			m_animator.SetBool(LeftHandParameter, _isLeftHand);
		if (HasParameter(_key))
			m_animator.SetTrigger(_key);
	}

	private void SetSpeed ( float _speed )
	{
		if (HasParameter(SpeedParameter))
			m_animator.SetFloat(SpeedParameter, _speed);
	}

	private void Begin ( object _owner, EntityActionData _data, string _key, Timing _timing, float _speed, Phase _phase )
	{
		m_owner = _owner;
		m_data = _data;
		m_key = _key;
		m_timing = _timing;
		m_speed = _speed;
		m_phase = _phase;
	}

	private void Clear ()
	{
		m_owner = null;
		m_data = null;
		m_key = null;
		m_phase = Phase.None;
	}

	private void KillTweens ()
	{
		if (m_ramp != null && m_ramp.IsActive())
			m_ramp.Kill();
		if (m_pending != null && m_pending.IsActive())
			m_pending.Kill();
		m_ramp = null;
		m_pending = null;
	}
}
