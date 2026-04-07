using UnityEngine;

[CreateAssetMenu(fileName = "HangarStructureUpgrade", menuName = "ScriptableObject/Structure/HangarStructureUpgrade")]
public class HangarStructureUpgrade : StructureUpgrade
{
    public int[] maxHangarUnitAmountPerLevel;
    public int[] maxComponentCapacityPerLevel;
    public int[] maxSquadUnitAmountPerLevel;
    public int[] maxSquadEnergyAmountPerLevel;

	public override int GetAddonValue ( int _level, int _addonID )
	{
		switch (_addonID)
		{
			case 0:
				return maxHangarUnitAmountPerLevel[_level];
			case 1:
				return maxComponentCapacityPerLevel[_level];
			case 2:
				return maxSquadUnitAmountPerLevel[_level];
			case 3:
				return maxSquadEnergyAmountPerLevel[_level];
		}

		return -1;
	}

	public int GetCurrentMaxHangarUnit ()
	{
		return maxHangarUnitAmountPerLevel[GetCurrentLevel()];
	}

	public int GetCurrentMaxComponentCapacity ()
	{
		return maxComponentCapacityPerLevel[GetCurrentLevel()];
	}

	public int GetCurrentMaxUnitAmount ()
	{
		return maxSquadUnitAmountPerLevel[GetCurrentLevel()];
	}

	public int GetCurrentMaxSquadEnergyAmount ()
	{
		return maxSquadEnergyAmountPerLevel[GetCurrentLevel()];
	}
}
