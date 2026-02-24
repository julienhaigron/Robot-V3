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
		GameManager.Instance.CurrentGameMode = GameManager.GameMode.Offline;
		Close();
		UIManager.Instance.OpenPanel<SoloCampainPanel>();
	}

	private void OnClickMultiModeBtn ()
	{
		GameManager.Instance.CurrentGameMode = GameManager.GameMode.Online;
		Close();
		UIManager.Instance.OpenPanel<LobbySelectionPanel>();
	}
}
