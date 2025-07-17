using UnityEngine;

public class EntityEquipmentData : ScriptableObject
{
    public string ID;
    public EquipmentType type;

    public enum EquipmentType { Chassis, Arm, Leg, Brain }
}
