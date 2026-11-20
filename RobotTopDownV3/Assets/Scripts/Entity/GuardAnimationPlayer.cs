using UnityEngine;

public class GuardAnimationPlayer : AnimatorLayerDriver
{
	public const string StatePrefix = "Guard ";
	public const string IdleStateName = "Idle";
	public const string TorsoParameter = "TorsoTwist";
	public const float TorsoRange = 60f;
	private const float c_fade = .35f;
	private const float c_torsoSpeed = 150f;

	private readonly int m_idleState = Animator.StringToHash(IdleStateName);
	private string m_key;
	private bool m_isLeftHand;
	private int m_state;
	private int m_lastGuard;
	private float m_torsoTarget;

	public float Torso { get; private set; }
	public bool HasGuard => m_state != 0;

	public GuardAnimationPlayer ( Animator _animator ) : base(_animator, null)
	{
	}

	public static string StateName ( string _key, bool _isLeftHand )
	{
		return HandStateName(StatePrefix, _key, _isLeftHand);
	}

	public bool HasState ( string _key, bool _isLeftHand )
	{
		return !string.IsNullOrEmpty(_key) && HasState(StateName(_key, _isLeftHand));
	}

	public void Set ( string _key, bool _isLeftHand, float _torsoYaw )
	{
		if (Layer < 0)
			return;

		if (_key != m_key || _isLeftHand != m_isLeftHand)
		{
			m_key = _key;
			m_isLeftHand = _isLeftHand;
			m_state = HasState(_key, _isLeftHand) ? Animator.StringToHash(StateName(_key, _isLeftHand)) : 0;
		}

		m_torsoTarget = HasGuard && HasParameter(TorsoParameter) ? Mathf.Clamp(_isLeftHand ? -_torsoYaw : _torsoYaw, -TorsoRange, TorsoRange) : 0f;
	}

	public void Update ( float _deltaTime )
	{
		if (!IsPlayable)
			return;

		int playing = PlayingState();
		if (m_state != 0 && playing != m_state)
		{
			m_animator.CrossFadeInFixedTime(m_state, c_fade, Layer);
			m_lastGuard = m_state;
		}
		else if (m_state == 0 && m_lastGuard != 0 && playing == m_lastGuard && HasState(IdleStateName))
			m_animator.CrossFadeInFixedTime(m_idleState, c_fade, Layer);

		Torso = Mathf.MoveTowards(Torso, m_torsoTarget, c_torsoSpeed * _deltaTime);
		if (HasParameter(TorsoParameter))
			m_animator.SetFloat(TorsoParameter, Torso / TorsoRange);
	}
}
