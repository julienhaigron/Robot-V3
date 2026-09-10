using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent()]
[RequireComponent(typeof(LoadingElement))]
//Not SingletonPersistant: this sits on a CHILD GameObject, where DontDestroyOnLoad is a no-op and only logs a
//warning. It survives scene loads through its persisting root (GameManager.prefab). Moving it out from under that root
//would silently break that - it would need to become SingletonPersistant again, on a root object.
public partial class ApplicationManager : Singleton<ApplicationManager>
{

	[SerializeField] LoadingElement m_loadingElement;

	[Header("Objects")]
	[SerializeField] private GameAssets m_gameAssets;
	[SerializeField] private GameConfig m_gameConfig;
	[SerializeField] private GameDatas m_gameDatas;

	//The fallbacks must NOT be editor-only: Awake order between singletons is undefined, so anything reading
	//these during its own Awake hits a null Instance. In the editor that was hidden by the fallback, in a build
	//it threw. Resources.Load resolves the same assets (Assets/Objects/Resources) in both.
	public static GameAssets assets
	{
		get
		{
			if (Instance == null)
				return Resources.Load<GameAssets>("GameAssets");

			return Instance.m_gameAssets;
		}
	}
	public static GameConfig config
	{
		get
		{
			if (Instance == null)
				return Resources.Load<GameConfig>("GameConfig");

			return Instance.m_gameConfig;
		}
	}

	public static GameDatas datas
	{
		get
		{
			if (Instance == null)
				return Resources.Load<GameDatas>("GameDatas");

			return Instance.m_gameDatas;
		}
	}

	public override void Awake ()
	{
		base.Awake();
		m_loadingElement.onLoadingStarted += Load;
	}

	private void Load ()
	{
		m_loadingElement.onLoadingStarted -= Load;

		if (Instance.m_gameConfig == null)
		{
			m_loadingElement.EndLoading(false, "Missing config file");
			return;
		}
		if (Instance.m_gameDatas == null)
		{
			m_loadingElement.EndLoading(false, "Missing datas file");
			return;
		}

		int newWidth = Mathf.RoundToInt(Screen.width * .75f);
		int newHeight = Mathf.RoundToInt(Screen.height * .75f);
		Screen.SetResolution(newWidth, newHeight, Screen.fullScreen);

		StartCoroutine(LoadCoroutine());
	}

	IEnumerator LoadCoroutine ()
	{
		config.Initialize();

#if UNITY_EDITOR
		if (m_gameDatas.preventSave)
		{
			if (config.datas.editorLoadDatasFromGamedatasFile)
			{
				m_gameDatas = GameDatas.LoadFromJson(GameDatas.DatasToJson(m_gameDatas));
			}
			else
			{
				m_gameDatas = GameDatas.Load();
			}
			m_gameDatas.preventSave = true;
		}
		else
		{
			if (config.datas.editorLoadDatasFromGamedatasFile)
			{
				m_gameDatas.Initialize();
			}
			else
			{
				m_gameDatas.Load();
			}
		}
#else
        m_gameDatas.Load();
#endif

		m_loadingElement.SetProgress(0.5f);
		yield return null;
		m_loadingElement.EndLoading(true);
	}

	public void SaveApplication ()
	{
		if (m_loadingElement.DidFinishedLoading)
		{
			GameDatas.Save();
		}
	}

	private void OnApplicationQuit ()
	{
		SaveApplication();
	}
}
