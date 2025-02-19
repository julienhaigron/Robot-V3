using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*using FishNet.Connection;
using FishNet.Object;*/
using Unity.Netcode;

public class OnlinePlayerInstance : NetworkBehaviour/*NetworkBehaviour*/
{
	//public static OnlinePlayerInstance Self => GameManager.Instance.Lobby.OwnedPlayerInstance;

	int m_connectionIndex;
	public int ConnectionIndex { get => m_connectionIndex; set => m_connectionIndex = value; }


	#region server connection

	/*public override void OnStartClient ()
	{
		base.OnStartClient();

		GameManager.Instance.Lobby.AddPlayerInstance(this);

		if (!IsOwner)
		{
			//then this is (one of) the other player
			return;
		}

		GameManager.Instance.Lobby.OwnedConnection = LocalConnection;
		GameManager.Instance.Lobby.OwnedPlayerInstance = this;

		GameManager.Instance.Lobby.ServerAddConnection(LocalConnection);
	}

	public override void OnStopClient ()
	{
		base.OnStopClient();

		if (!IsOwner)
			return;

		GameManager.Instance.Lobby.ServerRemoveConnection(LocalConnection);
	}

	private void Start ()
	{
		LobbyManager.ClientSetup += SetupClient;
	}

	void SetupClient ()
	{
		LobbyManager.ClientSetup -= SetupClient;

		for (int i = 0; i < GameManager.Instance.Lobby.PlayerInstances.Count; i++)
		{
			if (this == GameManager.Instance.Lobby.PlayerInstances[i])
			{
				m_connectionIndex = i;
				break;
			}
		}

		if (!IsOwner)
		{
			//TODO : init other player (enemy)

			return;
		}

		//_playerInput.enabled = true;

		// init self player

		GameManager.Instance.Lobby.ServerAddPlayerReady();
	}*/

	#endregion
}
