using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CounterDisplayBubble : UIElementMonoBehaviour
{
	[SerializeField] private ARectTransformVisibility m_rtv;
	[SerializeField] private float m_lifeTime = 2f;
	[SerializeField] private CounterDisplay m_counter;
	[SerializeField] private RectTransformAnimation m_rectAnimation;
	[SerializeField] private Canvas m_canvas;
	[SerializeField] private bool m_setBubbleTextColor = false;
	[ShowIf("m_setBubbleTextColor")]
	[SerializeField] private Color m_positiveColor = Color.green;
	[ShowIf("m_setBubbleTextColor")]
	[SerializeField] private Color m_negativeColor = Color.red;

	private float m_lastDiff;


	private float m_currentLifeTime;
	private BubbleState m_state;
	private enum BubbleState
	{
		Hidden,
		Show,
		Idle,
		Hide
	}

	protected override void OnCanvasParentEnabled ()
	{
		base.OnCanvasParentEnabled();
		m_rtv.SetPosIndex(0, 0f);
		m_state = BubbleState.Hidden;
		m_canvas.enabled = false;
	}

	protected override void OnCanvasParentDisabled ()
	{
		base.OnCanvasParentDisabled();
		StopAllCoroutines();
		m_rtv.SetPosIndex(0, 0f);
		m_state = BubbleState.Hidden;
		m_canvas.enabled = false;
	}

	public void ShowBubble ( bool _resetLastDiff, float _diff, string _prefix = null, string _suffix = null, bool? _useEngineeringNotation = null, string _stringFormat = "0" )
	{
		if (m_state == BubbleState.Hidden || _resetLastDiff)
			m_lastDiff = 0f;

		float diff = _diff + m_lastDiff;

		if (diff >= 0)
			_prefix = "+";

		if (m_setBubbleTextColor)
			m_counter.TextColor = diff >= 0 ? m_positiveColor : m_negativeColor;

		m_counter.SetValueInstant(diff, true, _prefix, _suffix, _useEngineeringNotation, _stringFormat);

		m_lastDiff = diff;

		switch (m_state)
		{
			default:
			case BubbleState.Hide:
			case BubbleState.Hidden:
				StopAllCoroutines();
				StartCoroutine(BubbleCR());
				break;

			case BubbleState.Show:
				m_rectAnimation.DoPunchScale();
				break;

			case BubbleState.Idle:
				m_rectAnimation.DoPunchScale();
				m_currentLifeTime = m_lifeTime;
				break;
		}
	}

	IEnumerator BubbleCR ()
	{
		m_canvas.enabled = true;
		m_state = BubbleState.Show;

		m_rtv.SetPosIndex(0, 0f);
		m_currentLifeTime = m_lifeTime;
		m_rtv.SetPosIndex(1);

		while (m_rtv.IsPlaying)
			yield return null;

		m_state = BubbleState.Idle;

		while (m_currentLifeTime > 0f)
		{
			m_currentLifeTime -= Time.unscaledDeltaTime;
			yield return null;
		}

		m_state = BubbleState.Hide;
		m_rtv.SetPosIndex(2);

		while (m_rtv.IsPlaying)
			yield return null;

		m_state = BubbleState.Hidden;
		m_canvas.enabled = false;
	}
}