using UnityEngine;

[CreateAssetMenu(fileName = "RecyclingStationStructureUpgrade", menuName = "ScriptableObject/Structure/RecyclingStationStructureUpgrade")]
public class RecyclerStructureUpgrade : StructureUpgrade
{
    public int[] maxRecyclingSlotAmount;
    public int[] recyclingTimeReduction;
    public float[] tradeBonusAmount;

	protected override Addon[] CreateAddons ()
	{
		return new Addon[]
		{
			new(LocalizationKey.structure_addon_recycler_slots, _level => maxRecyclingSlotAmount[_level]),
			new(LocalizationKey.structure_addon_recycler_time, _level => recyclingTimeReduction[_level]),
			new(LocalizationKey.structure_addon_recycler_resale, _level => tradeBonusAmount[_level], true),
		};
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
