using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using UnityEngine;

public class PlayerConnector : MonoBehaviour
{
	[Header("References")]
	[SerializeField] Tugboat m_tugboat;
	[SerializeField] NetworkManager m_networkManager;

	[Header("Params")]
	[SerializeField] ushort m_serverPort;
	public ushort ServerPort { get => m_serverPort; set => m_serverPort = value; }

	public void CreateLobby ()
	{
		SetPort(m_serverPort);

		m_networkManager.ServerManager.StartConnection();
		m_networkManager.ClientManager.StartConnection("localhost", m_serverPort);
	}

	public void LeaveLobby ()
	{
		m_networkManager.ClientManager.StopConnection();
	}

	public void CloseLobby ()
	{
		m_networkManager.ServerManager.StopConnection(true);
	}

	public void JoinLobby ( string ip, ushort port )
	{
		SetPort(port);

		m_networkManager.ClientManager.StartConnection(ip, port);
	}

	public void SetPort ( ushort port )
	{
		m_serverPort = port;
		m_tugboat.SetPort(m_serverPort);
	}

	public void OnSettingDefaultPort ( int _, string option )
	{
		ushort port;
		if (ushort.TryParse(option, out port))
		{
			m_serverPort = port;
		}
	}
}
