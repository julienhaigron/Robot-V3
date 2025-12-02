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

		GameManager.Instance.PlayerID = connectionIndex;
		GameManager.Instance.Lobby.AddPlayerInstance(this, IsOwner);
	}

	[ClientRpc(RequireOwnership = false)]
	public void SendPlayerInfosClientRPC(int _connectionIndex, PlayerSettingsInfo _infos )
	{
		GameManager.Instance.Lobby.Players[_connectionIndex].infos = _infos;
		LogConsole.AddLog("Player infos sent", LogConsole.LogEventType.PreGame);
	}


    [ServerRpc(RequireOwnership = false)]
    public void EndInputPhaseServerRPC ( ulong _senderPlayerID, TurnManager.RecordedEntityActionsContainer[] _entitiesRecordedActions )
    {
        if (Self.OwnerClientId != _senderPlayerID)
        {
            for (int i = 0; i < _entitiesRecordedActions.Length; i++)
            {
                Queue<TurnManager.RecordedAction> actionQueue = new Queue<TurnManager.RecordedAction>();
                foreach (TurnManager.RecordedAction action in _entitiesRecordedActions[i].actions)
                    actionQueue.Enqueue(action);
                TurnManager.Instance.RecordedActions.Add(_entitiesRecordedActions[i].entityId, actionQueue);
            }
        }

        NetworkTaskOrchestrator.Instance.NotifyClientEndedTaskFromServer("InputPhase", _senderPlayerID);
    }

	#endregion
}
