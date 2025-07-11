using UnityEngine;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkedGameManager : Singleton<NetworkedGameManager>
{
    [SerializeField] private NetworkedTurnSystem m_networkedTurnSystem;
    public NetworkedTurnSystem NetworkedTurnSystem => m_networkedTurnSystem;

    [SerializeField] private LobbyManager m_lobbyManager;
    public LobbyManager LobbyManager => m_lobbyManager;

    [SerializeField] private LobbyDiscoveryService m_lobbyService;
    public LobbyDiscoveryService LobbyService => m_lobbyService;

    [SerializeField] private UnityTransport m_transport;
    public UnityTransport Transport => m_transport;
}
