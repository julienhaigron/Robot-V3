using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Sirenix.OdinInspector;

public class MoveFadeRTV : MonoBehaviour
{
	public Action onTweenFinishAction;
	public Action onTweenStartAction;

	[Title("Depedencies")]
	[SerializeField] protected CanvasGroup m_canvasGroup;
	[SerializeField] protected RectTransform m_rectTfm;

	[Title("Parameters")]
	[SerializeField] protected TweenConfig m_tweenConfig;
	[SerializeField] protected bool m_ignoreTimeScale = true;
	[ShowIf("m_canvasGroup")] [SerializeField] protected bool m_setCanvasInteractibilityOnVisible = true;
	[SerializeField] private bool m_specifyPoses;

	[ShowIf("@m_specifyPoses == false")]
	public Vector3 hideOffset;
	[ShowIf("m_specifyPoses")]
	[SerializeField] private Vector3 m_hiddenPos;
	[ShowIf("m_specifyPoses")]
	[SerializeField] private Vector3 m_visiblePos;

	private Vector3[] m_posesArray;
	private Vector3[] PosesArray
	{
		get
		{
			if (m_posesArray == null)
				SetPosesArray();
			return m_posesArray;
		}
	}

	public void SetPosesArray ()
	{
		m_posesArray = new Vector3[2];
		m_posesArray[0] = m_specifyPoses ? m_hiddenPos : m_rectTfm.localPosition + hideOffset;
		m_posesArray[1] = m_specifyPoses ? m_visiblePos : m_rectTfm.localPosition;
	}

	protected int m_currentIndex;
	private Tweener m_fadeTween;
	private Tweener m_moveTween;
	private Tween m_delayTweener;

	protected bool UseFadeTween => m_canvasGroup != null;
	private bool UseMoveTween => hideOffset != Vector3.zero;

	#region getter setter
	protected bool m_isVisible;
	public bool IsVisible
	{
		get
		{
			return m_isVisible;
		}
		set
		{
			m_isVisible = value;
		}
	}
	public float ShowDuration { get { return m_tweenConfig.showDuration; } set { m_tweenConfig.showDuration = value; } }
	public float HideDuration { get { return m_tweenConfig.hideDuration; } set { m_tweenConfig.hideDuration = value; } }
	public Ease ShowEase { get { return m_tweenConfig.showEase; } set { m_tweenConfig.showEase = value; } }
	public Ease HideEase { get { return m_tweenConfig.hideEase; } set { m_tweenConfig.hideEase = value; } }
	#endregion

	public void SetVisible ( bool _visible, float? _duration = null, float _delay = 0f, Action _onFinishedAction = null )
	{
		m_currentIndex = _visible ? 1 : 0;

		_delay += _visible ? m_tweenConfig.showDelay : m_tweenConfig.hideDelay;
		float duration = _duration ?? (_visible ? ShowDuration : HideDuration);
		Ease ease = _visible ? ShowEase : HideEase;
		AnimationCurve curve = _visible ? m_tweenConfig.showEaseCurve : m_tweenConfig.hideEaseCurve;

		m_fadeTween?.Kill();
		m_moveTween?.Kill();
		m_delayTweener?.Kill();

		if (_duration == 0f)
		{
			if (_delay == 0f)
				ApplyValueInstant();
			else
				m_delayTweener = DOVirtual.DelayedCall(_delay, ApplyValueInstant);
		}
		else
		{
			if (UseFadeTween)
			{
				m_fadeTween = m_canvasGroup.DOFade(_visible ? 1f : 0f, duration).SetUpdate(m_ignoreTimeScale).SetEase(Ease.Linear).SetDelay(_delay).OnStart(OnStartTween).OnComplete(() =>
				{
					_onFinishedAction?.Invoke();
					OnTweenComplete();
				});
			}

			if (UseMoveTween)
			{
				if (m_tweenConfig.useCustomEaseCurve)
					m_moveTween = m_rectTfm.DOLocalMove(PosesArray[m_currentIndex], duration).SetUpdate(m_ignoreTimeScale).SetEase(Ease.OutQuart).OnStart(OnStartTween).SetDelay(_delay);
				else
					m_moveTween = m_rectTfm.DOLocalMove(PosesArray[m_currentIndex], duration).SetUpdate(m_ignoreTimeScale).SetEase(Ease.OutQuart).OnStart(OnStartTween).SetDelay(_delay);
			}

			if (!UseFadeTween)
			{
				m_moveTween.OnStart(OnStartTween).OnComplete(() =>
				{
					_onFinishedAction?.Invoke();
					OnTweenComplete();
				});
			}

		}
	}

	public void ApplyValueInstant ()
	{
		OnStartTween();

		if (UseFadeTween)
			m_canvasGroup.alpha = IsVisible ? 1f : 0f;

		if (UseMoveTween)
			m_rectTfm.localPosition = PosesArray[m_currentIndex];
	}

	public void OnStartTween ()
	{
		IsVisible = m_currentIndex == 1;

		if (m_setCanvasInteractibilityOnVisible && UseFadeTween)
		{
			m_canvasGroup.interactable = IsVisible;
			m_canvasGroup.blocksRaycasts = IsVisible;
		}

		onTweenStartAction?.Invoke();
	}

	protected virtual void OnTweenComplete ()
	{
		onTweenFinishAction?.Invoke();
	}


	[System.Serializable]
	public class TweenConfig
	{
		public float showDelay = 0f;
		public float showDuration = 0.3f;
		[ShowIf("@!useCustomEaseCurve")]
		public Ease showEase = Ease.OutQuad;
		[ShowIf("useCustomEaseCurve")]
		public AnimationCurve showEaseCurve;
		public float hideDelay = 0f;
		public float hideDuration = 0.2f;
		[ShowIf("@!useCustomEaseCurve", false)]
		public Ease hideEase = Ease.InQuad;
		[ShowIf("useCustomEaseCurve")]
		public AnimationCurve hideEaseCurve;
		public bool useCustomEaseCurve;

		public void CopyRef ( TweenConfig _ref )
		{
			showDelay = _ref.showDelay;
			showDuration = _ref.showDuration;
			showEase = _ref.showEase;
			showEaseCurve = _ref.showEaseCurve;
			hideDelay = _ref.hideDelay;
			hideDuration = _ref.hideDuration;
			hideEase = _ref.hideEase;
			hideEaseCurve = _ref.hideEaseCurve;
			useCustomEaseCurve = _ref.useCustomEaseCurve;
		}
	}

}
