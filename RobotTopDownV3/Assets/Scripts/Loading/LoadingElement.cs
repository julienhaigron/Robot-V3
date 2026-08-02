using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingElement : MonoBehaviour
{
	public static System.Action<LoadingElement> onAnyFinishedLoading;

	public System.Action<float> onProgress;
	public System.Action onLoadingStarted;
	public System.Action<bool> onLoadingEnd;

	private bool m_didFinishedLoading = false;
	public bool DidFinishedLoading => m_didFinishedLoading;

	public void Load ()
	{
		m_didFinishedLoading = false;
		onLoadingStarted?.Invoke();
		onProgress?.Invoke(0f);
	}

	public void SetProgress(float _progress )
	{
		onProgress?.Invoke(_progress);
	}

	public void EndLoading ( bool succeed, string errorMessage = "" )
	{
		m_didFinishedLoading = true;
		onLoadingEnd?.Invoke(succeed);
		onAnyFinishedLoading?.Invoke(this);
	}
}