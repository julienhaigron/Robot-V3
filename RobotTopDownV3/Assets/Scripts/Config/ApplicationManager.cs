using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent()]
[RequireComponent(typeof(LoadingElement))]
public partial class ApplicationManager : SingletonPersistant<ApplicationManager>
{

	LoadingElement m_loadingElement;

	[Header("Objects")]
	[SerializeField] private GameAssets m_gameAssets;
	[SerializeField] private GameConfig m_gameConfig;
	[SerializeField] private GameDatas m_gameDatas;

	public static GameAssets assets
	{
		get
		{
#if UNITY_EDITOR
			if (Instance == null)
				return Resources.Load<GameAssets>("GameAssets");
#endif
			return Instance.m_gameAssets;
		}
	}
	public static GameConfig config
	{
		get
		{
#if UNITY_EDITOR
			if (Instance == null)
				return Resources.Load<GameConfig>("GameConfig");
#endif
			return Instance.m_gameConfig;
		}
	}

	public static GameDatas datas
	{
		get
		{
#if UNITY_EDITOR
			if (Instance == null)
				return Resources.Load<GameDatas>("GameDatas");
#endif
			return Instance.m_gameDatas;
		}
	}

	public override void Awake ()
	{
		base.Awake();
		m_loadingElement = GetComponent<LoadingElement>();
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

		//ApplicationManager.config.app.UpdateFramerate();

		int newWidth = Mathf.RoundToInt(Screen.width * .75f);
		int newHeight = Mathf.RoundToInt(Screen.height * .75f);
		Screen.SetResolution(newWidth, newHeight, Screen.fullScreen);

		StartCoroutine(LoadCoroutine());
	}

	IEnumerator LoadCoroutine ()
	{
		config.Initialize();
		assets.Initialize();

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

		//GameDatas.current.player.RefreshStatsValues();
		m_loadingElement.SetProgress(0.5f);
		yield return null;
		//TimeManager.InitTimeManager();
		m_loadingElement.EndLoading(true);
	}

	public void SaveApplication ()
	{
		if (m_loadingElement.loadingProcessFinished)
		{
			GameDatas.Save();
		}
	}
}
