using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OnlinePlayerInstance : NetworkBehaviour
{
	public static OnlinePlayerInstance Self => GameManager.Instance.Lobby.OwnedPlayerInstance;

	public int connectionIndex; //host = 0 , other = 1

	#region server connection

	public override void OnNetworkSpawn ()
	{
		connectionIndex = (IsOwner == IsHost) ? 0 : 1;

		GameManager.Instance.Lobby.AddPlayerInstance(this, IsOwner);
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
