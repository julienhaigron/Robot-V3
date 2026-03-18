using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "UnitPreset", menuName = "ScriptableObject/UnitPreset", order = 1)]
public class UnitPreset : ScriptableObject
{
    public string displayName;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public FrameEquipmentData frame;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public ReactorEquipmentData reactor;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public BrainEquipmentData brain;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public WeaponEquipmentData[] arms;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public EntityEquipmentData[] auxiliary;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public ChipsetEquipmentData[] chipsets;

    public bool isInvocation = false;
    [ShowIf("@isInvocation")]
    public bool isTangible = true;

    [ReadOnly, SerializeField]
    private int m_totalEnergyCostRemaining;
    public int TotalEnergyCostRemaining => m_totalEnergyCostRemaining;

    [Button]
    private void RefreshTotalEnergyCostRemaining ()
	{
        m_totalEnergyCostRemaining = reactor.energyProduced;
        m_totalEnergyCostRemaining -= frame.energyCost;
        m_totalEnergyCostRemaining -= brain.energyCost;
        foreach (EntityEquipmentData equipment in arms)
            m_totalEnergyCostRemaining -= equipment.energyCost;
        foreach (EntityEquipmentData equipment in auxiliary)
            m_totalEnergyCostRemaining -= equipment.energyCost;
        foreach (EntityEquipmentData equipment in chipsets)
            m_totalEnergyCostRemaining -= equipment.energyCost;
    }

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
