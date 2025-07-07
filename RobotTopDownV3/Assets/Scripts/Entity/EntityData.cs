using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;


[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObject/EntityData", order = 1)]
public class EntityData : ScriptableObject
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
}

public class EntitySavedData : INetworkSerializable
{
    public Entity.EntityFaction faction;
    public int prefabId = 0;
    public int actionTokenAmount = 8;
    public EntityActionType[] knownedActions; 
    public int visibilityRange = 8;
    public EntityCapacityAsset.EntityCapacityType[] capacities;
    public Entity.EntityState[] knownedStates;
    public int maxHealth;

    public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref faction);
        serializer.SerializeValue(ref prefabId);
        serializer.SerializeValue(ref actionTokenAmount);
        serializer.SerializeValue(ref knownedActions);
        serializer.SerializeValue(ref visibilityRange);
        serializer.SerializeValue(ref capacities);
        serializer.SerializeValue(ref knownedStates);
        serializer.SerializeValue(ref maxHealth);
    }
}
