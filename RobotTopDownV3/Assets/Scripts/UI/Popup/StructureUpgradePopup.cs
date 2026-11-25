using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureUpgradePopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private TextMeshProUGUI m_levelTMP;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private GameObject m_priceGO;
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private BaseButton m_buyUpgradeBtn;
	[SerializeField] private StructureUpgradeAddonDisplay[] addonDisplays;

	private StructureType m_type;
	private StructureUpgrade Upgrade => GameAssets.current.game.structureUpgrades[m_type];

	public enum StructureType
	{
		Hangar,
		Recycler,
		PsyShop,
		CommandoShop,
		PaladinShop,
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
		StructureUpgrade upgrade = Upgrade;
		int level = upgrade.GetCurrentLevel();
		bool isMaxedOut = upgrade.IsMaxedOut();

		m_buyUpgradeBtn.SetInteractability(upgrade.CanUpgrade());

		m_titleTMP.text = upgrade.GetLocalizedName();
		m_priceGO.SetActive(!isMaxedOut);
		m_priceTMP.text = "-" + upgrade.GetCurrentPrice() + "r";
		m_levelTMP.text = isMaxedOut ? (level + 1).ToString() : (level + 1) + "<color=green><size=75%>+1</size></color>";

		string maxText = LocalizationManager.Instance.Get(LocalizationKey.upgrade_max);
		for (int i = 0; i < addonDisplays.Length; i++)
		{
			bool isUsed = i < upgrade.AddonCount;
			addonDisplays[i].gameObject.SetActive(isUsed);
			if (!isUsed)
				continue;

			if (isMaxedOut)
				addonDisplays[i].Init(upgrade.GetAddonTitle(i), upgrade.GetAddonValueText(level, i), null, maxText);
			else
				addonDisplays[i].Init(upgrade.GetAddonTitle(i), upgrade.GetAddonValueText(level, i), upgrade.GetAddonGainText(level, i), upgrade.GetAddonValueText(level + 1, i));
		}
	}

}
