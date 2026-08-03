using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;

public class AUIWindow : UICanvasParent
{
	public Action onWindowOpened;
	public Action onWindowClosed;

	[SerializeField] protected GameObject m_blockClickGO;
	[SerializeField] protected CanvasGroup[] m_canvasGroupVisibilityArray;
	[SerializeField] private TutorialHighlightZone[] m_highlighZones; 
	[SerializeField] protected bool m_overrideDurations = false;
	[ShowIf("m_overrideDurations")]
	[SerializeField] protected float m_showDuration = 0.3f;
	[ShowIf("m_overrideDurations")]
	[SerializeField] protected float m_hideDuration = 0.3f;

	protected WaitForSecondsRealtime m_hideDurationWFS;
	protected WaitForSecondsRealtime m_showDurationWFS;
	protected Coroutine m_showCoroutine;
	protected Coroutine m_hideCoroutine;

	public virtual bool CanClick
	{
		get
		{
			return !m_blockClickGO.activeSelf;
		}
		set
		{
			m_blockClickGO.SetActive(!value);
		}
	}

	protected virtual void SetContainersVisible ( bool _visible, float? _duration = null )
	{
		for(int i = 0; i < m_canvasGroupVisibilityArray.Length; i++)
		{
			if (!_duration.HasValue)
				m_canvasGroupVisibilityArray[i].alpha = _visible ? 1f : 0f;
			else
				m_canvasGroupVisibilityArray[i].DOFade(_visible ? 1f : 0f, _duration.Value);
		}
	}

	public virtual void Close ( float _delay = 0f, bool _instant = false )
	{
		foreach(TutorialHighlightZone zone in m_highlighZones)
		{
			if (zone.IsVisible)
				zone.Hide();
		}
		HideWindow(_delay, _instant);
	}

	public void HideWindow ( float _delay, bool _instant )
	{
		if (m_showCoroutine != null)
			StopCoroutine(m_showCoroutine);
		if (m_hideCoroutine != null)
			StopCoroutine(m_hideCoroutine);

		if(_delay == 0f && _instant)
		{
			OnHideStarted();
			SetContainersVisible(false, null);
			OnHideFinished();
		}
		else if (gameObject.activeSelf)
			m_hideCoroutine = StartCoroutine(HideCR(_delay, _instant));
	}

	protected virtual IEnumerator HideCR ( float _delay, bool _instant )
	{
		CanClick = false;

		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		OnHideStarted();
		SetContainersVisible(false, _instant ? 0f : (m_overrideDurations ? m_hideDuration : null));

		if (!_instant)
			yield return m_hideDurationWFS;

		OnHideFinished();
	}

	protected virtual void OnHideStarted ()
	{

	}

	protected virtual void OnHideFinished ()
	{
		onWindowClosed?.Invoke();
		gameObject.SetActive(false);
	}
	public virtual void ShowWindow ( float _delay, bool _instant )
	{
		if (m_showCoroutine != null)
			StopCoroutine(m_showCoroutine);
		if (m_hideCoroutine != null)
			StopCoroutine(m_hideCoroutine);

		if (_delay == 0f && _instant)
		{
			OnShowStarted();
			SetContainersVisible(true, null);
			OnShowFinished();
		}
		else if (gameObject.activeSelf)
			m_showCoroutine = StartCoroutine(ShowCR(_delay, _instant));
	}

	protected virtual IEnumerator ShowCR ( float _delay, bool _instant )
	{
		CanClick = false;

		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		OnShowStarted();
		SetContainersVisible(true, _instant ? 0f : (m_overrideDurations ? m_showDuration : null));

		if (!_instant)
			yield return m_showDurationWFS;

		OnShowFinished();
	}

	protected virtual void OnShowStarted ()
	{

	}

	protected virtual void OnShowFinished ()
	{
		CanClick = true;
	}

	public virtual void OnLoad ()
	{

	}

}
