using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BrainData", menuName = "ScriptableObject/Equipment/BrainData", order = 1)]
public class BrainEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public int chipsetSlotAvailable = 2;
    [BoxGroup(GroupID = "Stat")]
    public float agility = .155f;
    [BoxGroup(GroupID = "Stat")]
    public float distanceAccuracy = .5f;
    [BoxGroup(GroupID = "Stat")]
    public float distanceEvasion = .25f;
    [BoxGroup(GroupID = "Stat")]
    public float meleeEvasion = .25f;

    [BoxGroup(GroupID = "AI")]
    public EntityCapacityAsset.EntityCapacityType[] capacities;
    [BoxGroup(GroupID = "AI")]
    public Entity.EntityState[] knownedStates;
}
