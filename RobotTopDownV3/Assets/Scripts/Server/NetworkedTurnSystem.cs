using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
public class NetworkedTurnSystem : NetworkBehaviour
{
	[SerializeField] private SerializableDictionary<Entity, Queue<TurnManager.RecordedAction>> m_recordedActionInput = new();
	public SerializableDictionary<Entity, Queue<TurnManager.RecordedAction>> RecordedActions => m_recordedActionInput;


    // Fonction pour envoyer une liste d'actions au serveur
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
    }

}