using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RepairStationStructureUpgrade", menuName = "ScriptableObject/Structure/RepairStationStructureUpgrade")]
public class RepairStationStructureUpgrade : StructureUpgrade
{
    public int[] maxRepairedComponentSlotAmountPerLevel;
    public float[] repairPriceBonusPerLevel;
    public int[] maxRerollPerDayPerLevel;
    public int[] maxLockperDayPerLevel;

	public override void Upgrade ( bool removeMoney = true )
	{
		base.Upgrade(removeMoney);

		int maxSlotAmount = GetCurrentMaxRepairedComponentSlotAmountPerLevel();

		for (int currentSlotInSaveAmount = GameDatas.current.currentPlayerSave.dayData.repairingComponents.Count - 1; currentSlotInSaveAmount < maxSlotAmount; currentSlotInSaveAmount++)
			GameDatas.current.currentPlayerSave.dayData.repairingComponents.Add(new());
	}

	public override float GetAddonValue ( int _level, int _addonID )
	{
		switch (_addonID)
		{
			case 0:
				return maxRepairedComponentSlotAmountPerLevel[_level];
			case 1:
				return repairPriceBonusPerLevel[_level];
			case 2:
				return maxRerollPerDayPerLevel[_level];
			case 3:
				return maxLockperDayPerLevel[_level];
		}

		return -1;
	}

	public int GetCurrentMaxRepairedComponentSlotAmountPerLevel ()
	{
		return maxRepairedComponentSlotAmountPerLevel[GetCurrentLevel()];
	}

	public float GetCurrentRepairPriceBonusPerLevel ()
	{
		return repairPriceBonusPerLevel[GetCurrentLevel()];
	}

	public int GetCurrentMaxRerollPerDayPerLevel ()
	{
		return maxRerollPerDayPerLevel[GetCurrentLevel()];
	}

	public int GetCurrentMaxLockperDayPerLevel ()
	{
		return maxLockperDayPerLevel[GetCurrentLevel()];
	}
}
