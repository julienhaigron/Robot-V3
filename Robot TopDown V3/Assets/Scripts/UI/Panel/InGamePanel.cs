using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class InGamePanel : AUIPanel
{

	[SerializeField] private UIEntityActionList m_entityActionList;
	public UIEntityActionList EntityActionList => m_entityActionList;

	#region MonoBehaviour & Init

	public void Init () //add param
	{
		//Script Order : Awake - ShowPanelCR - l'Init
		//in case you need to get param from other UIPANEL
	}
	#endregion

	#region animation

	protected override IEnumerator ShowCR ( float _delay, bool _instant )
	{
		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		//SetModalVisible(true, _instant ? 0f : m_modalFadeDuration);
		SetContainersVisible(true, _instant ? 0f : (m_overrideDurations ? m_showDuration : null));

		if (!_instant)
			yield return m_showDurationWFS;

		OnShowFinished();
	}

	protected override void OnShowFinished ()
	{
		CanClick = true;
	}

	protected override IEnumerator HideCR ( float _delay, bool _instant )
	{
		CanClick = false;

		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		//SetModalVisible(false, _instant ? 0f : m_modalFadeDuration);
		SetContainersVisible(false, _instant ? 0f : (m_overrideDurations ? m_hideDuration : null));

		if (!_instant)
			yield return m_hideDurationWFS;

		OnHideFinished();
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}
	#endregion

	#region Callbacks
	#endregion
}
