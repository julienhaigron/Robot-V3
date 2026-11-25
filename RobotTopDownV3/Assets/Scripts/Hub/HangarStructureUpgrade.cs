using UnityEngine;

[CreateAssetMenu(fileName = "HangarStructureUpgrade", menuName = "ScriptableObject/Structure/HangarStructureUpgrade")]
public class HangarStructureUpgrade : StructureUpgrade
{
    public int[] maxHangarUnitAmountPerLevel;
    public int[] maxComponentCapacityPerLevel;
    public int[] maxSquadUnitAmountPerLevel;
    public int[] maxSquadEnergyAmountPerLevel;

	protected override Addon[] CreateAddons ()
	{
		return new Addon[]
		{
			new(LocalizationKey.structure_addon_hangar_units, _level => maxHangarUnitAmountPerLevel[_level]),
			new(LocalizationKey.structure_addon_hangar_components, _level => maxComponentCapacityPerLevel[_level]),
			new(LocalizationKey.structure_addon_hangar_squad_units, _level => maxSquadUnitAmountPerLevel[_level]),
			new(LocalizationKey.structure_addon_hangar_squad_energy, _level => maxSquadEnergyAmountPerLevel[_level]),
		};
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
