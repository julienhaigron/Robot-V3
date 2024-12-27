using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MultiplayerPanel : AUIPanel
{
	[Header("References")]
	public GameObject DefaultCamera;
	[SerializeField] GameObject m_startGameButton;
	[SerializeField] GameObject m_createLobbyButton;
	[SerializeField] GameObject m_joinLobbyButton;
	[SerializeField] TextMeshProUGUI m_playerCountText;
	[SerializeField] TMP_InputField m_inputField;
	[SerializeField] TextMeshProUGUI m_portText;
	[SerializeField] GameObject m_menuScene;

	bool m_isHost = false;

	private void Start ()
	{
		LobbyManager.ChangeConnectionCount += OnConnectionCountChange;
		LobbyManager.ClientSetup += SetupClient;
		m_menuScene.SetActive(true);
		DefaultCamera.SetActive(true);

	}

	#region Client

	void SetupClient ()
	{
		LobbyManager.ChangeConnectionCount -= OnConnectionCountChange;
		LobbyManager.ClientSetup -= SetupClient;
		gameObject.SetActive(false);
	}

	void OnConnectionCountChange ( int count )
	{
		m_playerCountText.text = $"Player Count: {count}";

		if (!m_isHost)
			return;

		if (GameManager.Instance.Lobby.ConnectionCount == 3)
			m_startGameButton.SetActive(true);
		else
			m_startGameButton.SetActive(false);
	}

	#endregion

	#region Button Callbacks

	public void CreateLobbyButton ()
	{
		m_isHost = true;
		GameManager.Instance.Connector.CreateLobby();

		m_createLobbyButton.SetActive(false);
		m_joinLobbyButton.SetActive(false);
		m_inputField.gameObject.SetActive(false);
		//GameManager.LoginVivoxUI.UsernameInput.gameObject.SetActive(false);
	}

	public void JoinLobbyButton ()
	{
		if (m_inputField.text == null || m_inputField.text.Length == 0)
			GameManager.Instance.Connector.JoinLobby("localhost", 7766);
		else
			GameManager.Instance.Connector.JoinLobby(m_inputField.text, 7766);


		m_createLobbyButton.SetActive(false);
		m_joinLobbyButton.SetActive(false);
		m_inputField.gameObject.SetActive(false);
		//GameManager.LoginVivoxUI.UsernameInput.gameObject.SetActive(false);
	}

	public void StartGameButton ()
	{
		DefaultCamera.SetActive(false);
		m_menuScene.SetActive(false);
		GameManager.Instance.Lobby.ObserversSetupClient();
		//GameManager.AnalyticsManager.GameStart();

	}

	public void SetPort01 ()
	{
		GameManager.Instance.Connector.SetPort(7766);
		UpdateText(7766.ToString());
	}

	void UpdateText ( string portText )
	{
		m_portText.text = $"Port: {portText}";
	}

	#endregion
}
