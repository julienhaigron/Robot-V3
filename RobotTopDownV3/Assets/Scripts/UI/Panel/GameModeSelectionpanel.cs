using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameModeSelectionpanel : AUIPanel
{
	[SerializeField] private BaseButton m_soloModeBtn;
	[SerializeField] private BaseButton m_multiplayerModeBtn;


	private void Awake ()
	{
		m_soloModeBtn.onClick += OnClickSoloModeBtn;
		m_multiplayerModeBtn.onClick += OnClickMultiModeBtn;
	}

	private void OnDestroy ()
	{
		m_soloModeBtn.onClick -= OnClickSoloModeBtn;
		m_multiplayerModeBtn.onClick -= OnClickMultiModeBtn;
	}


	private void OnClickSoloModeBtn ()
	{
		UIManager.Instance.OpenPanel<SoloCampainPanel>();
	}

	private void OnClickMultiModeBtn ()
	{
		UIManager.Instance.OpenPanel<LobbySelectionPanel>();
	}
}
