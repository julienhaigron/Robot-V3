using UnityEngine;

[CreateAssetMenu(fileName = "RepairStationStructureUpgrade", menuName = "ScriptableObject/Structure/RepairStationStructureUpgrade")]
public class RepairStationStructureUpgrade : StructureUpgrade
{
    public int[] maxRepairedComponentSlotAmountPerLevel;
    public float[] repairPriceBonusPerLevel;

	protected override Addon[] CreateAddons ()
	{
		return new Addon[]
		{
			new(LocalizationKey.structure_addon_repair_slots, _level => maxRepairedComponentSlotAmountPerLevel[_level]),
			new(LocalizationKey.structure_addon_repair_discount, _level => repairPriceBonusPerLevel[_level], true),
		};
	}

	public int GetCurrentMaxRepairedComponentSlotAmountPerLevel ()
	{
		return maxRepairedComponentSlotAmountPerLevel[GetCurrentLevel()];
	}

	public float GetCurrentRepairPriceBonusPerLevel ()
	{
		return repairPriceBonusPerLevel[GetCurrentLevel()];
	}
}
