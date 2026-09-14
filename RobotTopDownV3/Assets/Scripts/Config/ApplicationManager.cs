using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent()]
[RequireComponent(typeof(LoadingElement))]
public partial class ApplicationManager : Singleton<ApplicationManager>
{

	[SerializeField] LoadingElement m_loadingElement;

	[Header("Objects")]
	[SerializeField] private GameAssets m_gameAssets;
	[SerializeField] private GameConfig m_gameConfig;
	[SerializeField] private GameDatas m_gameDatas;

	private const string m_screenSetupPrefKey = "ScreenSetupVersion";
	private const int m_screenSetupVersion = 1;

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

		ApplyDefaultScreenSetup();

		StartCoroutine(LoadCoroutine());
	}

	private void ApplyDefaultScreenSetup ()
	{
#if !UNITY_EDITOR
		if (PlayerPrefs.GetInt(m_screenSetupPrefKey, 0) >= m_screenSetupVersion)
			return;

		PlayerPrefs.SetInt(m_screenSetupPrefKey, m_screenSetupVersion);
		PlayerPrefs.Save();

		Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
#endif
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
