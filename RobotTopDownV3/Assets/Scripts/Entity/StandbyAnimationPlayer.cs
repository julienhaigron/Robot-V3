using UnityEngine;

public class StandbyAnimationPlayer : AnimatorLayerDriver
{
	public const string LayerName = "Standby";

	private float m_appliedWeight;

	public float Weight { get; set; }

	public StandbyAnimationPlayer ( Animator _animator ) : base(_animator, LayerName)
	{
	}

	public void Update ()
	{
		if (!IsPlayable || Mathf.Approximately(m_appliedWeight, Weight))
			return;

		m_animator.SetLayerWeight(Layer, Weight);
		m_appliedWeight = Weight;
	}
}
