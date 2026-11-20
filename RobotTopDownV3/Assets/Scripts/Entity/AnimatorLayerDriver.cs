using System.Collections.Generic;
using UnityEngine;

public abstract class AnimatorLayerDriver
{
	protected readonly Animator m_animator;
	private readonly string m_layerName;
	private int m_layer = -1;
	private bool m_isLayerResolved;
	private HashSet<string> m_parameters;

	protected AnimatorLayerDriver ( Animator _animator, string _layerName )
	{
		m_animator = _animator;
		m_layerName = _layerName;
	}

	public static string HandStateName ( string _prefix, string _key, bool _isLeftHand )
	{
		return _prefix + _key + (_isLeftHand ? " L" : " R");
	}

	protected bool IsAnimatorReady => m_animator != null && m_animator.runtimeAnimatorController != null && m_animator.isInitialized;

	protected bool IsPlayable => Layer >= 0 && m_animator.isActiveAndEnabled;

	public int Layer
	{
		get
		{
			if (!m_isLayerResolved && IsAnimatorReady)
			{
				m_layer = string.IsNullOrEmpty(m_layerName) ? 0 : m_animator.GetLayerIndex(m_layerName);
				m_isLayerResolved = true;
			}

			return m_layer;
		}
	}

	public bool HasParameter ( string _name )
	{
		if (string.IsNullOrEmpty(_name) || !IsAnimatorReady)
			return false;

		if (m_parameters == null)
		{
			m_parameters = new HashSet<string>();
			foreach (AnimatorControllerParameter parameter in m_animator.parameters)
				m_parameters.Add(parameter.name);
		}

		return m_parameters.Contains(_name);
	}

	public bool HasState ( string _name )
	{
		return !string.IsNullOrEmpty(_name) && Layer >= 0 && m_animator.HasState(Layer, Animator.StringToHash(_name));
	}

	protected int PlayingState ()
	{
		return m_animator.IsInTransition(Layer) ? m_animator.GetNextAnimatorStateInfo(Layer).shortNameHash : m_animator.GetCurrentAnimatorStateInfo(Layer).shortNameHash;
	}

	protected void CrossFadeTo ( int _state, float _fade )
	{
		if (PlayingState() != _state)
			m_animator.CrossFadeInFixedTime(_state, _fade, Layer);
	}
}
