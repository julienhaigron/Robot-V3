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

    private int m_disconnectedPlayerIndex = -1;
    private ulong m_disconnectedClientId;
    public bool IsWaitingForReconnection => m_disconnectedPlayerIndex != -1;

    private Coroutine m_reconnectionCR;

    public override void OnNetworkSpawn ()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Si le host est aussi un joueur (ce qui est souvent le cas)
            connectedClients.Add(NetworkManager.Singleton.LocalClientId);

            TryStartGame();
        }
    }

    public override void OnNetworkDespawn ()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected ( ulong clientId )
    {
        connectedClients.Add(clientId);
        Debug.Log($"[Server] Client connecte: {clientId}");

        if (IsWaitingForReconnection)
        {
            NetworkTaskOrchestrator.Instance.ReplacePendingClient(m_disconnectedClientId, clientId);
            StopWaitingForReconnection();
            ResumeAfterDisconnectionRPC(_isReconnection: true);
            return;
        }

        TryStartGame();
    }

    private void OnClientDisconnected ( ulong clientId )
    {
        connectedClients.Remove(clientId);

        if (!GameManager.Instance.IsOnline || TurnManager.Instance == null
            || TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Off || IsWaitingForReconnection)
            return;

        int disconnectedPlayerIndex = GetPlayerIndexOf(clientId);
        if (disconnectedPlayerIndex == -1)
            return;

        m_disconnectedPlayerIndex = disconnectedPlayerIndex;
        m_disconnectedClientId = clientId;
        m_reconnectionCR = StartCoroutine(WaitForReconnectionCR());

        PauseForDisconnectionRPC(disconnectedPlayerIndex, GameConfig.current.online.disconnectionWaitDuration);
    }

    private int GetPlayerIndexOf ( ulong _clientId )
    {
        foreach (KeyValuePair<int, OnlinePlayerInstance> pair in Players)
        {
            if (pair.Value != null && pair.Value.OwnerClientId == _clientId)
                return pair.Key;
        }

        return -1;
    }

    private void StopWaitingForReconnection ()
    {
        m_disconnectedPlayerIndex = -1;

        if (m_reconnectionCR != null)
            StopCoroutine(m_reconnectionCR);

        m_reconnectionCR = null;
    }

    private IEnumerator WaitForReconnectionCR ()
    {
        yield return new WaitForSecondsRealtime(GameConfig.current.online.disconnectionWaitDuration);

        //Read before clearing, and never StopCoroutine on ourselves here.
        int forfeitingPlayerIndex = m_disconnectedPlayerIndex;
        m_disconnectedPlayerIndex = -1;
        m_reconnectionCR = null;

        ResumeAfterDisconnectionRPC(_isReconnection: false);
        NetworkedGameManager.Instance.NetworkedTurnSystem.EndRoundClientRPC(true
            , _playerOneWin: forfeitingPlayerIndex == 1, _playerTwoWin: forfeitingPlayerIndex == 0);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PauseForDisconnectionRPC ( int _disconnectedPlayerIndex, float _waitDuration )
    {
        Time.timeScale = 0f;
        UIManager.Instance.OpenPopup<DisconnectionPopup>().Init(_disconnectedPlayerIndex, _waitDuration);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResumeAfterDisconnectionRPC ( bool _isReconnection )
    {
        Time.timeScale = 1f;
        UIManager.Instance.ClosePopup<DisconnectionPopup>();

        //The returning client resumes mid tick and answers the barrier on its own, but if its tick had
        //already finished before the socket dropped, that answer was lost and has to go out again.
        if (_isReconnection && TurnManager.Instance != null)
            TurnManager.Instance.NotifyTickCompletionIfDone();
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
            LogConsole.AddLog("[Server] Tous les clients sont connectes. Lancement de la partie dans 1 seconde...", LogConsole.LogEventType.DebugSys);
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart ()
    {
        yield return new WaitForSeconds(1f); // TODO : loading screen later

        StartClientsGameServerRPC();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartClientsGameServerRPC ()
    {
        StartClientsGameClientRPC();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void StartClientsGameClientRPC ()
	{
		GameManager.Instance.StartGame();
	}

}
