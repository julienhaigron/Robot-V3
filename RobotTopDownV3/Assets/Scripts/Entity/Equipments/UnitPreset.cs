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
    public NeuronalMembraneEquipmentData neuronalMembrane;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")]
    public EntityEquipmentData[] arms;
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
        m_totalEnergyCostRemaining -= neuronalMembrane.energyCost;
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
        newUnit.frame = new() { ID = frame.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = frame.name };
        newUnit.reactor = new() { ID = reactor.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = reactor.name };
        newUnit.neuronalMembrane = new() { ID = neuronalMembrane.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = neuronalMembrane.name };
        newUnit.brain = new() { ID = brain.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = brain.name };

        List<GameDatas.PlayerSave.Equipment> armsContainer = new();
        foreach (EntityEquipmentData arm in arms)
            armsContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name });
        newUnit.arms = armsContainer.ToArray();

        List<GameDatas.PlayerSave.Equipment> auxiliaryContainer = new();
        foreach (EntityEquipmentData arm in auxiliary)
            auxiliaryContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name });
        newUnit.auxiliar = auxiliaryContainer.ToArray();

        List<GameDatas.PlayerSave.Equipment> chipstetsContainer = new();
        foreach (ChipsetEquipmentData arm in chipsets)
            chipstetsContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name });
        newUnit.chipsets = chipstetsContainer.ToArray();

        return newUnit;
    }

    [Button]
    public void AddToUnits ()
	{
        GameDatas.current.currentPlayerSave.squadUnits.Add(GetSavedData());
    }
}
