using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OnlinePlayerInstance : NetworkBehaviour
{
	public static OnlinePlayerInstance Self => GameManager.Instance.Lobby.OwnedPlayerInstance;

	public int connectionIndex; //host = 0 , other = 1

    public PlayerSettingsInfo infos;

	public class PlayerSettingsInfo : INetworkSerializable
	{
		EntitySavedData[] entities;
		
		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref entities);
		}
	}


	#region server connection

	public override void OnNetworkSpawn ()
	{
		connectionIndex = (IsOwner == IsHost) ? 0 : 1;

		//OnNetworkSpawn runs for every player object on every peer, so only the owned one says who we are.
		if (IsOwner)
			GameManager.Instance.PlayerID = connectionIndex;

		GameManager.Instance.Lobby.AddPlayerInstance(this, IsOwner);
	}

	//[ClientRpc(RequireOwnership = false)]
	[Rpc(SendTo.ClientsAndHost)]
	public void SendPlayerInfosClientRPC(int _connectionIndex, PlayerSettingsInfo _infos )
	{
		GameManager.Instance.Lobby.Players[_connectionIndex].infos = _infos;
		LogConsole.AddLog("Player infos sent", LogConsole.LogEventType.PreGame);
	}


	//[ServerRpc(RequireOwnership = false)]
	[Rpc(SendTo.Server)]
	public void EndInputPhaseServerRPC ( TurnManager.RecordedEntityActionsContainer[] _entitiesRecordedActions, RpcParams _rpcParams = default )
    {
        ulong senderClientId = _rpcParams.Receive.SenderClientId;

        if (Self.OwnerClientId != senderClientId)
        {
            for (int i = 0; i < _entitiesRecordedActions.Length; i++)
            {
                Queue<TurnManager.RecordedAction> actionQueue = new Queue<TurnManager.RecordedAction>();
                foreach (TurnManager.RecordedAction action in _entitiesRecordedActions[i].actions)
                    actionQueue.Enqueue(action);

                //Assign rather than Add: a second send from the same client would throw on the duplicate key
                //and abort the RPC before the task is ever notified, hanging the phase.
                TurnManager.Instance.RecordedActions[_entitiesRecordedActions[i].entityId] = actionQueue;
            }
        }

        NetworkTaskOrchestrator.Instance.NotifyClientEndedTaskFromServer("InputPhase", senderClientId);
    }

	#endregion
}
