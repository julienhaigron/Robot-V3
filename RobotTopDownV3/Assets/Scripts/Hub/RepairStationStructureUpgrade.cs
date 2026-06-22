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

		List<GameDatas.PlayerSave.DayData.RepairingComponentData> previousList = GameDatas.current.currentPlayerSave.dayData.repairingEntities.ToList();
		GameDatas.current.currentPlayerSave.dayData.repairingEntities = new GameDatas.PlayerSave.DayData.RepairingComponentData[GetCurrentMaxRepairedComponentSlotAmountPerLevel()];
		for (int i = 0; i < previousList.Count; i++)
			GameDatas.current.currentPlayerSave.dayData.repairingEntities[i] = previousList[i];
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
