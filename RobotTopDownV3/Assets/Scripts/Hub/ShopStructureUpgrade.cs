using UnityEngine;

[CreateAssetMenu(fileName = "ShopStructureUpgrade", menuName = "ScriptableObject/Structure/ShopStructureUpgrade")]
public class ShopStructureUpgrade : StructureUpgrade
{
    public int[] maxItemEachDayPerLevel;
    public float[] priceFactorPerLevel;
    public int[] maxRerollEachDayPerLevel;
    public int[] maxLockEachDayPerLevel;

	protected override Addon[] CreateAddons ()
	{
		return new Addon[]
		{
			new(LocalizationKey.structure_addon_shop_items, _level => maxItemEachDayPerLevel[_level]),
			new(LocalizationKey.structure_addon_shop_resale, _level => priceFactorPerLevel[_level], true),
			new(LocalizationKey.structure_addon_shop_rerolls, _level => maxRerollEachDayPerLevel[_level]),
			new(LocalizationKey.structure_addon_shop_freezes, _level => maxLockEachDayPerLevel[_level]),
		};
	}

	public int GetMaxItemAmount ()
	{
		return maxItemEachDayPerLevel[GetCurrentLevel()];
	}

	public float GetPriceFactor ()
	{
		return priceFactorPerLevel[GetCurrentLevel()];
	}

	public int GetMaxRerollAmount ()
	{
		return maxRerollEachDayPerLevel[GetCurrentLevel()];
	}

	public int GetMaxLockAmount ()
	{
		return maxLockEachDayPerLevel[GetCurrentLevel()];
	}
}
