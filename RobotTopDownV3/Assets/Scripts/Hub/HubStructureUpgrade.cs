using UnityEngine;

[CreateAssetMenu(fileName = "HubStructureUpgrade", menuName = "ScriptableObject/Structure/HubStructureUpgrade")]
public class HubStructureUpgrade : IncrementalUpgrade
{
    public int[] maxHangarUnitAmountPerLevel;
    public int[] maxComponentCapacityPerLevel;
    public int[] maxSquadUnitAmountPerLevel;
    public int[] maxSquadEnergyAmountPerLevel;

}
