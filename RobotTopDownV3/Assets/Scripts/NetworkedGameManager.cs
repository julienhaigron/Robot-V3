using UnityEngine;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkedGameManager : SingletonPersistant<NetworkedGameManager>
{
    [SerializeField] private NetworkedTurnSystem m_networkedTurnSystem;
    public NetworkedTurnSystem NetworkedTurnSystem => m_networkedTurnSystem;

    [SerializeField] private LobbyManager m_lobbyManager;
    public LobbyManager LobbyManager => m_lobbyManager;

    [SerializeField] private LobbyDiscoveryService m_lobbyService;
    public LobbyDiscoveryService LobbyService => m_lobbyService;

    [SerializeField] private UnityTransport m_transport;
    public UnityTransport Transport => m_transport;

    private Coroutine m_reconnectionCR;

    private void Start ()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy ()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    /// <summary>
    /// This lives on a plain MonoBehaviour on purpose: LobbyManager is a NetworkBehaviour and is despawned
    /// during the client's own disconnection, so it cannot be trusted to still be alive to react to it.
    /// Freezing here mid tick is what lets the resume finish that tick and answer the server barrier
    /// naturally, instead of having to replay anything.
    /// </summary>
    private void OnClientDisconnected ( ulong _clientId )
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer || m_reconnectionCR != null)
            return;

        if (GameManager.Instance == null || !GameManager.Instance.IsOnline || TurnManager.Instance == null
            || TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Off)
            return;

        Time.timeScale = 0f;
        UIManager.Instance.OpenPopup<DisconnectionPopup>()
            .InitForLocalDisconnection(GameConfig.current.online.disconnectionWaitDuration);

        m_reconnectionCR = StartCoroutine(TryReconnectCR());
    }

    private IEnumerator TryReconnectCR ()
    {
        float interval = Mathf.Max(.5f, GameConfig.current.online.reconnectionAttemptInterval);
        //One interval longer than the server window, so the server always gets to decide first and we
        //never give up on a reconnection it would still have accepted.
        float remainingTime = GameConfig.current.online.disconnectionWaitDuration + interval;

        while (remainingTime > 0f)
        {
            yield return new WaitForSecondsRealtime(interval);
            remainingTime -= interval;

            if (NetworkManager.Singleton == null)
                break;

            //The server resumes everyone through its own RPC once it sees us back.
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                m_reconnectionCR = null;
                yield break;
            }

            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartClient();
        }

        m_reconnectionCR = null;
        GiveUpAndReturnToMenu();
    }

    private void GiveUpAndReturnToMenu ()
    {
        Time.timeScale = 1f;
        UIManager.Instance.ClosePopup<DisconnectionPopup>(_instant: true);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        UIManager.Instance.ClosePanel<InGamePanel>(true);
        GameManager.Instance.GoToStartScreen();
    }
}
