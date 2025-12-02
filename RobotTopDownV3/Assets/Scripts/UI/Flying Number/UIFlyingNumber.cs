using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIFlyingNumber : FlyingNumber
{
	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private Canvas m_canvas;
	[SerializeField] private Image m_iconImg;

	public override void SetIconAndMergeIndex ( Sprite _iconSprite, float _iconScale = 1f )
	{
		if (m_iconImg != null)
		{
			m_iconImg.gameObject.SetActive(true);
			m_iconImg.transform.localScale = Vector3.one * _iconScale;
		}

		if (_iconSprite != null && m_iconImg != null)
			m_iconImg.sprite = _iconSprite;

		m_mergeIndex = _iconSprite.GetHashCode();
	}

	public override void DisableIconAndSetMergeIndex ( int _mergeIndex )
	{
		if (m_iconImg != null)
			m_iconImg.gameObject.SetActive(false);

		m_mergeIndex = _mergeIndex;
	}

	protected override void MoveToNewPos ( Vector3 _newPos )
	{
		m_rectTfm.anchoredPosition = _newPos;
	}

	protected override void MovePSToIdlePos ()
	{
		/*((RectTransform)m_counterDisplay.burstUpdatePI.transform).anchoredPosition = m_idlePosition;
		((RectTransform)m_counterDisplay.loopUpdatePI.transform).anchoredPosition = m_idlePosition;*/
	}

	protected override void FadeAllAlphas ( float _alpha, float _duration, bool _ignoreTimeScale )
	{
		m_fadeTween?.Kill();
		m_fadeTween = DOTween.Sequence();
		m_fadeTween.SetUpdate(_ignoreTimeScale);
		m_fadeTween.Append(m_canvasGroup.DOFade(_alpha, _duration).SetEase(Ease.Linear));
	}

	protected override void SetObjectEnable ( bool _active )
	{
		m_canvas.enabled = _active;
	}

	protected override void DOMoveToIdlePos ( float _duration, Ease _ease, bool _ignoreTimeScale )
	{
		m_moveTween?.Kill();
		m_moveTween = m_rectTfm.DOAnchorPos(m_idlePosition, _duration).SetEase(_ease).SetUpdate(_ignoreTimeScale);
	}

	protected override void PlayBurstParticle ()
	{
		/*if (m_counterDisplay.burstUpdatePI != null)
		{
			m_counterDisplay.burstUpdatePI.Stop();
			m_counterDisplay.burstUpdatePI.Play();
		}*/
	}
}