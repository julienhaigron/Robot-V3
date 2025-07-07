using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbySelectionPanel : AUIPanel
{
    [SerializeField] private BaseButton m_createLobbyBtn;
    [SerializeField] private BaseButton m_openLobbySettingsBtn;

	private List<LobbyDisplay> m_lobbies = new();

	private Coroutine m_updateCR;

	private void Awake ()
	{
		m_createLobbyBtn.onClick += OnClickCreateLobby;
		m_openLobbySettingsBtn.onClick += OnClickOpenLobbySettings;
	}

	protected override void OnShowFinished ()
	{
		base.OnShowFinished();

		if (m_updateCR != null)
			StopCoroutine(m_updateCR);

		m_updateCR = StartCoroutine(UpdateCR());
	}

	protected override void OnHideStarted ()
	{
		base.OnHideStarted();
		if (m_updateCR != null)
			StopCoroutine(m_updateCR);
	}

	private IEnumerator UpdateCR ()
	{
		RefreshLobbies();
		yield return new WaitForSeconds(5f);
	}

	private void OnClickCreateLobby ()
	{

	}
	
	private void OnClickOpenLobbySettings ()
	{
		
	}

	private void RefreshLobbies ()
	{
		//TODO refresh lobbies
	}

	private void AddLobbyDisplay ()
	{
		LobbyDisplay newDisplay = Instantiate(GameAssets.current.ui.baseLobbyDisplay);
		m_lobbies.Add(newDisplay);
	}



}
