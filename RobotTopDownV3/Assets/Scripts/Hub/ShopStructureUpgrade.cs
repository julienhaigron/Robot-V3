using UnityEngine;

[CreateAssetMenu(fileName = "ShopStructureUpgrade", menuName = "ScriptableObject/Structure/ShopStructureUpgrade")]
public class ShopStructureUpgrade : StructureUpgrade
{
    public int[] maxItemEachDayPerLevel;
    public float[] priceFactorPerLevel;
    public int[] maxRerollEachDayPerLevel;
    public int[] maxLockEachDayPerLevel;

	public override float GetAddonValue ( int _level, int _addonID )
	{
		switch (_addonID)
		{
			case 0:
				return maxItemEachDayPerLevel[_level];
			case 1:
				return priceFactorPerLevel[_level];
			case 2:
				return maxRerollEachDayPerLevel[_level];
			case 3:
				return maxLockEachDayPerLevel[_level];
		}

		return -1;
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
