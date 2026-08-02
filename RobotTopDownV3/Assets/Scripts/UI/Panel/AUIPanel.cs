using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class AUIPanel : AUIWindow
{
	public static Action onPanelShowStarted;
	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
		onPanelShowStarted?.Invoke();
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
		SetCanvasEnable(false);
	}

#if UNITY_EDITOR

	[FoldoutGroup("EditorBtns")]
	[Button("Open")]
	void OpenViaInspector ()
	{
		UIManager.Instance.OpenPanel(this);
	}
#endif
}
