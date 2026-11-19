using UnityEngine;

public class GuardAnimationPlayer
{
	public const string StatePrefix = "Guard ";
	public const string IdleStateName = "Idle";
	public const string TorsoParameter = "TorsoTwist";
	public const float TorsoRange = 60f;
	private const int c_layer = 0;
	private const float c_fade = .35f;
	private const float c_torsoSpeed = 150f;

	private readonly Animator m_animator;
	private readonly int m_idleState = Animator.StringToHash(IdleStateName);
	private bool m_isChecked;
	private bool m_hasIdle;
	private bool m_hasTorso;
	private string m_key;
	private bool m_isLeftHand;
	private int m_state;
	private int m_lastGuard;
	private float m_torsoTarget;

	public float Torso { get; private set; }
	public bool HasGuard => m_state != 0;

	public GuardAnimationPlayer ( Animator _animator )
	{
		m_animator = _animator;
	}

	public static string StateName ( string _key, bool _isLeftHand )
	{
		return StatePrefix + _key + (_isLeftHand ? " L" : " R");
	}

	public bool HasState ( string _key, bool _isLeftHand )
	{
		return IsReady() && !string.IsNullOrEmpty(_key) && m_animator.HasState(c_layer, Animator.StringToHash(StateName(_key, _isLeftHand)));
	}

	public void Set ( string _key, bool _isLeftHand, float _torsoYaw )
	{
		if (!IsReady())
			return;

		if (_key != m_key || _isLeftHand != m_isLeftHand)
		{
			m_key = _key;
			m_isLeftHand = _isLeftHand;
			m_state = HasState(_key, _isLeftHand) ? Animator.StringToHash(StateName(_key, _isLeftHand)) : 0;
		}

		m_torsoTarget = HasGuard && m_hasTorso ? Mathf.Clamp(_isLeftHand ? -_torsoYaw : _torsoYaw, -TorsoRange, TorsoRange) : 0f;
	}

	public void Update ( float _deltaTime )
	{
		if (!IsReady() || !m_animator.isActiveAndEnabled)
			return;

		int playing = m_animator.IsInTransition(c_layer) ? m_animator.GetNextAnimatorStateInfo(c_layer).shortNameHash : m_animator.GetCurrentAnimatorStateInfo(c_layer).shortNameHash;
		if (m_state != 0 && playing != m_state)
		{
			m_animator.CrossFadeInFixedTime(m_state, c_fade, c_layer);
			m_lastGuard = m_state;
		}
		else if (m_state == 0 && m_hasIdle && m_lastGuard != 0 && playing == m_lastGuard)
			m_animator.CrossFadeInFixedTime(m_idleState, c_fade, c_layer);

		Torso = Mathf.MoveTowards(Torso, m_torsoTarget, c_torsoSpeed * _deltaTime);
		if (m_hasTorso)
			m_animator.SetFloat(TorsoParameter, Torso / TorsoRange);
	}

	private bool IsReady ()
	{
		if (m_isChecked)
			return true;

		if (m_animator == null || m_animator.runtimeAnimatorController == null || !m_animator.isInitialized)
			return false;

		m_isChecked = true;
		m_hasIdle = m_animator.HasState(c_layer, m_idleState);
		foreach (AnimatorControllerParameter parameter in m_animator.parameters)
			m_hasTorso |= parameter.name == TorsoParameter && parameter.type == AnimatorControllerParameterType.Float;
		return true;
	}
}
