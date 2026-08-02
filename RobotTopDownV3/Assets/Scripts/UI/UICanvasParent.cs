using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent,
DefaultExecutionOrder(-45)]
public class UICanvasParent : MonoBehaviour
{
	public Action onCanvasEnabled;
	public Action onCanvasDisabled;

	[SerializeField] protected Canvas m_canvas;
	public Canvas WindowCanvas => m_canvas;

	[SerializeField] protected GraphicRaycaster m_raycaster;
	public GraphicRaycaster Raycaster => m_raycaster;


	[FoldoutGroup("EditorBtns")]
	[ShowIf("@m_canvas == null || m_raycaster == null")]
	[Button("Add Required Components")]
	public virtual void CreateWindowElements ()
	{
		Canvas canvas = GetComponent<Canvas>();
		if (canvas == null)
		{
			canvas = gameObject.AddComponent<Canvas>();
		}
		m_canvas = canvas;
		m_canvas.additionalShaderChannels = (AdditionalCanvasShaderChannels)~0;

		GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
		if (raycaster == null)
			raycaster = gameObject.AddComponent<GraphicRaycaster>();
		m_raycaster = raycaster;
	}

	public bool CanvasEnabled => m_canvas.enabled;

	public virtual void SetCanvasEnable ( bool _enable )
	{
		if (m_canvas.enabled == _enable) return;

		m_canvas.enabled = _enable;

		if (_enable)
		{
			if (!gameObject.activeSelf)
				gameObject.SetActive(true);

			OnEnableCanvas();
		}
		else
		{
			OnDisableCanvas();
		}
	}

	protected virtual void OnEnableCanvas ()
	{
		onCanvasEnabled?.Invoke();
	}

	protected virtual void OnDisableCanvas ()
	{
		onCanvasDisabled?.Invoke();
	}
}