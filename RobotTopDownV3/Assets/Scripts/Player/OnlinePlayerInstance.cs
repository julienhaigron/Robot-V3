using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OnlinePlayerInstance : NetworkBehaviour
{
	public static OnlinePlayerInstance Self => GameManager.Instance.Lobby.OwnedPlayerInstance;

	int m_connectionIndex;
	public int ConnectionIndex { get => m_connectionIndex; set => m_connectionIndex = value; }


	#region server connection

	/*public override void OnNetworkSpawn ()
	{
		if (!IsServer && IsOwner) //Only send an RPC to the server from the client that owns the NetworkObject of this NetworkBehaviour instance
		{
			ServerOnlyRpc(this, NetworkObjectId);
		}
	}

	[Rpc(SendTo.Server)]
	private void ServerOnlyRpc ( OnlinePlayerInstance _player, ulong _sourceNetworkObjectId )
	{
		ClientAndHostRpc(_player, _sourceNetworkObjectId);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void ClientAndHostRpc( OnlinePlayerInstance _player, ulong _sourceNetworkObjectId )
	{
		GameManager.Instance.Lobby.AddPlayerInstance(_player);
	}*/

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
