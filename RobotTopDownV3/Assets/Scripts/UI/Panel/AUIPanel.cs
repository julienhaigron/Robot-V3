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

	protected override SfxId GetDefaultOpenSfx ( UISoundConfig _config )
	{
		return _config.panelOpen;
	}

	protected override SfxId GetDefaultCloseSfx ( UISoundConfig _config )
	{
		return _config.panelClose;
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
