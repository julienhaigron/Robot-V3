using System.Collections.Generic;
using UnityEngine;

public class LocomotionAnimationPlayer : AnimatorLayerDriver
{
	public const string LayerName = "Locomotion";
	public const string StatePrefix = "Move ";
	public const string RestStateName = "Default";
	public const string MovingParameter = "IsMoving";
	public const string GroundedParameter = "IsGrounded";
	public const string WalkStyle = "Walk";
	public const string RunStyle = "Run";
	public const string FlyStyle = "Fly";
	private const float c_fade = .25f;

	private readonly int m_restState = Animator.StringToHash(RestStateName);
	private readonly Dictionary<string, int> m_styleStates = new();
	private bool? m_isMoving;
	private bool? m_isFlying;
	private int m_target;
	private int m_lastMove;

	public LocomotionAnimationPlayer ( Animator _animator ) : base(_animator, LayerName)
	{
	}

	public static string StateName ( string _style )
	{
		return StatePrefix + _style;
	}

	public bool HasStyle ( string _style )
	{
		return StyleState(_style) != 0;
	}

	public void Set ( bool _isMoving, string _style, bool _isFlying )
	{
		if (!IsAnimatorReady)
			return;

		if (m_isMoving != _isMoving)
		{
			m_isMoving = _isMoving;
			if (HasParameter(MovingParameter))
				m_animator.SetBool(MovingParameter, _isMoving);
		}

		if (m_isFlying != _isFlying)
		{
			m_isFlying = _isFlying;
			if (HasParameter(GroundedParameter))
				m_animator.SetBool(GroundedParameter, !_isFlying);
		}

		int fly = _isFlying ? StyleState(FlyStyle) : 0;
		int style = string.IsNullOrEmpty(_style) ? 0 : StyleState(_style);
		m_target = fly != 0 ? fly : !_isMoving ? 0 : style != 0 ? style : StyleState(WalkStyle);
	}

	public void Update ()
	{
		if (!IsPlayable)
			return;

		if (m_target != 0)
		{
			CrossFadeTo(m_target, c_fade);
			m_lastMove = m_target;
		}
		else if (m_lastMove != 0)
		{
			if (PlayingState() == m_lastMove && HasState(RestStateName))
				m_animator.CrossFadeInFixedTime(m_restState, c_fade, Layer);
			m_lastMove = 0;
		}
	}

	private int StyleState ( string _style )
	{
		if (string.IsNullOrEmpty(_style) || Layer < 0)
			return 0;

		if (!m_styleStates.TryGetValue(_style, out int state))
		{
			int hash = Animator.StringToHash(StateName(_style));
			state = m_animator.HasState(Layer, hash) ? hash : 0;
			m_styleStates.Add(_style, state);
		}

		return state;
	}
}
