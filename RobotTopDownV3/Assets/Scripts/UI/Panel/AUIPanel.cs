using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class AUIPanel : AUIWindow
{
	public static Action onPanelShowStarted;

	[SerializeField] private PanelSectionMotion[] m_motionSections = Array.Empty<PanelSectionMotion>();

	private bool m_isShowingInstant;
	private bool m_slidesOutOnHide;
	private float m_entranceDelay;
	private bool m_isSlidingOut;
	private Coroutine m_entranceRoutine;

	public void SlideOutOnHide ()
	{
		m_slidesOutOnHide = true;
	}

	public void DelayEntrance ( float _delay )
	{
		m_entranceDelay = _delay;
	}

	public override void ShowWindow ( float _delay, bool _instant )
	{
		m_isShowingInstant = _instant;
		base.ShowWindow(_delay, _instant);
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
		PlayEntrance();
		onPanelShowStarted?.Invoke();
	}

	private void PlayEntrance ()
	{
		float delay = m_entranceDelay;
		m_entranceDelay = 0f;
		StopEntrance();
		if (m_isShowingInstant)
			PanelSectionMotion.Rest(m_motionSections);
		else if (IsEntranceCovered())
			m_entranceRoutine = StartCoroutine(PlayEntranceWhenUncovered(delay));
		else
			PanelSectionMotion.PlayEnter(m_motionSections, delay);
	}

	private IEnumerator PlayEntranceWhenUncovered ( float _delay )
	{
		PanelSectionMotion.Hide(m_motionSections);
		while (IsEntranceCovered())
			yield return null;

		m_entranceRoutine = null;
		PanelSectionMotion.PlayEnter(m_motionSections, _delay);
	}

	private bool IsEntranceCovered ()
	{
		return m_motionSections.Length > 0 && UIManager.Instance != null && (UIManager.Instance.IsSwitchingPanel || IsCoveredByPopup());
	}

	private void StopEntrance ()
	{
		if (m_entranceRoutine == null)
			return;

		StopCoroutine(m_entranceRoutine);
		m_entranceRoutine = null;
	}

	protected override void OnHideStarted ()
	{
		base.OnHideStarted();
		StopEntrance();
		m_isSlidingOut = m_slidesOutOnHide && m_motionSections.Length > 0;
		m_slidesOutOnHide = false;
		if (m_isSlidingOut)
			PanelSectionMotion.PlayExit(m_motionSections);
	}

	protected override object HideWait ()
	{
		return m_isSlidingOut ? new WaitWhile(AreSectionsMoving) : base.HideWait();
	}

	private bool AreSectionsMoving ()
	{
		return PanelSectionMotion.AreMoving(m_motionSections);
	}

	protected override void OnHideFinished ()
	{
		StopEntrance();
		m_isSlidingOut = false;
		PanelSectionMotion.Rest(m_motionSections);
		base.OnHideFinished();
		SetCanvasEnable(false);
	}

	protected override SfxId GetDefaultOpenSfx ( UISoundConfig _config )
	{
		return _config.panelOpen;
	}

	protected override SfxId GetDefaultCloseSfx ( UISoundConfig _config )
	{
		return _config.panelClose;
	}

#if UNITY_EDITOR

	[FoldoutGroup("EditorBtns")]
	[Button("Open")]
	void OpenViaInspector ()
	{
		UIManager.Instance.OpenPanel(this);
	}
#endif
}
