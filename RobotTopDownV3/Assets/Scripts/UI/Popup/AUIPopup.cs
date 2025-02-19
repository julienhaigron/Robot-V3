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
}
