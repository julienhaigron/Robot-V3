using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AUIPopup : AUIWindow
{
	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
		UIManager.Instance.OnPopupClosed(this);
	}

	protected override SfxId GetDefaultOpenSfx ( UISoundConfig _config )
	{
		return _config.popupOpen;
	}

	protected override SfxId GetDefaultCloseSfx ( UISoundConfig _config )
	{
		return _config.popupClose;
	}
}
