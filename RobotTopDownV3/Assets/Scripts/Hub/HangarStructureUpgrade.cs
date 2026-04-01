using UnityEngine;

[CreateAssetMenu(fileName = "HangarStructureUpgrade", menuName = "ScriptableObject/Structure/HangarStructureUpgrade")]
public class HangarStructureUpgrade : StructureUpgrade
{
    public int[] maxHangarUnitAmountPerLevel;
    public int[] maxComponentCapacityPerLevel;
    public int[] maxSquadUnitAmountPerLevel;
    public int[] maxSquadEnergyAmountPerLevel;

}
