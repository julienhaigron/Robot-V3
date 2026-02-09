using UnityEngine;
using Sirenix.OdinInspector;

public class EntityEquipmentData : ScriptableObject
{
    //public EquipmentType type;
    public Sprite icon;
    [BoxGroup(GroupID ="Stat")]
    public int energyCost;
    [BoxGroup(GroupID ="Actions")]
    public EntityActionEnumID[] knownedActions;

    //public enum EquipmentType { Frame, Arm, Leg, Brain, Occultor, Reactor, Chipset, Armor }

    [System.Serializable, ShowOdinSerializedPropertiesInInspector]
    public class StatBonus
	{
        public enum StatType
		{
            VisualCamo,
            SoundCamo,
            Camo,
            Hp,
            VisualPerception,
            SoundPerception,

            FireResitance,
            ElectricResitance,
            LaserResitance,
            MagneticResitance,
            PlasmaResitance,
            RadiationResitance,
            FlankResistance,
            StatusResistance,
            SlashResitance,
            PiercingResitance,
            BludgeoningResitance,

            StatusDurationReduction,
            StatusChance,
            StatusAppliedDurationRaise,
            FlankBonus,
            GeneralDamageBonus,
            GeneralDamageResistance,

            FireDamageBonus,
            ElectricDamageBonus,
            LaserDamageBonus,
            MagneticDamageBonus,
            PlasmaDamageBonus,
            RadiationDamageBonus,
            FlankDamageBonus,
            StatusDamageBonus,
            SlashDamageBonus,
            PiercingDamageBonus,
            BludgeoningDamageBonus,

            PhysicalDamageBonus,
            ElementalDamageBonus,

            PhysicalDamageResistance,
            ElementalDamageResistance,

            FinalDamageBonus
        }

        public StatType type;
        public float value;
	}
}
