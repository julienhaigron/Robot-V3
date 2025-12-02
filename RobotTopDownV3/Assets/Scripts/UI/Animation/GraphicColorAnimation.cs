using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphicColorAnimation : UIElementMonoBehaviour
{
	[SerializeField] private Graphic m_gfx;
	public Graphic Gfx => m_gfx;

	public Color originalColor = Color.white;
	[SerializeField] private SerializableDictionary<AnimationType, TweenConfig> m_animDictionary;
	public bool autoPlayOnEnable = false;
	[ShowIf("@autoPlayOnEnable")]
	public AnimationType onEnableAnimationType = AnimationType.AlphaSinLoop;
	[ShowIf("@!autoPlayOnEnable")]
	public bool tryToRestartPreviousAnimationOnEnable = false;

	private Tween m_tween;
	public Tween CurrentTween => m_tween;

	private TweenConfig m_currentConfig;
	public TweenConfig CurrentConfig => m_currentConfig;

	public bool IsPlaying => m_tween.IsActive() && m_tween.IsPlaying();
	public bool IsPlayingAnimation ( AnimationType _animType )
	{
		if (!IsPlaying)
			return false;
		else
			return CurrentConfig != null && CurrentConfig.animationType == _animType;
	}

	protected override void Awake ()
	{
		base.Awake();

#if UNITY_EDITOR
		foreach (KeyValuePair<AnimationType, TweenConfig> item in m_animDictionary)
		{
			if (item.Value.onTestPlay == null)
				item.Value.onTestPlay = PlayAnimation;
		}
#endif
	}

#if UNITY_EDITOR
	protected override void Reset ()
	{
		base.Reset();
		m_gfx = GetComponent<Graphic>();

		if (m_gfx != null)
			originalColor = m_gfx.color;

		ResetDictionary();
	}
#endif

	public void ResetDictionary ()
	{
		//WARNING reset clear all config to default (edit the dictionary manually in the inspector if you need to)
		m_animDictionary = new SerializableDictionary<AnimationType, TweenConfig>();
		m_animDictionary.Add(AnimationType.RedLimitedCountBlink, TweenConfig.RedLimitedCountBlink);
		m_animDictionary.Add(AnimationType.RedInfiniteBlink, TweenConfig.RedInfiniteBlink);
		m_animDictionary.Add(AnimationType.CustomColorInfiniteBlink, TweenConfig.CustomColorInfiniteBlink);
		m_animDictionary.Add(AnimationType.RedOpaqueFadeToOriginalColor, TweenConfig.RedOpaqueFadeToOriginalColor);
		m_animDictionary.Add(AnimationType.GreenOpaqueFadeToOriginalColor, TweenConfig.GreenOpaqueFadeToOriginalColor);
		m_animDictionary.Add(AnimationType.BlueOpaqueFadeToOriginalColor, TweenConfig.BlueOpaqueFadeToOriginalColor);
		m_animDictionary.Add(AnimationType.YellowOpaqueFadeToOriginalColor, TweenConfig.YellowOpaqueFadeToOriginalColor);
		m_animDictionary.Add(AnimationType.CustomColorOpaqueFadeToOriginalColor, TweenConfig.CustomColorOpaqueFadeToOriginalColor);
		m_animDictionary.Add(AnimationType.RainbowColorsInfiniteLoop, TweenConfig.RainbowColorsInfiniteLoop);
		m_animDictionary.Add(AnimationType.OriginalColorOpaqueToFadeOut, TweenConfig.OriginalColorOpaqueToFadeOut);
		m_animDictionary.Add(AnimationType.AlphaSinLoop, TweenConfig.AlphaSinLoop);
		m_animDictionary.Add(AnimationType.FadeOut, TweenConfig.FadeOut);
		m_animDictionary.Add(AnimationType.FadeIn, TweenConfig.FadeIn);
		m_animDictionary.Add(AnimationType.FadeToCustomColor0, TweenConfig.FadeToCustomColor0);
		m_animDictionary.Add(AnimationType.FadeToCustomColor1, TweenConfig.FadeToCustomColor1);
		m_animDictionary.Add(AnimationType.CustomTween, new TweenConfig());
		//Add here if new enum created
	}

	//even if you don't have canvas parent, this method is called on Enable too
	protected override void OnCanvasParentEnabled ()
	{
		base.OnCanvasParentEnabled();

		if (!gameObject.activeInHierarchy)
			return;

		if (autoPlayOnEnable)
			PlayAnimation(onEnableAnimationType);
		else if (m_currentConfig != null && tryToRestartPreviousAnimationOnEnable)
			PlayAnimation(m_currentConfig);
	}

	protected override void OnCanvasParentDisabled ()
	{
		base.OnCanvasParentDisabled();

		if (IsPlaying)
			m_tween.Kill(true);
	}

	public enum AnimationType
	{
		RedLimitedCountBlink,

		RedInfiniteBlink,
		CustomColorInfiniteBlink,

		RedOpaqueFadeToOriginalColor,
		GreenOpaqueFadeToOriginalColor,
		BlueOpaqueFadeToOriginalColor,
		YellowOpaqueFadeToOriginalColor,
		CustomColorOpaqueFadeToOriginalColor,

		RainbowColorsInfiniteLoop,

		OriginalColorOpaqueToFadeOut,
		AlphaSinLoop,
		FadeOut,
		FadeIn,
		FadeToCustomColor0,
		FadeToCustomColor1,
		CustomTween,
		//update Reset if new enum created (WARNING : be sure to add at last pos)
	}

	[System.Serializable]
	public class TweenConfig
	{
		public Action<TweenConfig, Color?> onTestPlay;

		[FoldoutGroup("Config")]
		public AnimationType animationType;
		[FoldoutGroup("Config")]
		public int loopCount = 1;
		[FoldoutGroup("Config")]
		[ShowIf("@loopCount != 1")]
		public LoopType loopType = LoopType.Yoyo;
		//[ShowIf("@loopCount != 1")]
		//public float loopIntervalDuration = 0f;
		[FoldoutGroup("Config")]
		public float duration = 0.5f;
		[FoldoutGroup("Config")]
		public bool ignoreTimeScale = true;
		[Space]
		[FoldoutGroup("Config")]
		public bool resetColorToOriginalAtStart = true;

		[FoldoutGroup("Config")]
		public bool animateAlphaOnly = false;
		[FoldoutGroup("Config")]
		[ShowIf("@!animateAlphaOnly")]
		public bool useGradient = false;
		[FoldoutGroup("Config")]
		[ShowIf("@!animateAlphaOnly && useGradient")]
		public Gradient colorOverTimeGradient;
		[FoldoutGroup("Config")]
		[ShowIf("@!animateAlphaOnly && !useGradient")]
		public Color targetColor = Color.red;

		[FoldoutGroup("Config")]
		[ShowIf("@animateAlphaOnly")]
		public bool useAlphaCurve = false;
		[FoldoutGroup("Config")]
		[ShowIf("@animateAlphaOnly && !useAlphaCurve")]
		public float targetAlpha = 0f;
		[FoldoutGroup("Config")]
		[ShowIf("@animateAlphaOnly && useAlphaCurve")]
		public AnimationCurve alphaOverTimeCurve;

		[FoldoutGroup("Config")]
		[ShowIf("@!useGradient")]
		public bool useCustomEaseCurve = false;
		[FoldoutGroup("Config")]
		[ShowIf("@!useGradient && useCustomEaseCurve")]
		public AnimationCurve customEaseCurve;
		[FoldoutGroup("Config")]
		[ShowIf("@!useGradient && !useCustomEaseCurve")]
		public Ease ease = Ease.Linear;

		[FoldoutGroup("Config")]
		[Button()]
		void TestOnlyInPlay ()
		{
			onTestPlay?.Invoke(this, targetColor);
		}

		public TweenConfig () { }

		public static TweenConfig GetOpaqueColorToOriginalColorConfig ( AnimationType _type, Color _targetColor )
		{
			return new TweenConfig()
			{
				animationType = _type,
				loopCount = 1,
				duration = 1f,
				ignoreTimeScale = true,
				targetColor = _targetColor,
				resetColorToOriginalAtStart = true,
				useCustomEaseCurve = true,
				animateAlphaOnly = false,
				customEaseCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0f, 1f, 0f, 0f, 0f, 0f), new Keyframe(1f, 0f, 0f, 0f, 0f, 0f) }),
			};
		}

		public TweenConfig ( TweenConfig _reference )
		{
			animationType = _reference.animationType;
			loopCount = _reference.loopCount;
			loopType = _reference.loopType;
			//loopIntervalDuration = _reference.loopIntervalDuration;
			duration = _reference.duration;
			ignoreTimeScale = _reference.ignoreTimeScale;
			resetColorToOriginalAtStart = _reference.resetColorToOriginalAtStart;

			animateAlphaOnly = _reference.animateAlphaOnly;
			useGradient = _reference.useGradient;
			colorOverTimeGradient = _reference.colorOverTimeGradient;
			targetColor = _reference.targetColor;

			useAlphaCurve = _reference.useAlphaCurve;
			targetAlpha = _reference.targetAlpha;
			alphaOverTimeCurve = _reference.alphaOverTimeCurve;

			useCustomEaseCurve = _reference.useCustomEaseCurve;
			customEaseCurve = _reference.customEaseCurve;
			ease = _reference.ease;
		}

		public static TweenConfig RedLimitedCountBlink
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.RedLimitedCountBlink,
					loopCount = 6,
					loopType = LoopType.Yoyo,
					//loopIntervalDuration = 0f,
					duration = 0.2f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = true,
					useGradient = false,
					animateAlphaOnly = false,
					targetColor = Color.red,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig RedInfiniteBlink
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.RedInfiniteBlink,
					loopCount = -1,
					loopType = LoopType.Yoyo,
					//loopIntervalDuration = 0f,
					duration = 0.5f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = true,
					useGradient = false,
					animateAlphaOnly = false,
					targetColor = Color.red,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig CustomColorInfiniteBlink
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.CustomColorInfiniteBlink,
					loopCount = -1,
					loopType = LoopType.Yoyo,
					//loopIntervalDuration = 0f,
					duration = 0.3f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = true,
					useGradient = false,
					animateAlphaOnly = false,
					targetColor = ExtendedColor.HexToColor("#FFDF00"),
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}
		public static TweenConfig RedOpaqueFadeToOriginalColor { get { return TweenConfig.GetOpaqueColorToOriginalColorConfig(AnimationType.RedOpaqueFadeToOriginalColor, Color.red); } }
		public static TweenConfig GreenOpaqueFadeToOriginalColor { get { return TweenConfig.GetOpaqueColorToOriginalColorConfig(AnimationType.GreenOpaqueFadeToOriginalColor, ExtendedColor.HexToColor("#00E900")); } }
		public static TweenConfig BlueOpaqueFadeToOriginalColor { get { return TweenConfig.GetOpaqueColorToOriginalColorConfig(AnimationType.BlueOpaqueFadeToOriginalColor, ExtendedColor.HexToColor("#009CFF")); } }
		public static TweenConfig YellowOpaqueFadeToOriginalColor { get { return TweenConfig.GetOpaqueColorToOriginalColorConfig(AnimationType.YellowOpaqueFadeToOriginalColor, ExtendedColor.HexToColor("#FFDF00")); } }
		public static TweenConfig CustomColorOpaqueFadeToOriginalColor { get { return TweenConfig.GetOpaqueColorToOriginalColorConfig(AnimationType.CustomColorOpaqueFadeToOriginalColor, ExtendedColor.HexToColor("#FFDF00")); } }
		public static TweenConfig RainbowColorsInfiniteLoop
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.RainbowColorsInfiniteLoop,
					loopCount = -1,
					duration = 1f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = true,
					useGradient = true,
					colorOverTimeGradient = null, //GameConfig.current.ui.defaultRainbowGradient,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig OriginalColorOpaqueToFadeOut
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.OriginalColorOpaqueToFadeOut,
					loopCount = 1,
					duration = 1f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = true,
					useGradient = false,
					animateAlphaOnly = true,
					targetAlpha = 0f,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig FadeOut
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.FadeOut,
					loopCount = 1,
					duration = 0.5f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = false,
					useGradient = false,
					animateAlphaOnly = true,
					useAlphaCurve = false,
					targetAlpha = 0f,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig AlphaSinLoop
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.AlphaSinLoop,
					loopCount = -1,
					loopType = LoopType.Yoyo,
					duration = 1f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = true,
					useGradient = false,
					animateAlphaOnly = true,
					useAlphaCurve = true,
					alphaOverTimeCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0f, 0.2f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f) }),
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig FadeIn
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.FadeIn,
					loopCount = 1,
					duration = 0.5f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = false,
					useGradient = false,
					animateAlphaOnly = true,
					useAlphaCurve = false,
					targetAlpha = 1f,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig FadeToCustomColor0
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.FadeToCustomColor0,
					loopCount = 1,
					duration = 0.5f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = false,
					useGradient = false,
					animateAlphaOnly = false,
					targetColor = Color.white,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}

		public static TweenConfig FadeToCustomColor1
		{
			get
			{
				return new TweenConfig()
				{
					animationType = AnimationType.FadeToCustomColor1,
					loopCount = 1,
					duration = 0.5f,
					ignoreTimeScale = true,
					resetColorToOriginalAtStart = false,
					useGradient = false,
					animateAlphaOnly = false,
					targetColor = Color.grey,
					useCustomEaseCurve = false,
					ease = Ease.Linear,
				};
			}
		}
	}

	public void PlayAnimation ( AnimationType _animationType )
	{
		PlayAnimation(m_animDictionary[_animationType], null, null);
	}

	public void PlayAnimation ( AnimationType _animationType, Action _onComplete, Color? _customTargetColor = null )
	{
		PlayAnimation(m_animDictionary[_animationType], _onComplete, _customTargetColor);
	}

	public void PlayAnimation ( TweenConfig _config, Color? _customTargetColor = null )
	{
		PlayAnimation(_config, null, _customTargetColor);
	}

	public void PlayAnimation ( TweenConfig _config, Action _onComplete, Color? _customTargetColor = null )
	{
		if (m_tween.IsActive())
			m_tween.Kill();
		m_currentConfig = _config;

		if (_config.resetColorToOriginalAtStart)
			m_gfx.color = originalColor;

		if (_config.useGradient)
			m_tween = DOVirtual.Float(0f, 1f, _config.duration, UpdateColorFromGradient);
		else if (_config.animateAlphaOnly)
		{
			if (_config.useAlphaCurve)
				m_tween = DOVirtual.Float(0f, 1f, _config.duration, UpdateAlphaFromCurve);
			else
				m_tween = m_gfx.DOFade(_config.targetAlpha, _config.duration);
		}
		else
			m_tween = m_gfx.DOColor(_customTargetColor ?? _config.targetColor, _config.duration);

		if (_config.useCustomEaseCurve)
			m_tween.SetEase(_config.customEaseCurve);
		else
			m_tween.SetEase(_config.ease);
		m_tween.SetLoops(_config.loopCount, _config.loopType);
		m_tween.SetUpdate(_config.ignoreTimeScale);

		if (_config.loopCount > 0 && _onComplete != null)
		{
			m_tween.OnComplete(() =>
			{
				m_currentConfig = null;
				_onComplete.Invoke();
			});
		}
	}

	void UpdateColorFromGradient ( float _x )
	{
		m_gfx.color = m_currentConfig.colorOverTimeGradient.Evaluate(_x);
	}

	void UpdateAlphaFromCurve ( float _x )
	{
		m_gfx.color = new Color(m_gfx.color.r, m_gfx.color.g, m_gfx.color.b, m_currentConfig.alphaOverTimeCurve.Evaluate(_x));
	}

	public void ResetColor ( float _duration = 0.5f )
	{
		if (m_tween.IsActive())
			m_tween.Kill();
		m_currentConfig = null;

		if (_duration == 0f)
			m_gfx.color = originalColor;
		else
			m_tween = m_gfx.DOColor(originalColor, _duration).SetEase(Ease.Linear).SetUpdate(true);
	}

	[Button()]
	void ResetColorFade ()
	{
		ResetColor(0.5f);
	}
}