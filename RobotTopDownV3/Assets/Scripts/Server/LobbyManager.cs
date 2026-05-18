using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour
{
	public Dictionary<int, OnlinePlayerInstance> Players = new();
    public OnlinePlayerInstance OwnedPlayerInstance;

	[SerializeField] private int nbOfPlayer = 2;

    private HashSet<ulong> connectedClients = new();

    public override void OnNetworkSpawn ()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Si le host est aussi un joueur (ce qui est souvent le cas)
            connectedClients.Add(NetworkManager.Singleton.LocalClientId);

            TryStartGame();
        }
    }

    private void OnClientConnected ( ulong clientId )
    {
        connectedClients.Add(clientId);
        Debug.Log($"[Server] Client connecté: {clientId}");

        TryStartGame();
    }

    public void AddPlayerInstance ( OnlinePlayerInstance _player, bool _isOwn )
    {
        Players[_player.connectionIndex] = _player;
        if (_isOwn)
            OwnedPlayerInstance = _player;
    }

    private void TryStartGame ()
    {
        if (connectedClients.Count >= nbOfPlayer)
        {
            LogConsole.AddLog("[Server] Tous les clients sont connectés. Lancement de la partie dans 1 seconde...", LogConsole.LogEventType.DebugSys);
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart ()
    {
        yield return new WaitForSeconds(1f); // TODO : loading screen later

        StartClientsGameServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartClientsGameServerRPC ()
    {
        StartClientsGameClientRPC();
    }

    [ClientRpc(RequireOwnership = false)]
	private void StartClientsGameClientRPC ()
	{
		GameManager.Instance.StartGame();
	}

}
