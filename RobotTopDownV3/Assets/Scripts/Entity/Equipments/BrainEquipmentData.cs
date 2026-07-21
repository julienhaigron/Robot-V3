using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "BrainData", menuName = "ScriptableObject/Equipment/BrainData", order = 1)]
public class BrainEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat"), Parsing("Chipset Slot")]
    public int chipsetSlotAvailable = 2;
    [BoxGroup(GroupID = "Stat"), Parsing("Melee Accuracy")]
    public float agility = .155f;
    [BoxGroup(GroupID = "Stat"), Parsing("Shoot Accuracy")]
    public float distanceAccuracy = .5f;
    [BoxGroup(GroupID = "Stat"), Parsing("Shoot Dodge")]
    public float distanceEvasion = .25f;
    [BoxGroup(GroupID = "Stat"), Parsing("Melee Dodge")]
    public float meleeEvasion = .25f;

    [BoxGroup(GroupID = "AI")]
    public Entity.EntityState[] knownedStates;

    public override StatDescription[] GetDesciption ()
    {
        List<StatDescription> description = base.GetDesciption().ToList();
        description.Add(new() { ID = SecondaryStat.StatType.ChipsetSlot, title = "Chipset Slots", floatValue = chipsetSlotAvailable, stringValue = null });
        description.Add(new() { ID = SecondaryStat.StatType.MeleeAccuracy, title = "Agility", floatValue = agility, stringValue = (agility*100f)  + " %"});
        description.Add(new() { ID = SecondaryStat.StatType.MeleeEvasion, title = "Melee Evasion", floatValue = meleeEvasion, stringValue = (meleeEvasion * 100f) + " %"});
        description.Add(new() { ID = SecondaryStat.StatType.DistanceAccuracy, title = "Distance Accuracy", floatValue = distanceAccuracy, stringValue = (distanceAccuracy * 100f) + " %"});
        description.Add(new() { ID = SecondaryStat.StatType.DistanceEvasion, title = "Distance Evasion", floatValue = distanceEvasion, stringValue = (distanceEvasion * 100f) + " %"});
        string allStatesInString = "";
        for (int i = 0; i < knownedStates.Length; i++)
            allStatesInString += knownedStates[i].ToString() + (i+1 < knownedStates.Length ? ", " : "");
        description.Add(new() { ID = SecondaryStat.StatType.States, title = "Knowned States", floatValue = 0, stringValue = allStatesInString });

        return description.ToArray();
    }
}
