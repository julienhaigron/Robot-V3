using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementMonoBehaviour : MonoBehaviour
{
	[FoldoutGroup("UICanvasParent")]
	[SerializeField] private UICanvasParent m_canvasParent;

	[FoldoutGroup("UICanvasParent")]
	[Button("GetUICanvasParent")]

	protected bool canvasEnabled { get; private set; }//=> CanvasParent.CanvasEnabled;//cause issue with the inspector/Odin
	protected bool activeInHierarchy { get; private set; }

	public void GetWindowParent ()
	{
		if (m_canvasParent == null)
			m_canvasParent = GetComponentInParent<UICanvasParent>();

		//if (m_canvasParent == null)
		//	Debug.LogError(gameObject.name + " n'a pas pu trouver son CanvasParent.");
	}
	public UICanvasParent CanvasParent
	{
		get
		{
			if (m_canvasParent == null)
				GetWindowParent();
			return m_canvasParent;
		}
		set
		{
			m_canvasParent = value;
		}
	}


#if UNITY_EDITOR
	protected virtual void Reset ()
	{
		GetWindowParent();
	}
#endif
	protected virtual void Awake ()
	{
		if (m_canvasParent == null)
		{
			GetWindowParent();
			if (!m_canvasParent)
				return;
		}
		m_canvasParent.onCanvasEnabled += OnCanvasParentEnabled;
		m_canvasParent.onCanvasDisabled += OnCanvasParentDisabled;
	}

	protected virtual void OnDestroy ()
	{
		if (m_canvasParent != null)
		{
			m_canvasParent.onCanvasEnabled -= OnCanvasParentEnabled;
			m_canvasParent.onCanvasDisabled -= OnCanvasParentDisabled;
		}
	}

	protected virtual void OnEnable ()
	{
		activeInHierarchy = true;
		OnCanvasParentEnabled();
	}

	protected virtual void OnDisable ()
	{
		OnCanvasParentDisabled();
		activeInHierarchy = false;
	}

	protected virtual void OnCanvasParentEnabled ()
	{
		canvasEnabled = true;
	}

	protected virtual void OnCanvasParentDisabled ()
	{
		canvasEnabled = false;
	}
}