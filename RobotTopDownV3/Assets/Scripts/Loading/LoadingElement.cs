using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LoadingState
{
	Unloaded,
	Loading,
	Loaded,
	Failed
}
public enum LoadingType
{
	Required,
	Optional
}

public class LoadingElement : MonoBehaviour
{
	public System.Action<float> onProgress;
	public System.Action onLoadingStarted;
	public System.Action<bool> onLoadingEnd;

	[SerializeField]
	private float m_priority = 10f;
	public float priority => m_priority;
	[SerializeField]
	private LoadingType m_loadingType;
	public LoadingType loadingType => m_loadingType;
	[SerializeField]
	private string m_loadingText = "";
	public string loadingText => m_loadingText;

	private string m_errorMsg = "";
	public string errorMsg => m_errorMsg;

	public LoadingState loadingState { get; protected set; } = LoadingState.Unloaded;
	public bool loadingProcessFinished => loadingState == LoadingState.Loaded || loadingState == LoadingState.Failed;

	public float progress
	{
		get;
		private set;
	}


	public void SetProgress ( float percent )
	{
		percent = Mathf.Clamp01(percent);
		switch (loadingState)
		{
			case LoadingState.Unloaded:
				Load();
				progress = percent;
				onProgress?.Invoke(percent);
				break;
			case LoadingState.Loading:
				progress = percent;
				onProgress?.Invoke(percent);
				break;
			case LoadingState.Failed:
			case LoadingState.Loaded:
				break;
			default:
				break;
		}
	}

	public void Load ()
	{
		progress = 0f;
		loadingState = LoadingState.Loading;
		onLoadingStarted?.Invoke();
		onProgress?.Invoke(0f);
	}

	public void EndLoading ( bool succeed, string errorMessage = "" )
	{
		if (!succeed)
		{
			m_errorMsg = errorMessage;
		}
		progress = 1f;
		loadingState = succeed ? LoadingState.Loaded : LoadingState.Failed;
		onLoadingEnd?.Invoke(succeed);
	}
}