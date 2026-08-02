using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LoadingManager : SingletonPersistant<LoadingManager>
{
	[SerializeField] private LoadingElement[] m_loadingElements;

	private List<LoadingElement> m_awaitingLoadingElementList = new();
	private Action m_onLoadFinished;
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


	public void LoadScene ( string _sceneName, Action _onStartLoadFinished, Action _onLoadFinished )
	{
		if (m_isLoading)
			return;

		m_isLoading = true;
		m_onLoadFinished = _onLoadFinished;

		UIManager.Instance.LoadingScreenCanvasGroup.DOFade(1f, .3f).OnComplete(() =>
		{
			_onStartLoadFinished?.Invoke();
			SceneManager.LoadSceneAsync(_sceneName);
		});
	}

	private void OnSceneLoaded ( Scene _scene, LoadSceneMode _mode )
	{
		if (m_isLoading)
			EndLoad();
	}

	private void EndLoad ()
	{
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
