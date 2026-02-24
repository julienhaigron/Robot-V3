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
		//UIManager.Instance.OnPanelClosed(this);
	}

#if UNITY_EDITOR
	/*protected *//*override*//* void Reset ()
	{
		base.Reset();
		CreateSideContainer();
	}*/

	[FoldoutGroup("EditorBtns")]
	[Button("Open")]
	void OpenViaInspector ()
	{
		UIManager.Instance.OpenPanel(this);
	}
#endif
}
