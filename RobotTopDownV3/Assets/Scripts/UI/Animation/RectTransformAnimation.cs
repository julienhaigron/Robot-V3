using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RectTransformAnimation : UIElementMonoBehaviour
{

#if UNITY_EDITOR
	[GUIColor(nameof(GetEnumColor))]
#endif
	[SerializeField] protected AnimationType m_animsDisplayed = AnimationType.None;

#if UNITY_EDITOR
	[ShowIf(nameof(IsRectTfm))]
#endif
	[SerializeField] protected RectTransform m_rectTfm;//use Onlu when in UI

	[SerializeField] protected bool m_preventPosChange = false; //enable if pos already handled by layoutgroup parent
	[SerializeField] protected bool m_setPreventPosAtAwake = true;

	protected Transform AnimatedTfm => m_rectTfm == null ? transform : m_rectTfm.transform;

	[Flags]
	public enum AnimationType
	{
		Everything = ~0,
		LoopScale = 1 << 0,
		LoopPos = 1 << 1,
		LoopRot = 1 << 2,
		OneShotPunchScale = 1 << 3,
		OneShotScale = 1 << 4,
		OneShotPunchPos = 1 << 5,
		OneShotPos = 1 << 6,
		OneShotPunchRot = 1 << 7,
		OneShotRot = 1 << 8,

		AllLoop = LoopScale | LoopPos | LoopRot,
		AllOneShot = OneShotPunchScale | OneShotScale | OneShotPunchPos | OneShotPos | OneShotPunchRot | OneShotRot,
		None = 0,
	}

#if UNITY_EDITOR
	Color GetEnumColor ()
	{
		return m_animsDisplayed == AnimationType.None ? Color.red : Color.white;
	}

	bool IsRectTfm ()
	{
		if (m_rectTfm != null) return true;

		RectTransform r = transform.GetComponent<RectTransform>();

		return r != null;
	}
#endif
	private bool AnyLoopSelected => m_animsDisplayed.HasFlag(AnimationType.LoopPos) || m_animsDisplayed.HasFlag(AnimationType.LoopRot) || m_animsDisplayed.HasFlag(AnimationType.LoopScale);
	private bool AnyOneShotSelected => m_animsDisplayed.HasFlag(AnimationType.OneShotPunchScale) || m_animsDisplayed.HasFlag(AnimationType.OneShotScale) || m_animsDisplayed.HasFlag(AnimationType.OneShotPunchPos) || m_animsDisplayed.HasFlag(AnimationType.OneShotPos) || m_animsDisplayed.HasFlag(AnimationType.OneShotPunchRot) || m_animsDisplayed.HasFlag(AnimationType.OneShotRot);

	[System.Serializable]
	public class TweenConfig
	{
		[HideInInspector] public bool isLoopTween;

		[ShowIf("isLoopTween")]
		[ReadOnly]
		public bool enable = false;

		[ShowIf("isLoopTween")]
		public bool isPunchTween = true;

		public bool useOffsetForDestination = false;
		[ShowIf("useOffsetForDestination")]
		public Vector3 offSetValue = Vector3.one;
		[ShowIf("@!useOffsetForDestination")]
		public Vector3 targetValue = Vector3.one;

		public float delay = 0f;
		public float duration = 0.8f;
		public RotateMode rotMode = RotateMode.LocalAxisAdd;

		public bool useEaseCurve = false;
		[ShowIf("@!useEaseCurve")]
		public Ease ease = Ease.InOutQuad;
		[ShowIf("useEaseCurve")]
		public AnimationCurve curve;

		[ShowIf("isPunchTween")]
		public int vibrato = 5;
		[ShowIf("isPunchTween")]
		public float elasticity = 0.8f;

		public LoopType loopType = LoopType.Yoyo;
		[Range(1, 10)] public int loopCount = 1;

		[ShowIf("isLoopTween")]
		public float loopIntervalDuration = 1.5f;
		public bool ignoreTimeScale = true;


		public TweenConfig () { }
		public TweenConfig ( TweenConfig _reference )
		{
			isLoopTween = _reference.isLoopTween;
			isPunchTween = _reference.isPunchTween;
			useOffsetForDestination = _reference.useOffsetForDestination;
			offSetValue = _reference.offSetValue;
			targetValue = _reference.targetValue;
			delay = _reference.delay;
			duration = _reference.duration;
			useEaseCurve = _reference.useEaseCurve;
			ease = _reference.ease;
			curve = _reference.curve;
			vibrato = _reference.vibrato;
			elasticity = _reference.elasticity;
			loopType = _reference.loopType;
			loopCount = _reference.loopCount;
			loopIntervalDuration = _reference.loopIntervalDuration;
		}
		public static TweenConfig LoopTween
		{
			get
			{
				return new TweenConfig() { isLoopTween = true };
			}
		}
		public static TweenConfig OneShotTween
		{
			get
			{
				return new TweenConfig() { isLoopTween = false };
			}
		}
		public static TweenConfig PunchScaleLoop
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = true,
					isPunchTween = true,
					useOffsetForDestination = true,
					offSetValue = Vector3.one * 0.25f,
					delay = 0f,
					duration = 0.8f,
					vibrato = 5,
					elasticity = 0.8f,
					loopCount = 1,
				};
			}
		}
		public static TweenConfig PosLoop
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = true,
					isPunchTween = false,
					useOffsetForDestination = true,
					offSetValue = Vector3.up * 60f,
					delay = 0f,
					duration = 0.18f,
					ease = Ease.OutQuad,
					loopCount = 4,
				};
			}
		}
		public static TweenConfig RotLoop
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = true,
					isPunchTween = true,
					useOffsetForDestination = true,
					offSetValue = Vector3.forward * 20f,
					delay = 0f,
					duration = 0.8f,
					vibrato = 5,
					elasticity = 1f,
					loopCount = 1,
					loopIntervalDuration = 1.5f,
				};
			}
		}
		public static TweenConfig PunchScaleOS
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = false,
					useOffsetForDestination = true,
					offSetValue = Vector3.one * 0.3f,
					delay = 0f,
					duration = 0.6f,
					vibrato = 8,
					elasticity = 0.7f,
					loopCount = 1,
				};
			}
		}
		public static TweenConfig ScaleOS
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = false,
					isPunchTween = false,
					useOffsetForDestination = false,
					targetValue = Vector3.one * 1.4f,
					delay = 0f,
					duration = 0.7f,
					useEaseCurve = false,
					ease = Ease.OutQuint,
					loopType = LoopType.Yoyo,
					loopCount = 2,
					loopIntervalDuration = 1.5f,
				};
			}
		}
		public static TweenConfig PunchPosOS
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = false,
					isPunchTween = true,
					useOffsetForDestination = true,
					offSetValue = Vector3.right * 50f,
					delay = 0f,
					duration = 0.8f,
					vibrato = 9,
					elasticity = 0.8f,
					loopCount = 1,
					loopIntervalDuration = 1.5f,
				};
			}
		}
		public static TweenConfig PosOS
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = false,
					isPunchTween = false,
					useOffsetForDestination = true,
					offSetValue = Vector3.up * 60f,
					delay = 0f,
					duration = 0.18f,
					useEaseCurve = false,
					ease = Ease.OutQuad,
					loopType = LoopType.Yoyo,
					loopCount = 4,
					loopIntervalDuration = 1.5f,
				};
			}
		}
		public static TweenConfig PunchRotOS
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = false,
					isPunchTween = true,
					useOffsetForDestination = true,
					offSetValue = Vector3.forward * 20f,
					delay = 0f,
					duration = 0.8f,
					vibrato = 5,
					elasticity = 1f,
					loopCount = 1,
					loopIntervalDuration = 1.5f,
				};
			}
		}
		public static TweenConfig RotOS
		{
			get
			{
				return new TweenConfig()
				{
					isLoopTween = false,
					isPunchTween = false,
					useOffsetForDestination = true,
					offSetValue = Vector3.forward * -360f,
					delay = 0f,
					duration = 0.8f,
					useEaseCurve = false,
					ease = Ease.OutQuart,
					loopCount = 1,
					loopIntervalDuration = 1.5f,
				};
			}
		}
	}

	protected Tweener m_oneShotScaleTweener;
	protected Tweener m_oneShotPosTweener;
	protected Tweener m_oneShotRotTweener;
	protected Sequence m_rotLoopSeq;
	protected Sequence m_posLoopSeq;
	protected Sequence m_scaleLoopSeq;

	protected Action m_onOneShotScaleFinishedAction;
	protected Action m_onOneShotPosFinishedAction;
	protected Action m_onOneShotRotFinishedAction;

	protected Vector3 StartScale
	{
		get
		{
			if (!m_isInit)
				Init();
			return m_startScale;
		}
	}
	protected Vector3 m_startScale;
	protected Quaternion StartRot
	{
		get
		{
			if (!m_isInit)
				Init();
			return m_startRot;
		}
	}
	protected Quaternion m_startRot;
	protected Vector2 StartAnchorPos
	{
		get
		{
			if (!m_isInit)
				Init();
			return m_startAnchorPos;
		}
	}
	protected Vector2 m_startAnchorPos;
	protected Vector3 StartPos
	{
		get
		{
			if (!m_isInit)
				Init();
			return m_startPos;
		}
	}
	protected Vector3 m_startPos;
	protected bool m_isInit;

