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

    [Parsing("Role")]
    public EnnemiAIRole aiRole = EnnemiAIRole.PatrolPath;

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
        //Entities are always built from an EntitySavedData, never from the preset, so the role has to be
        //copied here or BotEnnemiPlayer never sees it. Invocations go through this same path.
        newUnit.aiRole = aiRole;
        newUnit.frame = new() { ID = frame.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = frame.name, isDamaged = false };
        newUnit.reactor = new() { ID = reactor.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = reactor.name, isDamaged = false };
        newUnit.neuronalMembrane = new() { ID = neuronalMembrane.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = neuronalMembrane.name, isDamaged = false };
        newUnit.brain = new() { ID = brain.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = brain.name, isDamaged = false };

        List<GameDatas.PlayerSave.Component> armsContainer = new();
        foreach (EntityEquipmentData arm in arms)
            armsContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name, isDamaged = false });
        newUnit.arms = armsContainer.ToArray();

        List<GameDatas.PlayerSave.Component> auxiliaryContainer = new();
        foreach (EntityEquipmentData arm in auxiliary)
            auxiliaryContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name, isDamaged = false });
        newUnit.auxiliar = auxiliaryContainer.ToArray();

        List<GameDatas.PlayerSave.Component> chipstetsContainer = new();
        foreach (ChipsetEquipmentData arm in chipsets)
            chipstetsContainer.Add(new() { ID = arm.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = arm.name, isDamaged = false });
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

/// <summary>
/// Which plan BotEnnemiPlayer builds for the unit at the start of a round, and how reactive that plan is
/// (the Entity.EntityState it tags its actions with: NoAIChange = inert for the whole round, Patroling =
/// EntityAIPlugin.CheckAction may replace the action on any tick).
/// PatrolPath must stay the first value: enums serialize by index, so every UnitPreset asset authored before
/// this field existed reads back 0, and PatrolPath is exactly what those units used to do.
/// </summary>
[System.Serializable]
public enum EnnemiAIRole
{
    PatrolPath,
    Immobile,
    Aggressive,
    Recon,
    Support,
}
