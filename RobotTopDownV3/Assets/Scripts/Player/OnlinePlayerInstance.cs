using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OnlinePlayerInstance : NetworkBehaviour
{
	public static OnlinePlayerInstance Self => GameManager.Instance.Lobby.OwnedPlayerInstance;

	public NetworkVariable<int> connectionIndex; //host = 0 , other = 1

	#region server connection

	public override void OnNetworkSpawn ()
	{
		connectionIndex.Value = IsHost ? 0 : 1;
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

	[ServerRpc(RequireOwnership = false)]
	public void SendActionsToServerRPC ( int _senderPlayerID, TurnManager.RecordedAction[] recordedActions )
	{
		Debug.Log("Received actions from player " + _senderPlayerID);
		foreach (TurnManager.RecordedAction action in recordedActions) 
		{
			TurnManager.Instance.AddAction(_senderPlayerID, action.action, action.entityState);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void EndInputPhaseRPC ( int _senderPlayerID, TurnManager.RecordedAction[] recordedActions )
	{
		Debug.Log("Received actions from player " + _senderPlayerID);
		foreach (TurnManager.RecordedAction action in recordedActions) 
		{
			TurnManager.Instance.AddAction(_senderPlayerID, action.action, action.entityState);
		}
	}

	#endregion
}
