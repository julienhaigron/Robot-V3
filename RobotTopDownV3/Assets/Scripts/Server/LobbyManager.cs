using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour
{
	public Dictionary<ulong, OnlinePlayerInstance> Players = new();
    public OnlinePlayerInstance OwnedPlayerInstance;

    public void AddPlayerInstance ( OnlinePlayerInstance _player, bool _isOwn )
	{
		Players.Add(_player.NetworkObjectId, _player);
		if (_isOwn)
			OwnedPlayerInstance = _player;
	}

}
