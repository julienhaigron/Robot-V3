using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
public class NetworkedTurnSystem : NetworkBehaviour
{
	[SerializeField] private SerializableDictionary<Entity, Queue<TurnManager.RecordedAction>> m_recordedActionInput = new();
	public SerializableDictionary<Entity, Queue<TurnManager.RecordedAction>> RecordedActions => m_recordedActionInput;
    public static Dictionary<int, Entity> Entities = new();

    #region Fncts

    public void AddEntity(Entity _entity )
    {
        Entities.Add(_entity.ID, _entity);
    }

    #endregion

    #region Converters

    #region Tile
    public static int ConvertTileToInt (Tile _tile)
    {
        int tile = _tile.coordinates.ID;

        return tile;
    }

    public static Tile ConvertIntToTile (int _int)
    {
        Tile tile = GridManager.Instance.Tiles[_int];

        return tile;
    }

    #endregion

    #region Entity

    public static int ConvertEntityToInt ( Entity _entity )
    {
        int entity = _entity.ID;

        return entity;
    }

    public static Entity ConvertIntToEntity ( int _int )
    {
        Entity entity = Entities[_int];

        return entity;
    }

    #endregion

    #endregion

    // Fonction pour envoyer une liste d'actions au serveur
    public void SendActionsToServer ( List<TurnManager.RecordedAction> recordedActions )
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
    private void SendActionsToServerRPC ( List<TurnManager.RecordedAction> recordedActions )
    {
        HandleActionsOnServer(recordedActions);
    }

    // Traitement de la liste d'actions côté serveur
    private void HandleActionsOnServer ( List<TurnManager.RecordedAction> recordedActions )
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
        Debug.Log("Traitement du déplacement pour l'entité : " + action.performingEntity.Data.name);
        // Vous pouvez utiliser `entityState` pour ajuster l'état de l'entité
    }


    /* public FastBufferWriter ConvertToNetworked ( AEntityAction action )
     {
         FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
         BufferSerializer<FastBufferWriter> serializer = new BufferSerializer<FastBufferWriter>(writer);

         action.NetworkSerialize(serializer);

         return writer;
     }

     public AEntityAction CreateActionFromNetwork ( FastBufferReader reader )
     {
         var serializer = new BufferSerializer<FastBufferReader>(new BufferSerializerReader(reader));

         // Lire le type de l’action en premier
         EntityActionType actionType = EntityActionType.Wait; // Valeur par défaut
         serializer.SerializeValue(ref actionType);

         AEntityAction action = actionType switch
         {
             EntityActionType.NeighborMove => new MoveToNeighborAction(),
             EntityActionType.TargetTileMove => new MoveToNeighborAction(), // À changer si nécessaire
             EntityActionType.Attack => new AttackAction(),
             _ => null
         };

         action?.NetworkSerialize(serializer);
         return action;
     }*/

    /*[ServerRpc(RequireOwnership = false)]
    public void SendActionsServerRpc ( NetworkedAction[] actions, ulong clientId )
    {
        foreach (var action in actions)
        {
            // Récupérer l'entité avec action.EntityId
            Entity entity = FindEntityById(action.EntityId);
            if (entity != null)
            {
                TurnManager.Instance.AddAction(entity, action.Action.action, action.Action.entityState);
            }
        }
    }

    public void SendRecordedActions ()
    {
        var recordedActions = TurnManager.Instance.RecordedActions;
        List<NetworkedAction> networkedActions = new();

        foreach (var entry in recordedActions)
        {
            int entityId = entry.Key.GetInstanceID(); // Ou un autre identifiant unique
            foreach (var action in entry.Value)
            {
                networkedActions.Add(new NetworkedAction { EntityId = entityId, Action = action });
            }
        }

        SendActionsServerRpc(networkedActions.ToArray(), NetworkManager.Singleton.LocalClientId);
    }*/

    private Entity FindEntityById ( int entityId )
    {
        // Implémente une recherche de l'entité via son ID
        return null;
    }
}