using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OnlinePlayerInstance : NetworkBehaviour
{
	public static OnlinePlayerInstance Self => GameManager.Instance.Lobby.OwnedPlayerInstance;


	#region server connection

	public override void OnNetworkSpawn ()
	{
		GameManager.Instance.Lobby.AddPlayerInstance(this, IsOwner);
	}

	/*[Rpc(SendTo.Server)]
	private void ServerOnlyRpc ( OnlinePlayerInstance _player, ulong _sourceNetworkObjectId )
	{
		ClientAndHostRpc(_player, _sourceNetworkObjectId);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void ClientAndHostRpc ( OnlinePlayerInstance _player, ulong _sourceNetworkObjectId )
	{
		GameManager.Instance.Lobby.AddPlayerInstance(_player);
	}*/

	#endregion

	#region Turn sys

	public void SendActionsToClients ( int _senderPlayerID, TurnManager.RecordedAction[] recordedActions )
	{
		SendActionsToServerRPC(_senderPlayerID, recordedActions);
	}

	[Rpc(SendTo.Server)]
	private void SendActionsToServerRPC ( int _senderPlayerID, TurnManager.RecordedAction[] recordedActions )
	{
		Debug.Log("Received actions from player " + _senderPlayerID);
		foreach (TurnManager.RecordedAction action in recordedActions) 
		{
			TurnManager.Instance.AddAction(_senderPlayerID, action.action, action.entityState);
		}
	}

	#endregion
}
