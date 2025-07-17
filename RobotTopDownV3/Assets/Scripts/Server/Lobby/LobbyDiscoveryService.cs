using System;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Community.Discovery;
using UnityEngine.Events;

public class LobbyDiscoveryService : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    public Action<IPEndPoint, DiscoveryResponseData> onLobbyDiscovered;

    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    public string ServerName = "EnterName";

    private bool m_HasStartedWithServer = false;


    public void Update ()
    {
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }

    protected override bool ProcessBroadcast ( IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response )
    {
        response = new DiscoveryResponseData()
        {
            ServerName = ServerName,
            Port = ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected override void ResponseReceived ( IPEndPoint sender, DiscoveryResponseData response )
    {
        onLobbyDiscovered.Invoke(sender, response);
    }
}