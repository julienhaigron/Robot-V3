using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour
{
	public Dictionary<int, OnlinePlayerInstance> Players = new();
    public OnlinePlayerInstance OwnedPlayerInstance;

    public void AddPlayerInstance ( OnlinePlayerInstance _player )
	{
		Players.Add(_player.ConnectionIndex, _player);
	}

}
