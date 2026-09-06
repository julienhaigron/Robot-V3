using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;

public class NetworkedTurnSystem : NetworkBehaviour
{
    //[ClientRpc(RequireOwnership = false)]
    [Rpc(SendTo.ClientsAndHost)]
    public void StartPlayPhaseClientRPC ( TurnManager.RecordedEntityActionsContainer[] _entitiesRecordedActions )
	{
		for (int i = 0; i < _entitiesRecordedActions.Length; i++)
		{
			Queue<TurnManager.RecordedAction> actionQueue = new Queue<TurnManager.RecordedAction>();
			foreach (TurnManager.RecordedAction action in _entitiesRecordedActions[i].actions)
			{
				actionQueue.Enqueue(action);
                LogConsole.AddLog("Action received: " + action.action.ToString(), LogConsole.LogEventType.InputPhase);
            }

            if (TurnManager.Instance.ActionsToPlay.ContainsKey(_entitiesRecordedActions[i].entityId))
                TurnManager.Instance.ActionsToPlay[_entitiesRecordedActions[i].entityId] = actionQueue;
            else
                TurnManager.Instance.ActionsToPlay.Add(_entitiesRecordedActions[i].entityId, actionQueue);
		}

        TurnManager.Instance.PlayThisRoundActions();
	}

    //[ClientRpc(RequireOwnership = false)]
    [Rpc(SendTo.ClientsAndHost)]
    public void EndRoundClientRPC (bool _isFinished, bool _playerOneWin, bool _playerTwoWin )
	{
		if (_isFinished)
		{
            TurnManager.Instance.EndLevel(TurnManager.Instance.GetLocalGameResult(_playerOneWin, _playerTwoWin));
		}
		else
		{
            TurnManager.Instance.StartInputPhase();
		}
	}



	#region Old
	/*// Fonction pour envoyer une liste d'actions au serveur
    public void SendActionsToServer ( TurnManager.RecordedAction[] recordedActions )
    {
        if (IsServer)
        {
            // Si nous sommes d�j� sur le serveur, nous traitons la liste directement
            HandleActionsOnServer(recordedActions);
        }
        else
        {
            // Sinon, nous appelons un RPC pour envoyer la liste au serveur
            SendActionsToServerRPC(recordedActions);
        }
    }

    // RPC pour envoyer la liste au serveur
    [ServerRpc(RequireOwnership = false)]
    private void SendActionsToServerRPC ( TurnManager.RecordedAction[] recordedActions )
    {
        HandleActionsOnServer(recordedActions);
    }

    // Traitement de la liste d'actions c�t� serveur
    private void HandleActionsOnServer ( TurnManager.RecordedAction[] recordedActions )
    {
        foreach (var recordedAction in recordedActions)
        {
            // Traitement de chaque action
            Debug.Log("Traitement de l'action : " + recordedAction.action);

            // Exemple de traitement sp�cifique � une action
            if (recordedAction.action is MoveToNeighborAction moveAction)
            {
                ProcessMoveAction(moveAction, recordedAction.entityState);
            }
            else
            {
                Debug.LogError("Type d'action non pris en charge : " + recordedAction.action.GetType());
            }
        }
    }

    // Traitement sp�cifique d'un d�placement
    private void ProcessMoveAction ( MoveToNeighborAction action, Entity.EntityState entityState )
    {
        // Impl�menter ici la logique sp�cifique pour traiter l'action de type MoveToNeighborAction
        Debug.Log("Traitement du d�placement pour l'entit� : " + GameManager.Instance.GetEntityFromID(action.performingEntityID).Data.name);
        // Vous pouvez utiliser `entityState` pour ajuster l'�tat de l'entit�
    }*/
	#endregion

}