using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityEquipmentData : AParsableScriptableObject
{
    [Parsing("Faction")]
    public EntityFaction faction;
    //public EquipmentType type;
    public Sprite icon;
    [Parsing("Name")]
    public string displayName = "default";

    [BoxGroup(GroupID ="Stat")]
    public int energyCost;
    [BoxGroup(GroupID ="Actions")]
    public EntityActionEnumID[] knownedActions;
    [BoxGroup(GroupID ="PassiveEffects")]
    public AEntityPassiveEffect.PassiveEffectContainer[] passiveEffects;
    [BoxGroup(GroupID ="Status")]
    [Range(0f, 1f)] public float statusHitProbability = .5f;

    [System.Serializable]
    public enum EquipmentType { Frame, Brain, Reactor, Occultor, NeuronalMembrane, Weapon, Tool, Armor, Chipset }

	protected override string GetSheetID ()
	{
        return GameConfig.current.parsing.componentGUIDPerPage[GetEquipmentType()];
	}

	public enum EntityFaction
    {
        Noone,
        Scout,
        Psy,
        Paladin,
        Commando
    }

    public EquipmentType GetEquipmentType ()
	{
        if (this is FrameEquipmentData)
            return EquipmentType.Frame;
        else if (this is BrainEquipmentData)
            return EquipmentType.Brain;
        else if (this is ReactorEquipmentData)
            return EquipmentType.Reactor;
        else if (this is OccultorEquipmentData)
            return EquipmentType.Occultor;
        else if (this is NeuronalMembraneEquipmentData)
            return EquipmentType.NeuronalMembrane;
        else if (this is WeaponEquipmentData)
            return EquipmentType.Weapon;
        else if (this is ToolEquipmentData)
            return EquipmentType.Tool;
        else if (this is ArmorEquipmentData)
            return EquipmentType.Armor;
        else if (this is ChipsetEquipmentData)
            return EquipmentType.Chipset;

        return EquipmentType.Frame;
    }

    public bool TryGetEquipmentType(out EquipmentType _type )
	{
        _type = GetEquipmentType();
        return true;
	}

    public System.Tuple<CurrencyType, ulong> GetPrice ()
	{
        return new System.Tuple<CurrencyType, ulong>(CurrencyType.SoftCurrency, 10ul);
	}
    
    public System.Tuple<CurrencyType, ulong> GetSellingPrice ()
	{
        return new System.Tuple<CurrencyType, ulong>(CurrencyType.SoftCurrency, 5ul);
	}

    [System.Serializable, ShowOdinSerializedPropertiesInInspector]
    public class StatBonus
	{
        public enum StatType
		{
            VisualCamo,
            RadarCamo,
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

            FinalDamageBonus,

            DistanceEvasion,
            MeleeEvasion,
            DistanceAccuracy,
            MeleeAccuracy,
            ThermalCamo
        }

        public StatType type;
        public float value;
	}

    [System.Serializable, ShowOdinSerializedPropertiesInInspector]
    public class StatBonusBuff
	{
        public StatBonus statBonus;
        public int duration;
	}

#if UNITY_EDITOR
    [Button]
    private void AddToInventory ()
	{
        if (GameDatas.current.currentPlayerSave != null)
            GameDatas.current.currentPlayerSave.AddEquipmentToInventory(this);
        
        EditorUtility.SetDirty(GameDatas.current);
    }

    [Button]
    private void RefreshIcons ()
    {
        icon = GameAssets.current.ui.baseEquipmentSprite;
        EditorUtility.SetDirty(this);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

#endif
}
