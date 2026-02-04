using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "UnitPreset", menuName = "ScriptableObject/UnitPreset", order = 1)]
public class UnitPreset : ScriptableObject
{
    public string displayName;
    public FrameEquipmentData frame;
    public ReactorEquipmentData reactor;
    public BrainEquipmentData brain;
    public WeaponEquipmentData[] arms;
    public EntityEquipmentData[] auxiliary;
    public ChipsetEquipmentData[] chipsets;

    public EntitySavedData GetSavedData ()
	{
        EntitySavedData newUnit = new();
        newUnit.name = displayName;
        newUnit.frameID = frame.name;
        newUnit.reactorID = reactor.name;
        newUnit.brainID = brain.name;

        List<StringContainer> armsContainer = new();
        foreach (WeaponEquipmentData arm in arms)
            armsContainer.Add(new() { value = arm.name });
        newUnit.armsIds = armsContainer.ToArray();

        List<StringContainer> auxiliaryContainer = new();
        foreach (EntityEquipmentData arm in auxiliary)
            auxiliaryContainer.Add(new() { value = arm.name });
        newUnit.auxiliarIds = auxiliaryContainer.ToArray();

        List<StringContainer> chipstetsContainer = new();
        foreach (ChipsetEquipmentData arm in chipsets)
            chipstetsContainer.Add(new() { value = arm.name });
        newUnit.chipsetsIds = chipstetsContainer.ToArray();

        return newUnit;
    }

    [Button]
    public void AddToUnits ()
	{
        GameDatas.current.player.units.Add(GetSavedData());
    }
}
