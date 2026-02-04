using UnityEngine;
using Sirenix.OdinInspector;

public class EntityEquipmentData : ScriptableObject
{
    //public EquipmentType type;
    public Sprite icon;
    [BoxGroup(GroupID ="Stat")]
    public int energyCost;

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
		}

        public StatType type;
        public float value;
	}
}
