using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "UnitPreset", menuName = "ScriptableObject/UnitPreset", order = 1)]
public class UnitPreset : AParsableScriptableObject
{
    [Parsing("Name")]
    public string displayName;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()"), Parsing("Frame")]
    public FrameEquipmentData frame;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()"), Parsing("Reactor")]
    public ReactorEquipmentData reactor;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()"), Parsing("AI Module")]
    public BrainEquipmentData brain;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()"), Parsing("Neural Interface")]
    public NeuronalMembraneEquipmentData neuronalMembrane;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")] //parsing done manualy
    public EntityEquipmentData[] arms;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()")] //parsing done manualy
    public EntityEquipmentData[] auxiliary;
    [OnValueChanged("@RefreshTotalEnergyCostRemaining()"), Parsing("Chipset")]
    public ChipsetEquipmentData[] chipsets;

    public Sprite icon;

    public bool isInvocation = false;
    [ShowIf("@isInvocation")]
    public bool isTangible = true;

    [ReadOnly, SerializeField]
    private int m_totalEnergyCostRemaining;
    public int TotalEnergyCostRemaining => m_totalEnergyCostRemaining;

    [Button]
    private void RefreshTotalEnergyCostRemaining ()
	{
        if(reactor != null)
            m_totalEnergyCostRemaining = reactor.energyProduced;
        if(frame != null)
            m_totalEnergyCostRemaining -= frame.energyCost;
        if(brain != null)
            m_totalEnergyCostRemaining -= brain.energyCost;
        if(neuronalMembrane != null)
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
        newUnit.isRepairing = false;
        newUnit.frame = new() { ID = frame.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = frame.name };
        newUnit.reactor = new() { ID = reactor.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = reactor.name };
        newUnit.neuronalMembrane = new() { ID = neuronalMembrane.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = neuronalMembrane.name };
        newUnit.brain = new() { ID = brain.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = brain.name };

        List<GameDatas.PlayerSave.Component> armsContainer = new();
        foreach (EntityEquipmentData arm in arms)
            armsContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name });
        newUnit.arms = armsContainer.ToArray();

        List<GameDatas.PlayerSave.Component> auxiliaryContainer = new();
        foreach (EntityEquipmentData arm in auxiliary)
            auxiliaryContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name });
        newUnit.auxiliar = auxiliaryContainer.ToArray();

        List<GameDatas.PlayerSave.Component> chipstetsContainer = new();
        foreach (ChipsetEquipmentData arm in chipsets)
            chipstetsContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name });
        newUnit.chipsets = chipstetsContainer.ToArray();

        newUnit.currentHp = newUnit.GetMaxHealth();

        return newUnit;
    }

    [Button]
    public void AddToUnits (bool _addToSquadIfAble)
	{
        GameDatas.current.currentPlayerSave.AddNewUnit(GetSavedData(), _addToSquadIfAble);
    }

	protected override string GetSheetID ()
	{
        return "0";
	}

	public override void OnParse ( ImportedData _data )
	{
        List<EntityEquipmentData> newArms = new();
        if (_data.TryGetValue("Weapon", out EntityEquipmentData[] weapons))
            newArms.AddRange(weapons);
        if(_data.TryGetValue("Tool", out EntityEquipmentData[] tools))
            newArms.AddRange(tools);
        arms = newArms.ToArray();

        List<EntityEquipmentData> newAux = new();
        if (_data.TryGetValue("Armouring", out EntityEquipmentData[] armors))
            newAux.AddRange(armors);
        if (_data.TryGetValue("Occultor", out EntityEquipmentData[] occultors))
            newAux.AddRange(occultors);
        auxiliary = newAux.ToArray();
    }
}
