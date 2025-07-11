using System;
using System.Net;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Community.Discovery;

public class LobbyDiscoveryService : NetworkDiscovery<LobbyDiscoveryService.DiscoveryBroadcast, LobbyDiscoveryService.DiscoveryAnswer>
{
    private string gameName = "Ma Partie LAN";

    public struct DiscoveredServer
    {
        public IPEndPoint EndPoint;
        public string GameName;
    }

    public event Action<DiscoveredServer> onServerDiscovered;

    [Serializable]
    public class DiscoveryBroadcast : INetworkSerializable
    {
        public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter { }
    }

    [Serializable]
    public class DiscoveryAnswer : INetworkSerializable
    {
        public string gameName;

        public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
        {
            serializer.SerializeValue(ref gameName);
        }
    }

    /// <summary>
    /// Lance en mode serveur (répond aux broadcasts)
    /// </summary>
    public void StartAsServer ( string customGameName = null )
    {
        if (!string.IsNullOrEmpty(customGameName))
            gameName = customGameName;

        StartServer();
    }

    /// <summary>
    /// Lance en mode client (envoie les broadcasts)
    /// </summary>
    public void StartAsClient ()
    {
        StartClient();
        SendDiscoveryRequest();
    }

    /// <summary>
    /// Réinitialise le composant (client ou serveur)
    /// </summary>
    public void Stop ()
    {
        StopDiscovery();
    }

    public void SendDiscoveryRequest ()
    {
        if (IsClient)
        {
            ClientBroadcast(new DiscoveryBroadcast());
        }
    }

    protected override bool ProcessBroadcast ( IPEndPoint sender, DiscoveryBroadcast broadcast, out DiscoveryAnswer response )
    {
        response = new DiscoveryAnswer { gameName = gameName };
        return true; // on répond toujours avec le nom de la partie
    }

    protected override void ResponseReceived ( IPEndPoint sender, DiscoveryAnswer response )
    {
        onServerDiscovered?.Invoke(new DiscoveredServer
        {
            EndPoint = sender,
            GameName = response.gameName
        });
    }

    public void JoinDiscoveredServer ( DiscoveredServer server )
    {
        NetworkedGameManager.Instance.Transport.SetConnectionData(server.EndPoint.Address.ToString(), (ushort)server.EndPoint.Port);
        NetworkManager.Singleton.StartClient();
    }

    private void OnDestroy ()
    {
        Stop();
    }
}
