using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*using Unity.Netcode.Community.Discovery;*/

public class LobbyDiscoveryServer : MonoBehaviour
{
	/*public NetworkManager networkManager;
	public UnityTransport transport;
	public NetworkDiscovery networkDiscovery;

	void Awake ()
	{
		networkManager = GetComponent<NetworkManager>();
		transport = GetComponent<UnityTransport>();
		networkDiscovery = GetComponent<NetworkDiscovery>();

		networkDiscovery.OnStartServerDiscovery();
		networkDiscovery.StartServer();
	}

	public void StartHost ()
	{
		transport.SetConnectionData("0.0.0.0", transport.HostPort);
		networkManager.StartHost();
		networkDiscovery.AdvertiseServer();
	}

	void OnDestroy ()
	{
		networkDiscovery.StopServer();
	}*/
}
