using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.SceneManagement.SceneManager;

public class NetworkSceneManager : NetworkBehaviour
{
	string m_currentSceneName = string.Empty;

	[Header("References")]
	[SerializeField] GameObject m_vfxPrefab;
	[SerializeField] GameObject m_particlePrefab;
	[SerializeField] List<Transform> m_vfxPositions;
	[Space]
	[SerializeField] GameObject m_defaultRoom;
	[SerializeField] GameObject m_oldEnvironment;
	[SerializeField] GameObject m_newEnvironment;

	List<GameObject> m_vfxInScene = new();
	List<GameObject> m_particlesInScene = new();

	//Scene _newScene;
	//bool _didLoadOnce = false;
	//bool _areEyesBlinking;

	#region Public Methods

	public void ReloadScene ()
	{
		//Destroy(GameManager.Connector.gameObject);
		Destroy(GameManager.Instance.gameObject);
		LoadScene(GetActiveScene().name);
	}

	public void LoadAdditiveSceneForAll ( string sceneName )
	{
		NetworkConnection[] connections = GameManager.Instance.Lobby.Connections.ToArray();
		LoadAdditiveSceneForClients(sceneName, connections);
	}

	public void LoadAdditiveSceneForClients ( string sceneName, NetworkConnection[] connections )
	{
		if (string.IsNullOrEmpty(sceneName))
			return;

		ServerLoadAdditiveScene(connections, sceneName);
	}

	public void UnloadAdditiveScene ()
	{
		ServerUnloadAdditiveScene();
	}

	#endregion

	#region Server Methods

	[ServerRpc(RequireOwnership = false)]
	void ServerLoadAdditiveScene ( NetworkConnection[] connections, string sceneName )
	{
		if (!IsServerInitialized)
			return;

		if (m_currentSceneName != string.Empty)
		{
			//LogError("NetworkSceneManager can not handle multiple additive scenes.");
			return;
		}
		m_currentSceneName = sceneName;

		foreach (NetworkConnection connection in connections)
		{
			TargetLoadAdditiveScene(connection, sceneName);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	void ServerUnloadAdditiveScene ()
	{
		if (!IsServerInitialized)
			return;

		if (m_currentSceneName == string.Empty)
		{
			//LogError("NetworkSceneManager can not unload a scene if no scene is loaded.");
			return;
		}

		ObserversUnloadAdditiveScene(m_currentSceneName);

		m_currentSceneName = string.Empty;
	}

	#endregion

	#region Client Methods

	[TargetRpc]
	void TargetLoadAdditiveScene ( NetworkConnection conn, string sceneName )
	{
		//GameManager.PostProcessManager.BlinkPlayerView(GameManager.DialogManager.GetTotalDialogReadingTime());
		LoadSceneAsync(sceneName, LoadSceneMode.Additive);
	}

	[ObserversRpc]
	void ObserversUnloadAdditiveScene ( string sceneName )
	{
		if (GetSceneByName(sceneName).isLoaded)
		{
			UnloadSceneAsync(sceneName);
		}
	}

	#endregion
}