#if UNITY_EDITOR
	protected override void Reset ()
	{
		base.Reset();
		m_rectTfm = GetComponent<RectTransform>();

		//MUST DO : if you add a tween or change the default values, set the default constructor here
		m_scaleLoopConfig = TweenConfig.PunchScaleLoop;
		m_posLoopConfig = TweenConfig.PosLoop;
		m_rotLoopConfig = TweenConfig.RotLoop;

		m_punchScaleOSConfig = TweenConfig.PunchScaleOS;
		m_scaleOSConfig = TweenConfig.ScaleOS;
		m_punchAnchorPosOSConfig = TweenConfig.PunchPosOS;
		m_anchorPosOSConfig = TweenConfig.PosOS;
		m_punchRotOSConfig = TweenConfig.PunchRotOS;
		m_rotOSConfig = TweenConfig.RotOS;

		if (!m_preventPosChange && transform.parent != null && m_rectTfm != null)
		{
			if (transform.parent != null && transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null)
				m_preventPosChange = true;
		}
	}
#endif
	protected override void Awake ()
	{
		base.Awake();
		GetWindowParent();
		Init();
		//cause unwanted behavior because the m_startPos != layoutGroup Pos
		if (!m_preventPosChange && m_setPreventPosAtAwake && transform.parent != null && transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null)
		{
			Debug.LogWarning(gameObject.name + " rectAnim have 'preventPosChange' set to TRUE because its parent is a layoutGroup, you should set it to true manually to hide this warning log");
			m_preventPosChange = true;
		}
	}

	protected void Init ()
	{
		m_startScale = AnimatedTfm.localScale;
		m_startRot = AnimatedTfm.localRotation;
		m_startPos = transform.localPosition;
		if (m_rectTfm != null)
			m_startAnchorPos = m_rectTfm.anchoredPosition;
		m_isInit = true;
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();

		m_oneShotScaleTweener?.Kill();
		m_oneShotPosTweener?.Kill();
		m_oneShotRotTweener?.Kill();
		m_rotLoopSeq?.Kill();
		m_posLoopSeq?.Kill();
		m_scaleLoopSeq?.Kill();
	}

	protected override void OnCanvasParentEnabled ()
	{
		base.OnCanvasParentEnabled();
		if (!activeInHierarchy)
			return;
		SetScaleLoopActive(m_scaleLoopConfig.enable, false);
		SetPosLoopActive(m_posLoopConfig.enable, false);
		SetRotLoopActive(m_rotLoopConfig.enable, false);
	}

	protected override void OnCanvasParentDisabled ()
	{
		base.OnCanvasParentDisabled();
		if (!activeInHierarchy)
			return;
		KillScaleLoop();
		KillPosLoop();
		KillRotLoop();
		ResetAllOneShotAnimation();
	}

	//protected override void OnEnable ()
	//{
	//	base.OnEnable();
	//	if (!canvasEnabled)
	//		return;
	//	if (m_scaleLoopConfig.enable)
	//		SetScaleLoopActive(true, false);
	//	if (m_posLoopConfig.enable)
	//		SetPosLoopActive(true, false);
	//	if (m_rotLoopConfig.enable)
	//		SetRotLoopActive(true, false);
	//}

	//protected override void OnDisable ()
	//{
	//	base.OnDisable();
	//	if (!canvasEnabled)
	//		return;
	//	KillScaleLoop();
	//	KillPosLoop();
	//	KillRotLoop();
	//	ResetAllOneShotAnimation();
	//}

	#region LOOP
	#region PunchScale / Scale
	//ScaleLoop 
	//PunchScaleLoop

	[FoldoutGroup("Loop", VisibleIf = "AnyLoopSelected")]
	[GUIColor("@new Color(m_scaleLoopConfig.enable ? 0.4f : 1f, 1f, 1f)")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.LoopScale)")]
	[SerializeField] protected TweenConfig m_scaleLoopConfig;

	public void UpdateAllLoop ( bool _active )
	{
		if (!activeInHierarchy)
			return;

		if (m_animsDisplayed == AnimationType.AllLoop)
		{
			SetScaleLoopActive(_active, false);
			SetPosLoopActive(_active, false);
			SetRotLoopActive(_active, false);
		}
		else
		{
			if (m_animsDisplayed == AnimationType.LoopScale)
				SetScaleLoopActive(_active, false);

			if (m_animsDisplayed == AnimationType.LoopPos)
				SetPosLoopActive(_active, false);

			if (m_animsDisplayed == AnimationType.LoopRot)
				SetRotLoopActive(_active, false);
		}
	}

	public void SetScaleLoopActive ( bool _active, bool _forceUpdateInstant = true )//the last param is useful when setting a loop in runtime so you don't have to call "UpdateLoopStatus" after
	{
		Sequence seq = m_scaleLoopSeq;
		TweenConfig conf = m_scaleLoopConfig;

		conf.enable = _active;

		if (_active)
		{
			if (seq != null)
			{
				if (!seq.IsPlaying())
					seq.Play();
			}
			else
			{
				//Init Sequence
				seq = DOTween.Sequence();
				seq.SetUpdate(conf.ignoreTimeScale);
				seq.SetDelay(conf.delay);

				if (!conf.isPunchTween)
				{
					Vector3 targetValue = conf.useOffsetForDestination ? StartScale + conf.offSetValue : conf.targetValue;

					if (conf.useEaseCurve)
						seq.Append(AnimatedTfm.DOScale(targetValue, conf.duration).SetEase(conf.curve).SetLoops(conf.loopCount, conf.loopType));
					else
						seq.Append(AnimatedTfm.DOScale(targetValue, conf.duration).SetEase(conf.ease).SetLoops(conf.loopCount, conf.loopType));
				}
				else
				{
					Vector3 targetValue = conf.useOffsetForDestination ? conf.offSetValue : conf.targetValue;

					seq.Append(AnimatedTfm.DOPunchScale(targetValue, conf.duration, conf.vibrato, conf.elasticity));
				}

				seq.AppendInterval(conf.loopIntervalDuration);

				seq.SetLoops(-1);
				m_scaleLoopSeq = seq;
			}
		}
		else
		{
			seq?.Kill(true);
			seq = null;
			m_scaleLoopSeq = null;
		}

		if (_forceUpdateInstant)
			UpdateLoopStatus();
	}

	public bool IsScaleLoopSeqPlaying => m_scaleLoopSeq != null;

	void KillScaleLoop ()
	{
		//if (m_scaleLoopSeq != null && m_scaleLoopSeq.IsPlaying())
		//m_scaleLoopSeq.Pause();

		m_scaleLoopSeq?.Kill(true);
		m_scaleLoopSeq = null;
	}

	#endregion
	#region PunchPos / Pos
	//PosLoop 
	//PunchPosLoop
	[FoldoutGroup("Loop")]
	[GUIColor("@new Color(m_posLoopConfig.enable ? 0.4f : 1f, 1f, 1f)")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.LoopPos)")]
	[SerializeField] protected TweenConfig m_posLoopConfig;
	public void SetPosLoopActive ( bool _active, bool _forceUpdateInstant = true ) //the last param is useful when setting a loop in runtime so you don't have to call "UpdateLoopStatus" after
	{
		Sequence seq = m_posLoopSeq;
		TweenConfig conf = m_posLoopConfig;

		conf.enable = _active;

		if (_active)
		{
			if (seq != null)
			{
				if (!seq.IsPlaying())
					seq.Play();
			}
			else
			{
				//Init Sequence
				seq = DOTween.Sequence();
				seq.SetUpdate(conf.ignoreTimeScale);
				seq.SetDelay(conf.delay);

				if (!conf.isPunchTween)
				{
					if (m_rectTfm != null)
					{
						Vector3 targetValue = conf.useOffsetForDestination ? (Vector3)StartAnchorPos + conf.offSetValue : conf.targetValue;
						if (conf.useEaseCurve)
							seq.Append(m_rectTfm.DOAnchorPos(targetValue, conf.duration).SetEase(conf.curve).SetLoops(conf.loopCount, conf.loopType));
						else
							seq.Append(m_rectTfm.DOAnchorPos(targetValue, conf.duration).SetEase(conf.ease).SetLoops(conf.loopCount, conf.loopType));
					}
					else
					{
						Vector3 targetValue = conf.useOffsetForDestination ? (Vector3)StartPos + conf.offSetValue : conf.targetValue;
						if (conf.useEaseCurve)
							seq.Append(transform.DOLocalMove(targetValue, conf.duration).SetEase(conf.curve).SetLoops(conf.loopCount, conf.loopType));
						else
							seq.Append(transform.DOLocalMove(targetValue, conf.duration).SetEase(conf.ease).SetLoops(conf.loopCount, conf.loopType));
					}
				}
				else
				{
					Vector3 targetValue = conf.useOffsetForDestination ? conf.offSetValue : conf.targetValue;
					if (m_rectTfm != null)
					{
						seq.Append(m_rectTfm.DOPunchAnchorPos(targetValue, conf.duration, conf.vibrato, conf.elasticity));
					}
					else
					{
						seq.Append(transform.DOPunchPosition(targetValue, conf.duration, conf.vibrato, conf.elasticity));
					}
				}

				seq.AppendInterval(conf.loopIntervalDuration);

				seq.SetLoops(-1);
				m_posLoopSeq = seq;
			}
		}
		else
		{
			seq?.Kill(true);
			seq = null;
			m_posLoopSeq = null;
		}

		if (_forceUpdateInstant)
			UpdateLoopStatus();
	}

	public bool IsPosLoopSeqPlaying =>

		m_posLoopSeq != null;

	void KillPosLoop ()
	{
		//if (m_posLoopSeq != null && m_posLoopSeq.IsPlaying())
		//m_posLoopSeq.Pause();
		m_posLoopSeq?.Kill(true);
		m_posLoopSeq = null;
	}
	#endregion
	#region PunchRot / Rot
	//RotLoop 
	//PunchRotLoop
	[FoldoutGroup("Loop")]
	[GUIColor("@new Color(m_rotLoopConfig.enable ? 0.4f : 1f, 1f, 1f)")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.LoopRot)")]
	[SerializeField] protected TweenConfig m_rotLoopConfig;
	public TweenConfig RotLoopConfig => m_rotLoopConfig;
	public void SetRotLoopActive ( bool _active, bool _forceUpdateInstant = true )//the last param is useful when setting a loop in runtime so you don't have to call "UpdateLoopStatus" after
	{
		Sequence seq = m_rotLoopSeq;
		TweenConfig conf = m_rotLoopConfig;

		conf.enable = _active;

		if (_active)
		{
			if (seq != null)
			{
				if (!seq.IsPlaying())
					seq.Play();
			}
			else
			{
				//Init Sequence
				seq = DOTween.Sequence();
				seq.SetUpdate(conf.ignoreTimeScale);
				seq.SetDelay(conf.delay);

				if (!conf.isPunchTween)
				{
					Vector3 targetValue = conf.useOffsetForDestination ? StartRot.eulerAngles + conf.offSetValue : conf.targetValue;

					if (conf.useEaseCurve)
						seq.Append(AnimatedTfm.DORotate(targetValue, conf.duration, conf.rotMode).SetEase(conf.curve).SetLoops(conf.loopCount, conf.loopType));
					else
						seq.Append(AnimatedTfm.DORotate(targetValue, conf.duration, conf.rotMode).SetEase(conf.ease).SetLoops(conf.loopCount, conf.loopType));
				}
				else
				{
					Vector3 targetValue = conf.useOffsetForDestination ? conf.offSetValue : conf.targetValue;

					seq.Append(AnimatedTfm.DOPunchRotation(targetValue, conf.duration, conf.vibrato, conf.elasticity));
				}

				seq.AppendInterval(conf.loopIntervalDuration);

				seq.SetLoops(-1);
				m_rotLoopSeq = seq;
			}
		}
		else
		{
			seq?.Kill(true);
			seq = null;
			m_rotLoopSeq = null;
		}

		if (_forceUpdateInstant)
			UpdateLoopStatus();
	}

	public bool IsRotLoopSeqPlaying => m_rotLoopSeq != null;

	void KillRotLoop ()
	{
		//if (m_rotLoopSeq != null && m_rotLoopSeq.IsPlaying())
		//m_rotLoopSeq.Pause();

		m_rotLoopSeq?.Kill(true);
		m_rotLoopSeq = null;
	}
	#endregion

	[FoldoutGroup("Loop")]
	[Button]
	public void UpdateLoopStatus () //was OnAllOneShotAnimationsFinished
	{
		OnOneShotScaleFinished();
		OnOneShotRotFinished();
		OnOneShotPosFinished();
	}

	[FoldoutGroup("Loop")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.LoopScale)")]
	[Button("@m_scaleLoopConfig.enable ? \"SetScaleLoop : OFF\" : \"SetScaleLoop : ON\"")]
	public void ToggleScaleLoop ()
	{
		m_scaleLoopConfig.enable = !m_scaleLoopConfig.enable;
		UpdateLoopStatus();
	}
	[FoldoutGroup("Loop")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.LoopPos)")]
	[Button("@m_posLoopConfig.enable ? \"SetPosLoop : OFF\" : \"SetPosLoop : ON\"")]
	public void TogglePosLoop ()
	{
		m_posLoopConfig.enable = !m_posLoopConfig.enable;
		UpdateLoopStatus();
	}
	[FoldoutGroup("Loop")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.LoopRot)")]
	[Button("@m_rotLoopConfig.enable ? \"SetRotLoop : OFF\" : \"SetRotLoop : ON\"")]
	public void ToggleRotLoop ()
	{
		m_rotLoopConfig.enable = !m_rotLoopConfig.enable;
		UpdateLoopStatus();
	}
	#endregion

	#region ONE SHOT
	public void ResetAllOneShotAnimation ()
	{
		ResetOneShotScale();
		ResetOneShotPos();
		ResetOneShotRot();
	}

	#region PunchScale / Scale
	void ResetOneShotScale ()
	{
		m_oneShotScaleTweener?.Kill(true);
		m_oneShotScaleTweener = null;
		m_onOneShotScaleFinishedAction = null;
		AnimatedTfm.localScale = StartScale;
	}

	void OnOneShotScaleFinished ()
	{
		m_onOneShotScaleFinishedAction?.Invoke();
		ResetOneShotScale();
		SetScaleLoopActive(m_scaleLoopConfig.enable, false);
	}

	//PunchScale 
	[FoldoutGroup("OneShot", VisibleIf = "AnyOneShotSelected")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPunchScale)")]
	[SerializeField] protected TweenConfig m_punchScaleOSConfig;
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPunchScale)")]
	[Button("Test PunchScale")]
	public void DoPunchScale ( Action _onFinished = null )
	{
		KillScaleLoop();
		ResetOneShotScale();
		m_onOneShotScaleFinishedAction = _onFinished;
		TweenConfig conf = m_punchScaleOSConfig;
		Vector3 targetValue = conf.useOffsetForDestination ? conf.offSetValue : conf.targetValue;

		m_oneShotScaleTweener = AnimatedTfm.DOPunchScale(targetValue, conf.duration, conf.vibrato, conf.elasticity).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).SetDelay(conf.delay).OnComplete(OnOneShotScaleFinished);
	}

	//Scale
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotScale)")]
	[SerializeField] protected TweenConfig m_scaleOSConfig;
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotScale)")]
	[Button("Test Scale")]
	public void DoScale ( Action _onFinished = null )
	{
		KillScaleLoop();
		ResetOneShotScale();
		m_onOneShotScaleFinishedAction = _onFinished;
		TweenConfig conf = m_scaleOSConfig;
		Vector3 targetValue = conf.useOffsetForDestination ? StartScale + conf.offSetValue : conf.targetValue;

		if (conf.useEaseCurve)
			m_oneShotScaleTweener = AnimatedTfm.DOScale(targetValue, conf.duration).SetDelay(conf.delay).SetEase(conf.curve).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotScaleFinished);
		else
			m_oneShotScaleTweener = AnimatedTfm.DOScale(targetValue, conf.duration).SetDelay(conf.delay).SetEase(conf.ease).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotScaleFinished);
	}
	#endregion
	#region PunchPos / Pos
	void ResetOneShotPos ()
	{
		m_oneShotPosTweener?.Kill(true);
		m_oneShotPosTweener = null;
		m_onOneShotPosFinishedAction = null;
		if (!m_preventPosChange)
		{
			if (m_rectTfm != null)
				m_rectTfm.anchoredPosition = StartAnchorPos;
			else
				transform.localPosition = StartPos;
		}
	}

	void OnOneShotPosFinished ()
	{
		m_onOneShotPosFinishedAction?.Invoke();
		ResetOneShotPos();
		SetPosLoopActive(m_posLoopConfig.enable, false);
	}

	//PunchPos 
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPunchPos)")]
	[SerializeField] protected TweenConfig m_punchAnchorPosOSConfig;
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPunchPos)")]
	[Button("Test PunchAnchorPos")]
	public void DoPunchAnchorPosition ( Action _onFinished = null )
	{
		KillPosLoop();
		ResetOneShotPos();
		m_onOneShotPosFinishedAction = _onFinished;
		TweenConfig conf = m_punchAnchorPosOSConfig;
		Vector3 targetValue = conf.useOffsetForDestination ? conf.offSetValue : conf.targetValue;
		if (m_rectTfm != null)
			m_oneShotPosTweener = m_rectTfm.DOPunchAnchorPos(targetValue, conf.duration, conf.vibrato, conf.elasticity).SetDelay(conf.delay).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotPosFinished);
		else
			m_oneShotPosTweener = transform.DOPunchPosition(targetValue, conf.duration, conf.vibrato, conf.elasticity).SetDelay(conf.delay).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotPosFinished);
	}

	//Pos
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPos)")]
	[SerializeField] protected TweenConfig m_anchorPosOSConfig;
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPos)")]
	[Button("Test AnchorPos")]
	public void DoAnchorPos ( Action _onFinished = null )
	{
		KillPosLoop();
		ResetOneShotPos();
		m_onOneShotPosFinishedAction = _onFinished;
		TweenConfig conf = m_anchorPosOSConfig;
		if (m_rectTfm != null)
		{
			Vector3 targetValue = conf.useOffsetForDestination ? (Vector3)StartAnchorPos + conf.offSetValue : conf.targetValue;

			if (conf.useEaseCurve)
				m_oneShotPosTweener = m_rectTfm.DOAnchorPos(targetValue, conf.duration).SetDelay(conf.delay).SetEase(conf.curve).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotPosFinished);
			else
				m_oneShotPosTweener = m_rectTfm.DOAnchorPos(targetValue, conf.duration).SetDelay(conf.delay).SetEase(conf.ease).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotPosFinished);
		}
		else
		{
			Vector3 targetValue = conf.useOffsetForDestination ? (Vector3)StartPos + conf.offSetValue : conf.targetValue;

			if (conf.useEaseCurve)
				m_oneShotPosTweener = transform.DOLocalMove(targetValue, conf.duration).SetDelay(conf.delay).SetEase(conf.curve).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotPosFinished);
			else
				m_oneShotPosTweener = transform.DOLocalMove(targetValue, conf.duration).SetDelay(conf.delay).SetEase(conf.ease).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotPosFinished);
		}
	}
	#endregion
	#region PunchRot / Rot
	void ResetOneShotRot ()
	{
		m_oneShotRotTweener?.Kill(true);
		m_oneShotRotTweener = null;
		m_onOneShotRotFinishedAction = null;
		AnimatedTfm.localRotation = StartRot;
	}

	void OnOneShotRotFinished ()
	{
		m_onOneShotRotFinishedAction?.Invoke();
		ResetOneShotRot();
		SetRotLoopActive(m_rotLoopConfig.enable, false);
	}

	//PunchRot
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPunchRot)")]
	[SerializeField] protected TweenConfig m_punchRotOSConfig;
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotPunchRot)")]
	[Button("Test PunchRot")]
	public void DoPunchRotation ( Action _onFinished = null )
	{
		KillRotLoop();
		ResetOneShotRot();
		m_onOneShotRotFinishedAction = _onFinished;
		TweenConfig conf = m_punchRotOSConfig;
		Vector3 targetValue = conf.useOffsetForDestination ? conf.offSetValue : conf.targetValue;

		m_oneShotRotTweener = AnimatedTfm.DOPunchRotation(targetValue, conf.duration, conf.vibrato, conf.elasticity).SetDelay(conf.delay).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotRotFinished);
	}

	//Pos
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotRot)")]
	[SerializeField] protected TweenConfig m_rotOSConfig;
	[FoldoutGroup("OneShot")]
	[ShowIf("@m_animsDisplayed.HasFlag(AnimationType.OneShotRot)")]
	[Button("Test Rot")]
	public void DoRotation ( Action _onFinished = null )
	{
		KillRotLoop();
		ResetOneShotPos();
		m_onOneShotRotFinishedAction = _onFinished;
		TweenConfig conf = m_rotOSConfig;
		Vector3 targetValue = conf.useOffsetForDestination ? StartRot.eulerAngles + conf.offSetValue : conf.targetValue;

		if (conf.useEaseCurve)
			m_oneShotRotTweener = AnimatedTfm.DORotate(targetValue, conf.duration, conf.rotMode).SetEase(conf.curve).SetDelay(conf.delay).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotRotFinished);
		else
			m_oneShotRotTweener = AnimatedTfm.DORotate(targetValue, conf.duration, conf.rotMode).SetEase(conf.ease).SetDelay(conf.delay).SetUpdate(conf.ignoreTimeScale).SetLoops(conf.loopCount, conf.loopType).OnComplete(OnOneShotRotFinished);
	}
	#endregion
	#endregion

}