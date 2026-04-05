using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class HangarPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private BaseButton m_openInventoryBtn;
	[SerializeField] private BaseButton m_addNewEntityBtn;
	[SerializeField] private BaseButton m_upgradeHangarBtn;

	[SerializeField] private TextMeshProUGUI m_maxUnitInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxEnergyCostInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxUnitInHangarTMP;

	HangarStructureUpgrade HangarUpgrade => GameAssets.current.game.structureUpgrades[StructureUpgradePopup.StructureType.Hangar] as HangarStructureUpgrade;

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_openInventoryBtn.onClick += OnClickOpenInventory;
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

	private void OnClickCreateNewEntity ()
	{
		FrameEquipmentData baseFrame = GameAssets.current.game.frames[0];
		HubManager.Instance.AddEntity(GameDatas.current.currentPlayerSave.AddNewUnit(baseFrame));
	}
	
	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Hangar);
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

	private void RefreshDisplay ()
	{
		m_addNewEntityBtn.SetInteractability(GameDatas.current.currentPlayerSave.squadUnits.Count < HangarUpgrade.GetMaxHangarUnit());	
		m_maxUnitInSquadTMP.text = GameDatas.current.currentPlayerSave.squadUnits.Count +  "/" + HangarUpgrade.GetMaxHangarUnit();

		//m_maxEnergyCostInSquadTMP.text = 
	}
}
