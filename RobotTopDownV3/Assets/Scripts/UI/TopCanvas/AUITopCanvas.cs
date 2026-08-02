using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AUITopCanvas : UICanvasParent
{
	[SerializeField] protected bool m_visibleAtStart = true;
	[SerializeField] protected bool m_setCanvasEnableWithVisibility = false;
	protected bool m_visible;
	public bool Visible => m_visible;

	public virtual void OnLoad ()
	{

	}

	public virtual void SetVisible ( bool _visible, float? _duration = null, float _delay = 0f, Action _onFinishAction = null )
	{
		m_visible = _visible;

		gameObject.SetActive(_visible);
	}

	protected virtual void ResetToHiddenState ()
	{
		gameObject.SetActive(false);
	}

	protected virtual void DisableCanvas ()
	{
		SetCanvasEnable(false);
	}

	protected virtual void Awake ()
	{

	}

	public virtual void OnRegister ()
	{
		if (m_visibleAtStart)
		{
			gameObject.SetActive(true);
			SetVisible(true, 0f);
		}
		else
		{
			ResetToHiddenState();
		}
	}

	public new virtual Type GetType ()
	{
		return (base.GetType());
	}

	public override void CreateWindowElements ()
	{
		Canvas canvas = GetComponent<Canvas>();
		if (canvas == null)
		{
			canvas = gameObject.AddComponent<Canvas>();
			canvas.additionalShaderChannels = (AdditionalCanvasShaderChannels)~0;
		}
		m_canvas = canvas;
	}
}
