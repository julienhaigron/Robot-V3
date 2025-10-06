using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;


[CreateAssetMenu(fileName = "FrameData", menuName = "ScriptableObject/FrameData", order = 1)]
public class FrameEquipmentData : EntityEquipmentData
{
    public Entity.EntityFaction faction;

    public int prefabId = 0;

    [Title("Action")]
    public int actionTokenAmount = 8;
    public EntityActionType[] knownedActions;
    [Min(0)] public int visibilityRange = 8;

    [Title("AI")]
    public EntityCapacityAsset.EntityCapacityType[] capacities;
    public Entity.EntityState[] knownedStates;

    [Title("Structure")]
    public int maxHealth;
    public int armSlotAvailable = 2;
}

[System.Serializable]
public class EntitySavedData : INetworkSerializable
{
    public string name;
    public string frameID;
    public string brainID;
    public StringContainer[] armsIds;

    public FrameEquipmentData FrameData => GameAssets.current.equipments[frameID] as FrameEquipmentData;

    public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref frameID);
        serializer.SerializeValue(ref brainID);
        serializer.SerializeValue(ref armsIds);
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
