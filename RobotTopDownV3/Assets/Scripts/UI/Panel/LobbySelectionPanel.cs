using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LobbySelectionPanel : AUIPanel
{
    [SerializeField] private BaseButton m_createLobbyBtn;
    [SerializeField] private BaseButton m_openLobbySettingsBtn;
    [SerializeField] private Transform m_lobbyListContainer;

    private readonly List<LobbyDisplay> m_lobbies = new();
    private Coroutine m_updateCR;

    private void Awake ()
    {
        m_createLobbyBtn.onClick += OnClickCreateLobby;
        m_openLobbySettingsBtn.onClick += OnClickOpenLobbySettings;
    }

    protected override void OnShowFinished ()
    {
        base.OnShowFinished();

        // Start discovery as client
        NetworkedGameManager.Instance.LobbyService.onServerDiscovered += OnServerDiscovered;
        NetworkedGameManager.Instance.LobbyService.StartAsClient();

        if (m_updateCR != null)
            StopCoroutine(m_updateCR);
        m_updateCR = StartCoroutine(UpdateCR());
    }

    protected override void OnHideStarted ()
    {
        base.OnHideStarted();

        if (m_updateCR != null)
            StopCoroutine(m_updateCR);

        NetworkedGameManager.Instance.LobbyService.onServerDiscovered -= OnServerDiscovered;
        NetworkedGameManager.Instance.LobbyService.Stop();
        ClearLobbies();
    }

    private IEnumerator UpdateCR ()
    {
        while (true)
        {
            NetworkedGameManager.Instance.LobbyService.SendDiscoveryRequest(); // resend broadcast
            yield return new WaitForSeconds(5f);
        }
    }

    private void OnClickCreateLobby ()
    {
        string lobbyName = UIManager.Instance.GetPopup<LobbySettingsPopup>().LobbyName;
        ushort port = UIManager.Instance.GetPopup<LobbySettingsPopup>().Port;
        NetworkedGameManager.Instance.Transport.SetConnectionData("0.0.0.0", port);
        NetworkedGameManager.Instance.LobbyService.StartAsServer(lobbyName);

        NetworkManager.Singleton.StartHost();

        // Optionnel : cacher le panel ou charger la scène du lobby
        Debug.Log("Lobby créé !");
    }

    private void OnClickOpenLobbySettings ()
    {
        UIManager.Instance.OpenPopup<LobbySettingsPopup>();
    }

    private void OnServerDiscovered ( LobbyDiscoveryService.DiscoveredServer server )
    {
        // Ignore duplicatas
        if (m_lobbies.Any(l => l.Matches(server.EndPoint)))
            return;

        var newDisplay = Instantiate(GameAssets.current.ui.baseLobbyDisplay, m_lobbyListContainer);
        newDisplay.Setup(server.GameName, () =>
        {
            Debug.Log($"Rejoindre {server.GameName}");
            NetworkedGameManager.Instance.LobbyService.JoinDiscoveredServer(server);
        }, server.EndPoint);

        m_lobbies.Add(newDisplay);
    }

    private void ClearLobbies ()
    {
        foreach (var display in m_lobbies)
        {
            Destroy(display.gameObject);
        }
        m_lobbies.Clear();
    }
}