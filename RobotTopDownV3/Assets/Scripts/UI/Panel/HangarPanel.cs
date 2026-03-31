using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HangarPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	//[SerializeField] private BaseButton m_openForgeBtn;
	[SerializeField] private BaseButton m_openInventoryBtn;
	[SerializeField] private BaseButton m_addNewEntityBtn;
	[SerializeField] private BaseButton m_upgradeHangarBtn;


	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_openInventoryBtn.onClick += OnClickOpenInventory;
		//m_openForgeBtn.onClick += OnClickOpenForge;
		m_addNewEntityBtn.onClick += OnClickCreateNewEntity;
		m_upgradeHangarBtn.onClick += OnClickOpenUpgradePopup;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloHubPanel>();
	}
	
	private void OnClickOpenInventory ()
	{
		UIManager.Instance.OpenPanel<InventoryPanel>();
	}
	
	/*private void OnClickOpenForge ()
	{
		UIManager.Instance.OpenPanel<ForgePanel>();
	}*/

	private void OnClickCreateNewEntity ()
	{
		FrameEquipmentData baseFrame = GameAssets.current.game.frames[0];
		HubManager.Instance.AddEntity(GameDatas.current.currentPlayerSave.AddNewUnit(baseFrame));
	}
	
	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<HangarUpgradePopup>();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		HubManager.Instance.ShowHangar();
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
		HubManager.Instance.HideHangar();
	}

}
