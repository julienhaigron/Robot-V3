using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
	[Header("Params")]
	public bool PlayOnboarding = false;

	[HideInInspector] public bool IsGameStarted = false;

	[Header("References")]
	//[SerializeField] LobbyUIMode _lobbyUIMode;
	//public LobbyUIMode LobbyUIMode { get { return _lobbyUIMode; } }

	//Server only, used temporarily to track connected clients before game start
	readonly List<NetworkConnection> m_serverConnections = new();

	//Server only, number of client set up and ready to start
	int m_readyCount = 0;

	//List of all connected clients
	readonly List<NetworkConnection> m_connections = new();
	public List<NetworkConnection> Connections { get => m_connections; }

	//Owned network connection of this client
	NetworkConnection m_ownedConnection;
	public NetworkConnection OwnedConnection { get => m_ownedConnection; set => m_ownedConnection = value; }

	//List if all player instances, setup when the game starts
	readonly List<OnlinePlayerInstance> m_playerInstances = new();
	public List<OnlinePlayerInstance> PlayerInstances { get => m_playerInstances; }

	//Owned PlayerInstance object of this client
	OnlinePlayerInstance m_ownedPlayerInstance;
	public OnlinePlayerInstance OwnedPlayerInstance { get => m_ownedPlayerInstance; set => m_ownedPlayerInstance = value; }

	//Current number of connected clients
	int m_connectionCount = 0;
	public int ConnectionCount { get => m_connectionCount; }

	List<string> m_connectedUsernames = new();
	public List<string> ConnectedUsernames { get => m_connectedUsernames; }

	//Called every time the connection count changes
	public static Action<int> ChangeConnectionCount;
	public static Action<List<string>> ChangeConnectedUsernames;
	public static Action ClientConnect;

	//Called right before the game starts, used to set all server related variables on clients
	public static Action ClientSetup;

	#region Lobby Creation

	[ServerRpc(RequireOwnership = false)]
	public void ServerAddConnection ( NetworkConnection conn )
	{
		if (!IsServerInitialized)
			return;

		m_serverConnections.Add(conn);

		ObserversUpdatePlayerCount(m_serverConnections.Count);

		if (m_serverConnections.Count == 3)
		{
			foreach (NetworkConnection connection in m_serverConnections)
			{
				ObserversAddConnection(connection);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ServerRemoveConnection ( NetworkConnection conn )
	{
		if (!IsServerInitialized)
			return;

		m_serverConnections.Remove(conn);

		ObserversUpdatePlayerCount(m_serverConnections.Count);

		//Pause the game if already started
		//GameManager.AnalyticsManager.GameEnd();
	}

	public void AddPlayerInstance ( OnlinePlayerInstance instance )
	{
		m_playerInstances.Add(instance);

		if (m_playerInstances.Count == 3)
			m_playerInstances.OrderBy(index => index.LocalConnection.ClientId);
	}

	[ObserversRpc]
	public void ObserversUpdatePlayerCount ( int count )
	{
		m_connectionCount = count;
		ChangeConnectionCount?.Invoke(m_connectionCount);
		ClientConnect?.Invoke();
	}

	[ObserversRpc]
	public void ObserversAddConnection ( NetworkConnection conn )
	{
		m_connections.Add(conn);
	}

	[ObserversRpc]
	public void ObserversSetupClient ()
	{
		ClientSetup?.Invoke();
	}

	[ServerRpc(RequireOwnership = false)]
	public void ServerAddPlayerReady ()
	{
		if (!IsServerInitialized)
			return;

		m_readyCount++;

		if (m_readyCount == 3)
		{
			ObserversInitGame();

			//GameManager.NetworkInteractions.ServerSetDefaultVoiceVolume();

			//TODO : Start Game

			/*if (PlayOnboarding)
			{
				GameManager.OnboardingManager.ObserversStartOnboarding();
				GameManager.AudioManager.StartTransitionMusic();
			}
			else
			{
				GameManager.MainStory.PlayNextEvent(new IntroductionStoryEvent());
				GameManager.AudioManager.StartTransitionMusic();
			}*/
		}
	}

	[ObserversRpc]
	void ObserversInitGame ()
	{
		/*_lobbyUIMode.DefaultCamera.SetActive(false);

		if (!PlayOnboarding)
		{
			PlayerClass[] playerClasses = new PlayerClass[] { PlayerClass.Mage, PlayerClass.Ranger, PlayerClass.Warrior };
			for (int i = 0; i < _playerInstances.Count; i++)
			{
				_playerInstances[i].ApplyHeroClass(playerClasses[i]);
			}

			GameManager.LevelElements.SetupOtherPlayersVisuals();
			SetNameplates();
			GameManager.PostProcessManager.FadeToBlack(false);
		}
		else
		{
			GameManager.ChapterManager.Book.CloseBook(true);
		}

		PlayerInstance.Self.CameraController.Enable();*/


		IsGameStarted = true;
	}

	[ServerRpc(RequireOwnership = false)]
	public void ServerAddUsername ( string username )
	{
		if (!IsServerInitialized)
			return;

		m_connectedUsernames.Add(username);
		ObserversUpdateConnectedUsernames(m_connectedUsernames.ToArray());
	}

	[ObserversRpc]
	private void ObserversUpdateConnectedUsernames ( string[] connectedUsername )
	{
		m_connectedUsernames = connectedUsername.ToList();
		ChangeConnectedUsernames?.Invoke(m_connectedUsernames);
	}

	/*public void SetNameplates()
	{
		for (int i = 0; i < _playerInstances.Count; i++)
		{
			_playerInstances[i].Nameplate.text = _connectedUsernames[i];
		}
	}*/

	//[ObserversRpc]
	//void ObserversStartGame()
	//{
	//	if (!IsServer)
	//		return;

	//	if (_initWayfarerAI)
	//		GameManager.WayfarerAI.Init();

	//	GameManager.MainStory.PlayNextEvent(new IntroductionStoryEvent());
	//}

	#endregion

	#region Instances getters

	/*public PlayerInstance GetPlayerLeftOf(PlayerInstance player)
	{
		int positionIndex = 0;

		for (int i = 0; i < PlayerInstances.Count; i++)
		{
			if (PlayerInstances[i] == player)
				positionIndex = i;
		}

		return PlayerInstances[(positionIndex + 1) % 3];
	}

	public NetworkConnection GetPlayerLeftOf(NetworkConnection player)
	{
		int positionIndex = 0;

		for (int i = 0; i < Connections.Count; i++)
		{
			if (Connections[i] == player)
			{
				positionIndex = i;

				return Connections[(positionIndex + 1) % 3];
			}
		}

		return Connections[(positionIndex + 1) % 3];
	}

	public PlayerInstance GetPlayerRightOf(PlayerInstance player)
	{
		int positionIndex = 0;

		for (int i = 0; i < PlayerInstances.Count; i++)
		{
			if (PlayerInstances[i] == player)
				positionIndex = i;
		}

		return PlayerInstances[(positionIndex + 2) % 3];
	}

	public NetworkConnection GetPlayerRightOf(NetworkConnection player)
	{
		int positionIndex = 0;

		for (int i = 0; i < Connections.Count; i++)
		{
			if (Connections[i] == player)
			{
				positionIndex = i;
				return Connections[(positionIndex + 2) % 3];
			}
		}

		return Connections[(positionIndex + 2) % 3];
	}*/

	public NetworkConnection GetNetworkConnectionFromPlayerInstance ( OnlinePlayerInstance playerInstance )
	{
		for (int i = 0; i < PlayerInstances.Count; i++)
		{
			if (PlayerInstances[i] == playerInstance)
			{
				return Connections[i];
			}
		}
		return null;
	}

	public OnlinePlayerInstance GetPlayerInstanceFromNetworkConnection ( NetworkConnection networkConnection )
	{
		for (int i = 0; i < Connections.Count; i++)
		{
			if (Connections[i] == networkConnection)
			{
				return PlayerInstances[i];
			}
		}
		return null;
	}

	#endregion
}