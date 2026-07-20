using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RecyclingStationStructureUpgrade", menuName = "ScriptableObject/Structure/RecyclingStationStructureUpgrade")]
public class RecyclerStructureUpgrade : StructureUpgrade
{
    public int[] maxRecyclingSlotAmount;
    public int[] recyclingTimeReduction;
    public float[] tradeBonusAmount;


	public override void Upgrade ( bool removeMoney = true )
	{
		base.Upgrade(removeMoney);

		int maxSlotAmount = GetCurrentMaxRecyclingSlotAmount();

		for (int currentSlotInSaveAmount = GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Count - 1; currentSlotInSaveAmount < maxSlotAmount; currentSlotInSaveAmount++)
			GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Add(new());
	}


	public override float GetAddonValue ( int _level, int _addonID )
	{
		switch (_addonID)
		{
			case 0:
				return maxRecyclingSlotAmount[_level];
			case 1:
				return recyclingTimeReduction[_level];
			case 2:
				return tradeBonusAmount[_level];
			case 3:
				return -1;
		}

		return -1;
	}

	public int GetCurrentMaxRecyclingSlotAmount ()
	{
		return maxRecyclingSlotAmount[GetCurrentLevel()];
	}

	public int GetCurrentRecyclingTimeReduction ()
	{
		return recyclingTimeReduction[GetCurrentLevel()];
	}

	public float GetCurrentTradeBonusAmount ()
	{
		return tradeBonusAmount[GetCurrentLevel()];
	}
}
