using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureUpgradePopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private BaseButton m_buyUpgradeBtn;
	[SerializeField] private StructureUpgradeAddonDisplay[] addonDisplays;

	private StructureType m_type;
	private StructureUpgrade Upgrade => GameAssets.current.game.structureUpgrades[m_type];

	public enum StructureType
	{
		Hangar,
		Recycler,
		Shop,
		RepairStation
	}

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		m_buyUpgradeBtn.onClick += OnClickOnBuyBtn;
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}

	private void OnClickClose ()
	{
		Close();
	}
	
	private void OnClickOnBuyBtn ()
	{
		if (!Upgrade.CanUpgrade())
			return;

		Upgrade.Upgrade(true);

		RefreshVisual();
	}

	public void Init ( StructureType _type )
	{
		m_type = _type;

		RefreshVisual();
	}

	private void RefreshVisual ()
	{
		m_buyUpgradeBtn.SetInteractability(Upgrade.CanUpgrade());

		m_titleTMP.text = Upgrade.displayName;
		for(int i = 0; i< addonDisplays.Length; i++)
		{
			if (Upgrade.addonDescriptions.Length <= i)
				continue;

			float bonus = Upgrade.GetAddonValue(Upgrade.GetCurrentLevel() + 1, i) - Upgrade.GetAddonValue(Upgrade.GetCurrentLevel(), i);
			string content = Upgrade.GetAddonDescription(i, bonus);
			addonDisplays[i].Init(content);
		}
	}

}
