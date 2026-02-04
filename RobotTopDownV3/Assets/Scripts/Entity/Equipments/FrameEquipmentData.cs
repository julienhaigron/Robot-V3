using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;


[CreateAssetMenu(fileName = "FrameData", menuName = "ScriptableObject/Equipment/FrameData", order = 1)]
public class FrameEquipmentData : EntityEquipmentData
{
    public Entity.EntityFaction faction;

    public Entity prefab;

    [Title("Action")]
    public int actionTokenAmount = 8;
    public EntityActionEnumID[] knownedActions;
    [Min(0)] public int visibilityRange = 8;

    [Title("AI")]
    public EntityCapacityAsset.EntityCapacityType[] capacities;
    public Entity.EntityState[] knownedStates;

    [BoxGroup(GroupID = "Stat")]
    public int maxHealth;
    [BoxGroup(GroupID = "Stat")]
    public int armSlotAvailable = 2;
    [BoxGroup(GroupID = "Stat")]
    public int auxiliarSlotAvailable = 2;
    [BoxGroup(GroupID = "Stat")]
    public float hpBonus = .3f; //is this stat variable?
    /*public float evasion = 2;
    public float camo = 2;*/
}

[System.Serializable]
public class EntitySavedData : INetworkSerializable
{
    public string name;
    public string frameID;
    public string reactorID;
    public string brainID;
    public StringContainer[] armsIds;
    public StringContainer[] auxiliarIds;
    public StringContainer[] chipsetsIds;

    public FrameEquipmentData FrameData => GameAssets.current.equipments[frameID] as FrameEquipmentData;
    public ReactorEquipmentData ReactorData => GameAssets.current.equipments[reactorID] as ReactorEquipmentData;
    public BrainEquipmentData BrainData => GameAssets.current.equipments[brainID] as BrainEquipmentData;

    public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref frameID);
        serializer.SerializeValue(ref reactorID);
        serializer.SerializeValue(ref brainID);
        serializer.SerializeValue(ref armsIds);
        serializer.SerializeValue(ref auxiliarIds);
        serializer.SerializeValue(ref chipsetsIds);
    }

    public float GetStaticDamageBonus ()
	{
        //TODO
        return 1;
	}

    public float GetStaticPerceptionBonus (bool _isVisual)
    {
        float result = 0;
        foreach (StringContainer container in auxiliarIds)
        {
            if (GameAssets.current.equipments[container.value] is OccultorEquipmentData occultor)
            {
                foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
                {
                    if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualPerception && _isVisual)
                        result += statBonus.value;
                    else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundPerception && !_isVisual)
                        result += statBonus.value;
                }
            }
        }
        foreach (StringContainer container in chipsetsIds)
        {
            if (GameAssets.current.equipments[container.value] is ChipsetEquipmentData chipset)
            {
                foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
                {
                    if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualPerception && _isVisual)
                        result += statBonus.value;
                    else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundPerception && !_isVisual)
                        result += statBonus.value;
                }
            }
        }
        return result;
    }

    public float GetStaticStealthBonus (bool _isVisual)
	{
        float result = 0;
        foreach(StringContainer container in auxiliarIds)
		{
            if(GameAssets.current.equipments[container.value] is OccultorEquipmentData occultor )
			{
                if (_isVisual)
                    result += occultor.visualCamo;
                else
                    result += occultor.soundCamo;

                foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
                {
                    if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualCamo && _isVisual)
                        result += statBonus.value;
                    else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundCamo && !_isVisual)
                        result += statBonus.value;
                }
            }
        }
        foreach (StringContainer container in chipsetsIds)
        {
            if (GameAssets.current.equipments[container.value] is ChipsetEquipmentData chipset)
            {
                foreach(EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
				{
                    if(statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualCamo && _isVisual)
                        result += statBonus.value;
                    else if(statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundCamo && !_isVisual)
                        result += statBonus.value;
				}
            }
        }
        return result;

    }
}

[System.Serializable]
public class StringContainer : INetworkSerializable
{
    public string value;

    public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref value);
    }
}
