using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;

public class NetworkedTurnSystem : NetworkBehaviour
{

	[ClientRpc(RequireOwnership = false)]
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

        TurnManager.Instance.PlayThisPhaseActions();
	}

	[ClientRpc(RequireOwnership = false)]
	public void EndRoundClientRPC ( bool _isPlayerOneDead, bool _isPlayerTwoDead )
	{
		if (_isPlayerOneDead || _isPlayerTwoDead)
		{
            TurnManager.Instance.EndLevel(!_isPlayerOneDead);
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
            // Si nous sommes déjà sur le serveur, nous traitons la liste directement
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

    // Traitement de la liste d'actions côté serveur
    private void HandleActionsOnServer ( TurnManager.RecordedAction[] recordedActions )
    {
        foreach (var recordedAction in recordedActions)
        {
            // Traitement de chaque action
            Debug.Log("Traitement de l'action : " + recordedAction.action);

            // Exemple de traitement spécifique à une action
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

    // Traitement spécifique d'un déplacement
    private void ProcessMoveAction ( MoveToNeighborAction action, Entity.EntityState entityState )
    {
        // Implémenter ici la logique spécifique pour traiter l'action de type MoveToNeighborAction
        Debug.Log("Traitement du déplacement pour l'entité : " + GameManager.Instance.GetEntityFromID(action.performingEntityID).Data.name);
        // Vous pouvez utiliser `entityState` pour ajuster l'état de l'entité
    }*/
	#endregion

}