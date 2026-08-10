using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LoadingManager : SingletonPersistant<LoadingManager>
{
	public static Action onStartFadeOut;
	[SerializeField] private LoadingElement[] m_loadingElements;

	private List<LoadingElement> m_awaitingLoadingElementList = new();
	private Action m_onLoadFinished;
	private Action m_onSceneLoaded;
	private Coroutine m_loadingCR;
	private bool m_isLoading = false;
	public bool IsLoading => m_isLoading;

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


	public void LoadScene ( string _sceneName, Action _onStartLoadFinished, Action _onSceneLoaded, Action _onLoadFinished )
	{
		if (m_isLoading)
			return;

		m_isLoading = true;
		m_onLoadFinished = _onLoadFinished;
		m_onSceneLoaded = _onSceneLoaded;

		UIManager.Instance.LoadingScreenCanvasGroup.DOFade(1f, .3f).OnComplete(() =>
		{
			_onStartLoadFinished?.Invoke();
			SceneManager.LoadSceneAsync(_sceneName);
		});
	}

	private void OnSceneLoaded ( Scene _scene, LoadSceneMode _mode )
	{
		m_onSceneLoaded?.Invoke();

		if (m_isLoading)
			EndLoad();
	}

	private void EndLoad ()
	{
		onStartFadeOut?.Invoke();

		m_isLoading = false; 
		if (!m_didFinishInitialLoad)
		{
			m_didFinishInitialLoad = true;
			UIManager.Instance.OpenPanel<StartMenuPanel>();
		}

		UIManager.Instance.LoadingScreenCanvasGroup.DOFade(0f, .3f).OnComplete(() =>
		{
			m_onLoadFinished?.Invoke();
		});
	}
}
