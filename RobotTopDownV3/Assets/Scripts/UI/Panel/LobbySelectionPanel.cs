using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Community.Discovery;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class LobbySelectionPanel : AUIPanel
{
    [SerializeField] private BaseButton m_createLobbyBtn;
    [SerializeField] private BaseButton m_openLobbySettingsBtn;
    [SerializeField] private Transform m_lobbyListContainer;
    [SerializeField] private GameObject m_noLobbiesFoundMessage;

    private readonly List<LobbyDisplay> m_lobbies = new();
    Dictionary<IPEndPoint, DiscoveryResponseData> discoveredServers = new();
    private Coroutine m_updateCR;

    private void Awake ()
    {
        m_createLobbyBtn.onClick += OnClickCreateLobby;
        m_openLobbySettingsBtn.onClick += OnClickOpenLobbySettings;
    }

    protected override void OnShowFinished ()
    {
        base.OnShowFinished();

        // Écoute des réponses
        NetworkedGameManager.Instance.LobbyService.onLobbyDiscovered += OnServerDiscovered;
        NetworkedGameManager.Instance.LobbyService.StartClient();

        if (m_updateCR != null)
            StopCoroutine(m_updateCR);
        m_updateCR = StartCoroutine(UpdateCR());
    }

    protected override void OnHideStarted ()
    {
        base.OnHideStarted();

        if (m_updateCR != null)
            StopCoroutine(m_updateCR);

        NetworkedGameManager.Instance.LobbyService.onLobbyDiscovered -= OnServerDiscovered;
        NetworkedGameManager.Instance.LobbyService.StopDiscovery();
        ClearLobbies();
    }

    private IEnumerator UpdateCR ()
    {
        while (true)
        {
            discoveredServers.Clear();
            NetworkedGameManager.Instance.LobbyService.ClientBroadcast(new DiscoveryBroadcastData());
            yield return new WaitForSeconds(5f);
        }
    }

    private void OnClickCreateLobby ()
    {
        ushort port = 0;
        try
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] UDPendpoints = properties.GetActiveUdpListeners();
            for (int i = 0; i < 10; i++)
            {
                port = (ushort)(7770 + i);
                if (Array.Find<IPEndPoint>(UDPendpoints, ep =>
                {
                    return ep.Port == port;
                }) == null) break;
            }
        }
        catch (NotImplementedException)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    port = (ushort)(7770 + i);
                    UdpClient m_Client = new UdpClient(port);
                    m_Client.Dispose();
                }
                catch (Exception e)
                {
                    // do nothing - assuming this is a port clash
                    continue;
                }
                // if we get here - it worked
                break;
            }
        }
        UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.SetConnectionData("0.0.0.0", port);
        NetworkManager.Singleton.StartHost();
        NetworkedGameManager.Instance.LobbyService.StartServer();

        if (m_updateCR != null)
            StopCoroutine(m_updateCR);
        UIManager.Instance.OpenPanel<InGamePanel>();
    }

    private void OnClickOpenLobbySettings ()
    {
        UIManager.Instance.OpenPopup<LobbySettingsPopup>();
    }

    private void OnServerDiscovered ( IPEndPoint sender, DiscoveryResponseData response )
    {
        discoveredServers[sender] = response;

        LobbyDisplay newDisplay = Instantiate(GameAssets.current.ui.baseLobbyDisplay, m_lobbyListContainer);
        newDisplay.Setup(response.ServerName, () =>
        {
            Debug.Log($"[Lobby] Connexion à {response.ServerName}...");
            UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            transport.SetConnectionData(sender.Address.ToString(), response.Port);
            NetworkManager.Singleton.StartClient();
            UIManager.Instance.OpenPanel<InGamePanel>();
        }, sender);
        m_lobbies.Add(newDisplay);

        if (m_noLobbiesFoundMessage != null)
            m_noLobbiesFoundMessage.SetActive(false);
    }

    private void ClearLobbies ()
    {
        foreach (LobbyDisplay display in m_lobbies)
        {
            Destroy(display.gameObject);
        }
        m_lobbies.Clear();

        if (m_noLobbiesFoundMessage != null)
            m_noLobbiesFoundMessage.SetActive(true);
    }
}