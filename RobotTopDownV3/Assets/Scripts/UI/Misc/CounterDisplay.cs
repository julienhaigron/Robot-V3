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
	[Title("Text Refs only")]
	[SerializeField] private TMP_Text[] m_counterTextArray;

	[FoldoutGroup("Particles")]
	public bool useParticleSystems = true;
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

	public void SetOutline ( Color _color, float _width )
	{
		for (int i = 0; i < m_counterTextArray.Length; i++)
		{
			//Reading fontMaterial makes TMP instance the material, so the outline stays on this text only
			m_counterTextArray[i].outlineColor = _color;
			m_counterTextArray[i].outlineWidth = _width;
		}
	}

	public void SetRawText ( string _text )
	{
		m_textTween?.Kill();
		CounterText = _text;
	}

	void StopAllAnimation ()
	{
		m_textTween?.Kill(true);
		m_currentValue = m_finalValue;
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

		float diff = Mathf.Abs(_value - m_finalValue);
		if (m_currentAndFinalValuesInitialized && diff == 0) return;

		m_isIncreasing = (_value - m_finalValue) > 0;
		bool playPS = _playParticles ?? animateOnUpdate;

		bool diffIsEnoughToLaunchLoopUpdate = false;

		if (diff > minDiffToLoopUpdate)
			diffIsEnoughToLaunchLoopUpdate = (diff / (Mathf.Abs(m_finalValue) + 1)) > minDiffRatioToLoopUpdate;

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
			}
			_onUpdateComplete?.Invoke();
		}
		else 
		{
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
			else
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
		}
	}

#if UNITY_EDITOR
	[FoldoutGroup("Debug")]
	[Button()]
	void UpdateDebugValue ( float _value )
	{
		UpdateValue(_value);
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