using UnityEngine;

public class EntityEquipmentData : ScriptableObject
{
    public string ID;
    public EquipmentType type;
    public Sprite icon;

    public enum EquipmentType { Chassis, Arm, Leg/*, Brain*/ }
}
