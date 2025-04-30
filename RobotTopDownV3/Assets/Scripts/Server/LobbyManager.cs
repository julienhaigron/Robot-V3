using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour
{
	public Dictionary<int, OnlinePlayerInstance> Players = new();
    public OnlinePlayerInstance OwnedPlayerInstance;

	[SerializeField] private int nbOfPlayer = 2;

    public void AddPlayerInstance ( OnlinePlayerInstance _player, bool _isOwn )
	{
		Players.Add(_player.connectionIndex.Value, _player);
		if (_isOwn)
			OwnedPlayerInstance = _player;

		if (IsServer && Players.Count == nbOfPlayer)
			StartClientsGameRPC();
	}

	[ClientRpc(RequireOwnership = false)]
	private void StartClientsGameRPC ()
	{
		GameManager.Instance.StartGame();
	}

}
