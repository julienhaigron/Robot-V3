using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pinpin.UI
{
	public abstract class ARectTransformVisibility : UIElementMonoBehaviour
	{
		public Action onTweenFinishAction;
		public Action onTweenStartAction;

		[Space(20)]
		[SerializeField] protected CanvasGroup m_canvasGroup;
		[SerializeField] protected RectTransform m_rectTfm;
		[SerializeField] protected TweenConfig m_tweenConfig;
		[SerializeField] protected bool m_ignoreTimeScale = true;
		[ShowIf("m_canvasGroup")][SerializeField] protected bool m_setCanvasInteractibilityOnVisible = true;
		[SerializeField] protected bool m_autoShowFromHiddenOnCanvasEnable = false;

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
		}

		#region getter setter
		public float ShowDuration { get { return m_tweenConfig.showDuration; } set { m_tweenConfig.showDuration = value; } }
		public float HideDuration { get { return m_tweenConfig.hideDuration; } set { m_tweenConfig.hideDuration = value; } }
		public Ease ShowEase { get { return m_tweenConfig.showEase; } set { m_tweenConfig.showEase = value; } }
		public Ease HideEase { get { return m_tweenConfig.hideEase; } set { m_tweenConfig.hideEase = value; } }
		#endregion

		public virtual bool IsPlaying
		{
			get
			{
				return false;
			}
		}

		protected Tween m_delayTweener;
		protected int m_currentIndex;

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

		protected bool UseFadeTween => m_canvasGroup != null;

		public virtual void SetPosIndex ( int _posIndex, float? _duration = null, float _delay = 0, Action _onFinishedAction = null )
		{

		}

		public virtual void SetPosIndex ( int _posIndex, bool instant, float _delay = 0, Action _onFinishedAction = null )
		{
			SetPosIndex(_posIndex, instant ? 0f : null, _delay, _onFinishedAction);
		}

		public virtual void SetVisible ( bool _visible, float? _duration = null, float _delay = 0f, Action _onFinishedAction = null )
		{

		}

		public void SetVisible ( bool _visible, bool instant, float _delay = 0f, Action _onFinishedAction = null )
		{
			SetVisible(_visible, instant ? 0f : null, _delay, _onFinishedAction);
		}

		protected virtual void ApplyValueInstant ()
		{
			OnStartTween();
		}

		protected virtual void OnStartTween ()
		{
			IsVisible = m_currentIndex == 1;

			if (m_setCanvasInteractibilityOnVisible && UseFadeTween)
			{
				m_canvasGroup.interactable = IsVisible;
				m_canvasGroup.blocksRaycasts = IsVisible;
			}

			onTweenStartAction?.Invoke();
			onTweenStartAction = null;
		}

		//protected virtual void OnTweenPlaying(){}

		protected virtual void OnTweenComplete ()
		{
			onTweenFinishAction?.Invoke();
			onTweenFinishAction = null;
		}

#if UNITY_EDITOR
		protected override void Reset ()
		{
			base.Reset();
			GetRefs();
		}
#endif
		[FoldoutGroup("Editor Btns")]
		[ShowIf("@m_canvasGroup == null")]
		[Button("AddCanvasGroup")]
		public void AddCanvasGroup ()
		{
			if (m_canvasGroup == null)
			{
				m_canvasGroup = gameObject.GetComponent<CanvasGroup>();

				if (m_canvasGroup == null)
					m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
		}

		[FoldoutGroup("Editor Btns")]
		[Button("GetRefs")]
		public virtual void GetRefs ()
		{
			if (m_canvasGroup == null)
				m_canvasGroup = GetComponent<CanvasGroup>();
			if (m_rectTfm == null)
				m_rectTfm = GetComponent<RectTransform>();
		}

		internal void SetVisible ( bool visible, float? duration, object onHideFinished )
		{
			throw new NotImplementedException();
		}
	}
}