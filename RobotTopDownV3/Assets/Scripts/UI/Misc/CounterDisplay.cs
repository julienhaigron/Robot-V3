//using AssetKits.ParticleImage;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class CounterDisplay : MonoBehaviour
{
	[Title("Text Refs only")] //note don't need to add the refs of the icons they are set on start
	[SerializeField] private TMP_Text[] m_counterTextArray;

	public List<RectTransformAnimation> rectTfmAnimationList;
	public List<GraphicColorAnimation> graphicColorAnimationList;

	[FoldoutGroup("Particles")]
	public bool useParticleSystems = true;
	/*[FoldoutGroup("Particles")]
	public ParticleImage loopUpdatePI;
	[FoldoutGroup("Particles")]
	public ParticleImage burstUpdatePI;*/
	[FoldoutGroup("Particles")]
	public ParticleSystem loopUpdatePS;
	[FoldoutGroup("Particles")]
	public ParticleSystem burstUpdatePS;
	[FoldoutGroup("Particles")]
	public bool playPSOnlyWhenIncreasing = true;

	[FoldoutGroup("Value Animation Config")]
	public bool forceBurstUpdate = false;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("@!forceBurstUpdate ")]
	public float minDiffToLoopUpdate = 5;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("@!forceBurstUpdate ")]
	public float minDiffRatioToLoopUpdate = 0.05f;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("@!forceBurstUpdate ")]
	public float loopUpdateTicIncrementDuration = 0.01f;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("@!forceBurstUpdate ")]
	public Vector2 loopUpdateMinMaxDuration = new Vector2(0.5f, 1.5f);
	[FoldoutGroup("Value Animation Config")]
	public bool animateOnUpdate = true;//bounce
	[FoldoutGroup("Value Animation Config")]
	public bool animateOnlyWhenIncreasing = true;
	[FoldoutGroup("Value Animation Config")]
	public bool bounceOnLoopUpdateFinished = true;

	[FoldoutGroup("Value Animation Config")]
	public bool animateColorOnUpdate = false;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("animateColorOnUpdate")]
	public GraphicColorAnimation.AnimationType onBurstUpdateColorAnimation = GraphicColorAnimation.AnimationType.CustomColorOpaqueFadeToOriginalColor;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("animateColorOnUpdate")]
	public GraphicColorAnimation.AnimationType onLoopUpdateColorAnimation = GraphicColorAnimation.AnimationType.CustomColorInfiniteBlink;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("animateColorOnUpdate")]
	public bool useBlinkCustomColor = true;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("@animateColorOnUpdate && useBlinkCustomColor")]
	public Color blinkCustomColor = ExtendedColor.HexToColor("#8ED854");

	[FoldoutGroup("Value Animation Config")]
	public bool onlyBlinkText = true;
	[FoldoutGroup("Value Animation Config")]
	public bool onlyShakeText = true;
	[FoldoutGroup("Value Animation Config")]
	public bool showCounterBubble = false;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("showCounterBubble")] public bool showChangeBubbleOnlyWhenIncreasing = true;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("showCounterBubble")] public bool bubbleShowStackedValue = true;
	[FoldoutGroup("Value Animation Config")]
	[ShowIf("showCounterBubble")]
	[SerializeField] private CounterDisplayBubble m_counterBubble;

	[Title("Text Config")]
	public string defaultPrefix;
	public string defaultSuffix;
	public bool useEngineerNotationPerDefault;
	public string defaultStringFormat = "0";

	private bool m_isIncreasing;
	private float m_lastUpdateCallTime;
	private bool m_currentAndFinalValuesInitialized = false;
	private StringBuilder m_sb = new StringBuilder();

	public TMP_Text[] CounterTextArray => m_counterTextArray;
	private string CounterText
	{
		get
		{
			return m_counterTextArray[0].text;
		}
		set
		{
			for (int i = 0; i < m_counterTextArray.Length; i++)
			{
				m_counterTextArray[i].text = value;
			}
		}
	}

	public Color TextColor
	{
		get
		{
			return m_counterTextArray[0].color;
		}
		set
		{
			for (int i = 0; i < m_counterTextArray.Length; i++)
			{
				m_counterTextArray[i].color = value;
			}
		}
	}
	private Tweener m_textTween;
	private float m_currentValue = 0f;
	private float m_finalValue = 0f;
	public float CurrentValue => m_currentValue;
	public float FinalValue => m_finalValue;

	public bool TextTweenIsPlaying
	{
		get
		{
			if (!m_textTween.IsActive())
				return false;
			else
				return m_textTween.IsPlaying();
		}
	}

	public void SetValueInstant ( float _value, bool _resetAnim = true, string _prefix = null, string _suffix = null, bool? _useEngineeringNotation = null, string _stringFormat = "" )
	{
		if (_resetAnim)
			StopAllAnimation();
		ApplyTextToCounterText(_value, _prefix, _suffix, _useEngineeringNotation, _stringFormat);
		m_finalValue = _value;
		m_currentValue = _value;
		m_currentAndFinalValuesInitialized = true;
	}

	void StopAllAnimation ()
	{
		m_textTween?.Kill(true);
		m_currentValue = m_finalValue;
		for (int i = 0; i < rectTfmAnimationList.Count; i++)
		{
			rectTfmAnimationList[i].UpdateLoopStatus();
		}
		ApplyTextToCounterText(m_currentValue);
	}

	#region Apply on Text
	public void ApplyTextToCounterText ( float _value, string _prefix = null, string _suffix = null, bool? _useEngineeringNotation = null, string _stringFormat = "" )
	{
		if (string.IsNullOrEmpty(_stringFormat))
			_stringFormat = defaultStringFormat;

		m_sb.Clear();
		m_sb.Append(_prefix ?? defaultPrefix);

		if (_useEngineeringNotation ?? useEngineerNotationPerDefault)
			m_sb.Append(_value.ToString());
			//m_sb.Append(MathHelper.ConvertToEngineeringNotation((long)_value));
		else
			m_sb.Append(_value.ToString(_stringFormat));

		m_sb.Append(_suffix ?? defaultSuffix);
		CounterText = m_sb.ToString();
	}
	#endregion

	public void UpdateValue ( float _value, float? _duration = null, bool? _playParticles = null, string _prefix = null, string _suffix = null, bool? _useEngineeringNotation = null, string _stringFormat = "", Action _onUpdateComplete = null, bool _forceBurstUpdate = false )
	{
		float intervalBetweenUpdateCall = Time.unscaledTime - m_lastUpdateCallTime;
		m_lastUpdateCallTime = Time.unscaledTime;
		//0.1f

		float diff = Mathf.Abs(_value - m_finalValue);
		if (m_currentAndFinalValuesInitialized && diff == 0) return;

		m_isIncreasing = (_value - m_finalValue) > 0;
		bool playPS = _playParticles ?? animateOnUpdate;

		//BUBBLE
		if (showCounterBubble)
		{
			if (!showChangeBubbleOnlyWhenIncreasing || m_isIncreasing)
				m_counterBubble.ShowBubble(!bubbleShowStackedValue, _value - m_finalValue);
		}

		bool diffIsEnoughToLaunchLoopUpdate = false;

		if (diff > minDiffToLoopUpdate)
			diffIsEnoughToLaunchLoopUpdate = (diff / (Mathf.Abs(m_finalValue) + 1)) > minDiffRatioToLoopUpdate;

		//BURST UPDATE
		if (_forceBurstUpdate || ((!diffIsEnoughToLaunchLoopUpdate || forceBurstUpdate) && (_duration == null || _duration == 0f) && intervalBetweenUpdateCall > 0.1f))
		{
			StopAllAnimation();
			ApplyTextToCounterText(_value, _prefix, _suffix, _useEngineeringNotation, _stringFormat);
			if (animateOnUpdate)
			{
				if (burstUpdatePS != null)
				{
					if (useParticleSystems && playPS && (!playPSOnlyWhenIncreasing || m_isIncreasing))
					{
						burstUpdatePS.Stop();
						burstUpdatePS.Play();
					}
				}
				/*else if (burstUpdatePI != null)
				{
					if (useParticleSystems && playPS && (!playPSOnlyWhenIncreasing || m_isIncreasing))
					{
						burstUpdatePI.Stop();
						burstUpdatePI.Play();
					}
				}*/

				if (!animateOnlyWhenIncreasing || m_isIncreasing)
				{
					DoPunchScale();

					if (animateColorOnUpdate)
						PlayColorAnimation(onBurstUpdateColorAnimation, useBlinkCustomColor ? blinkCustomColor : null);
				}
			}
			_onUpdateComplete?.Invoke();
		}
		else //LOOP UPDATE
		{
			//already in loop
			if (TextTweenIsPlaying)
			{
				m_textTween.Kill();
				float duration = diff * loopUpdateTicIncrementDuration;
				duration = Mathf.Clamp(duration, loopUpdateMinMaxDuration.x, loopUpdateMinMaxDuration.y);

				float finalDuration = _duration ?? duration;
				m_textTween = DOVirtual.Float(m_currentValue, _value, finalDuration, ( x ) =>
				{
					m_currentValue = x;
					ApplyTextToCounterText(x, _prefix, _suffix, _useEngineeringNotation, _stringFormat);
				}).SetUpdate(true).SetEase(Ease.Linear).OnComplete(() =>
				{
					OnCompleteLoopUpdate();
					_onUpdateComplete?.Invoke();
				});
			}
			else //startLoop
			{
				StopAllAnimation();

				if (animateOnUpdate)
				{
					if (loopUpdatePS != null)
					{
						if (useParticleSystems && playPS && (!playPSOnlyWhenIncreasing || m_isIncreasing))
						{
							loopUpdatePS.Stop();
							loopUpdatePS.Play();
						}
					}
					/*else if (loopUpdatePI != null)
					{
						if (useParticleSystems && playPS && (!playPSOnlyWhenIncreasing || m_isIncreasing))
						{
							loopUpdatePI.Stop();
							loopUpdatePI.Play();
						}
					}*/

					if (!animateOnlyWhenIncreasing || m_isIncreasing)
					{
						for (int i = 0; i < rectTfmAnimationList.Count; i++)
						{
							rectTfmAnimationList[i].SetScaleLoopActive(true);
						}

						if (animateColorOnUpdate)
							PlayColorAnimation(onLoopUpdateColorAnimation, useBlinkCustomColor ? blinkCustomColor : null);
					}
				}

				float duration = diff * loopUpdateTicIncrementDuration;
				duration = Mathf.Clamp(duration, loopUpdateMinMaxDuration.x, loopUpdateMinMaxDuration.y);

				float finalDuration = _duration ?? duration;
				m_textTween = DOVirtual.Float(m_currentValue, _value, finalDuration, ( x ) =>
				{
					m_currentValue = x;
					ApplyTextToCounterText(x, _prefix, _suffix, _useEngineeringNotation, _stringFormat);
				}).SetUpdate(true).SetEase(Ease.Linear).OnComplete(() =>
				{
					OnCompleteLoopUpdate();
					_onUpdateComplete?.Invoke();
				});
			}
		}

		m_finalValue = _value;
		m_currentAndFinalValuesInitialized = true;
	}

	void OnCompleteLoopUpdate ()
	{
		if (animateOnUpdate)
		{
			if (loopUpdatePS != null && useParticleSystems && loopUpdatePS.isPlaying)
				loopUpdatePS.Stop();
			/*if (loopUpdatePI != null && useParticleSystems)
				loopUpdatePI.Stop();*/

			for (int i = 0; i < rectTfmAnimationList.Count; i++)
			{
				rectTfmAnimationList[i].SetScaleLoopActive(false);
			}

			if (animateColorOnUpdate)
				ResetColorAnimation();
		}

		if (bounceOnLoopUpdateFinished)
		{
			if (!animateOnlyWhenIncreasing || m_isIncreasing)
				DoPunchScale();
		}
	}

	public void DoPunchScale ()
	{
		for (int i = 0; i < rectTfmAnimationList.Count; i++)
		{
			rectTfmAnimationList[i].DoPunchScale();
		}
	}

	public void DoScale ()
	{
		for (int i = 0; i < rectTfmAnimationList.Count; i++)
		{
			rectTfmAnimationList[i].DoScale();
		}
	}

	public void DoAnchorPos ()
	{
		for (int i = 0; i < rectTfmAnimationList.Count; i++)
		{
			rectTfmAnimationList[i].DoAnchorPos();
		}
	}

	public void DoPunchAnchorPosition ()
	{
		for (int i = 0; i < rectTfmAnimationList.Count; i++)
		{
			rectTfmAnimationList[i].DoPunchAnchorPosition();
		}
	}

	public void PlayColorAnimation ( GraphicColorAnimation.AnimationType _animationType, Color? _customTargetColor = null )
	{
		//just set enable
		for (int i = 0; i < graphicColorAnimationList.Count; i++)
		{
			if (onlyBlinkText && i > 0) break;

			graphicColorAnimationList[i].PlayAnimation(_animationType, null, _customTargetColor);
		}
	}

	public void DoShakeBlink ( Color? _blinkColor = null )
	{
		for (int i = 0; i < graphicColorAnimationList.Count; i++)
		{
			if (onlyBlinkText && i > 0) break;

			graphicColorAnimationList[i].PlayAnimation(GraphicColorAnimation.AnimationType.RedLimitedCountBlink, null, _blinkColor);
		}

		for (int i = 0; i < rectTfmAnimationList.Count; i++)
		{
			if (onlyShakeText && i > 0) break;

			rectTfmAnimationList[i].DoPunchAnchorPosition();
		}
	}

	public bool ColorAnimationIsPlaying ()
	{
		foreach (GraphicColorAnimation anim in graphicColorAnimationList)
		{
			if (anim.IsPlaying)
				return true;
		}
		return false;
	}

	public void ResetColorAnimation ( float _duration = 0.5f )
	{
		for (int i = 0; i < graphicColorAnimationList.Count; i++)
		{
			if (onlyBlinkText && i > 0) break;

			graphicColorAnimationList[i].ResetColor(_duration);
		}
	}

#if UNITY_EDITOR
	[FoldoutGroup("Debug")]
	[Button()]
	void UpdateDebugValue ( float _value )
	{
		UpdateValue(_value);
	}

	[FoldoutGroup("Debug")]
	[Button()]
	void TestColorAnimation ( GraphicColorAnimation.AnimationType _animationType, Color? _customTargetColor = null )
	{
		PlayColorAnimation(_animationType, _customTargetColor);
	}

	[FoldoutGroup("Debug")]
	[Button()]
	void TestShakeBlink ()
	{
		DoShakeBlink();
	}

	private void Reset ()
	{
		if (m_counterTextArray == null || m_counterTextArray.Length == 0)
		{
			m_counterTextArray = new TMP_Text[1];
			m_counterTextArray[0] = GetComponent<TMP_Text>();
		}
	}
#endif
}