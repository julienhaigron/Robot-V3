using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTaskOrchestrator : NetworkBehaviour
{
    public static NetworkTaskOrchestrator Instance;

    private class TaskRequest
    {
        public HashSet<ulong> PendingClients;
        public Action OnAllClientsResponded;
    }

    private Dictionary<string, TaskRequest> activeTasks = new();

    private void Awake ()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Lance une tâche réseau vers tous les clients, identifiée par un ID unique.
    /// </summary>
    public void LaunchClientTask ( string requestId, Action onAllClientsResponded )
    {
        if (!IsServer)
        {
            Debug.LogWarning("NetworkTaskOrchestrator: Seul le serveur peut lancer une tâche réseau.");
            return;
        }

        if (activeTasks.ContainsKey(requestId))
        {
            Debug.LogWarning($"NetworkTaskOrchestrator: Une tâche avec l'ID '{requestId}' est déjà en cours.");
            return;
        }

        var pendingClients = new HashSet<ulong>(NetworkManager.ConnectedClientsIds);
        activeTasks[requestId] = new TaskRequest
        {
            PendingClients = pendingClients,
            OnAllClientsResponded = onAllClientsResponded
        };

        //Debug.Log($"[Server] Envoi de la tâche '{requestId}' à {pendingClients.Count} clients.");
        //PerformClientTaskClientRpc(requestId);
    }

    /*[ClientRpc]
    private void PerformClientTaskClientRpc ( string requestId )
    {
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Tâche reçue: {requestId}");

        // Simule une tâche côté client (remplacer avec ta logique)
        //StartCoroutine(SimulateClientResponse(requestId));
    }*/

    /*private IEnumerator SimulateClientResponse ( string requestId )
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));

        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Réponse envoyée pour la tâche '{requestId}'");
        NotifyServerOfCompletionServerRpc(requestId);
    }*/

    //call this when implementing
    [ServerRpc(RequireOwnership = false)]
    public void NotifyTaskEndToServerRPC ( string requestId, ServerRpcParams rpcParams = default )
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!activeTasks.TryGetValue(requestId, out var task))
        {
            Debug.LogWarning($"[Server] Tâche inconnue ou déjà terminée: {requestId}");
            return;
        }

        if (task.PendingClients.Remove(clientId))
        {
            //Debug.Log($"[Server] Réponse reçue de {clientId} pour '{requestId}'. Clients restants: {task.PendingClients.Count}");

            if (task.PendingClients.Count == 0)
            {
                //Debug.Log($"[Server] Tous les clients ont terminé la tâche '{requestId}'");
                task.OnAllClientsResponded?.Invoke();
                activeTasks.Remove(requestId);
            }
        }
    }
}
