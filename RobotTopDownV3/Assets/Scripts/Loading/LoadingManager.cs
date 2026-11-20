using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;

public interface ILoadingScreen
{
	void Show ( Action _onCovered );
	void SetProgress ( float _progress );
	void Hide ( Action _onHidden );
}

public class LoadingManager : SingletonPersistant<LoadingManager>
{
	public static Action onStartFadeOut;
	[SerializeField] private LoadingElement[] m_loadingElements;

	private List<LoadingElement> m_awaitingLoadingElementList = new();
	private Action m_onLoadFinished;
	private Action m_onSceneLoaded;
	private bool m_isLoading = false;
	public bool IsLoading => m_isLoading;
	private ILoadingScreen m_screen;
	private AsyncOperation m_loadOperation;
	private float m_reportedProgress = -1f;

	private bool m_didFinishInitialLoad = false;

	public override void Awake ()
	{
		base.Awake();
		SceneManager.sceneLoaded += OnSceneLoaded;
		LoadingElement.onAnyFinishedLoading += OnLoadingElementFinishedLoading;
	}

	private void Start ()
	{
		InitialLoad();
	}

	private void Update ()
	{
		if (m_screen == null || m_loadOperation == null)
			return;

		float progress = m_loadOperation.progress / .9f;
		if (progress == m_reportedProgress)
			return;

		m_reportedProgress = progress;
		m_screen.SetProgress(progress);
	}

	private void InitialLoad ()
	{
		m_didFinishInitialLoad = false;
		m_isLoading = true;
		UIManager.Instance.LoadingScreenCanvasGroup.alpha = 1f;

		foreach (LoadingElement loadingElement in m_loadingElements)
		{
			m_awaitingLoadingElementList.Add(loadingElement);
			loadingElement.Load();
		}
	}

	private void OnLoadingElementFinishedLoading ( LoadingElement _loadingElement )
	{
		m_awaitingLoadingElementList.Remove(_loadingElement);

		if (m_isLoading && m_awaitingLoadingElementList.Count == 0)
			EndLoad();
	}


	public void LoadScene ( string _sceneName, Action _onStartLoadFinished, Action _onSceneLoaded, Action _onLoadFinished, ILoadingScreen _screen = null )
	{
		if (m_isLoading)
			return;

		m_isLoading = true;
		m_onLoadFinished = _onLoadFinished;
		m_onSceneLoaded = _onSceneLoaded;
		m_screen = _screen;
		m_reportedProgress = -1f;

		if (m_screen != null)
		{
			m_screen.Show(() =>
			{
				_onStartLoadFinished?.Invoke();
				m_loadOperation = LoadSceneAsync(_sceneName);
			});
			return;
		}

		UIManager.Instance.LoadingScreenCanvasGroup.DOFade(1f, .3f).OnComplete(() =>
		{
			_onStartLoadFinished?.Invoke();
			LoadSceneAsync(_sceneName);
		});
	}

	private static AsyncOperation LoadSceneAsync ( string _sceneName )
	{
#if UNITY_EDITOR
		if (!Application.CanStreamedLevelBeLoaded(_sceneName))
		{
			foreach (string guid in UnityEditor.AssetDatabase.FindAssets(_sceneName + " t:Scene"))
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				if (System.IO.Path.GetFileNameWithoutExtension(path) == _sceneName)
					return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
			}
		}
#endif
		return SceneManager.LoadSceneAsync(_sceneName);
	}

	private void OnSceneLoaded ( Scene _scene, LoadSceneMode _mode )
	{
		if (m_onSceneLoaded != null)
		{
			Action previousAction = new(m_onSceneLoaded);
			m_onSceneLoaded -= previousAction;
			previousAction?.Invoke();
		}

		if (m_isLoading)
			EndLoad();
	}

	private void EndLoad ()
	{
		if (onStartFadeOut != null)
		{
			Action previousAction = new(onStartFadeOut);
			onStartFadeOut -= previousAction;
			previousAction?.Invoke();
		}

		m_isLoading = false;
		if (!m_didFinishInitialLoad)
		{
			m_didFinishInitialLoad = true;
			UIManager.Instance.OpenPanel<StartMenuPanel>();
		}

		if (m_screen != null)
		{
			ILoadingScreen screen = m_screen;
			m_screen = null;
			m_loadOperation = null;
			screen.SetProgress(1f);
			screen.Hide(InvokeLoadFinished);
			return;
		}

		UIManager.Instance.LoadingScreenCanvasGroup.DOFade(0f, .3f).OnComplete(InvokeLoadFinished);
	}

	private void InvokeLoadFinished ()
	{
		if (m_onLoadFinished == null)
			return;

		Action previousAction = new(m_onLoadFinished);
		m_onLoadFinished -= previousAction;
		previousAction?.Invoke();
	}
}
