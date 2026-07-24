using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
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

	[BoxGroup(GroupID = "Stat"), Parsing("Energy Cost")]
	public int energyCost;
	[BoxGroup(GroupID = "Actions"), Parsing("Actions")]
	public EntityActionEnumID[] knownedActions;
	[BoxGroup(GroupID = "PassiveEffects"), Parsing("Passive Ability")]
	public AEntityPassiveEffect.PassiveEffectContainer[] passiveEffects;
	[BoxGroup(GroupID = "Status")]
	[Range(0f, 1f)] public float statusHitProbability = .5f;
	[Parsing("Credit Cost")]
	public ulong creditCost;

	public int recyclingDurationAmount = 1;
	public int reparingDurationAmount = 1;

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
		Commando,
		Dummy
	}

	[System.Serializable]
	public class StatDescription
	{
		public SecondaryStat.StatType ID;
		public string title;
		public string stringValue;
		public float floatValue;

		public void Add(StatDescription _statDescription )
		{
			switch (GameConfig.current.meta.formatPerStartTypeDictionary[ID])
			{
				case SecondaryStat.StatTypeFormat.Int:
					floatValue += _statDescription.floatValue;
					if(string.IsNullOrEmpty(stringValue))
						stringValue = null;
					else
						stringValue = floatValue.ToString();
					break;
				case SecondaryStat.StatTypeFormat.Percentage:
					floatValue += _statDescription.floatValue;
					stringValue = (floatValue*100) + " %";
					break;
				case SecondaryStat.StatTypeFormat.Cell:
					floatValue += _statDescription.floatValue;
					stringValue = floatValue + " C";
					break;
				case SecondaryStat.StatTypeFormat.String:
					if(!string.IsNullOrEmpty(_statDescription.stringValue))
						stringValue += ", " + _statDescription.stringValue;
					break;
			}
		}

		public void Remove(StatDescription _statDescription )
		{
			switch (GameConfig.current.meta.formatPerStartTypeDictionary[ID])
			{
				case SecondaryStat.StatTypeFormat.Int:
					floatValue -= _statDescription.floatValue;
					stringValue = null;
					break;
				case SecondaryStat.StatTypeFormat.Percentage:
					floatValue -= _statDescription.floatValue;
					stringValue = (floatValue * 100) + " %";
					break;
				case SecondaryStat.StatTypeFormat.Cell:
					floatValue -= _statDescription.floatValue;
					stringValue = floatValue + " C";
					break;
				case SecondaryStat.StatTypeFormat.String:
					if (!string.IsNullOrEmpty(_statDescription.stringValue))
						stringValue.Replace(_statDescription.stringValue, "");
					break;
			}
		}

		public StatDescription Compare (StatDescription _statDescription )
		{
			StatDescription result = new() { ID = ID, title = title, floatValue = floatValue, stringValue = stringValue };


			return result;
		}
	}

	public virtual StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = new();
		description.Add(new() { ID = SecondaryStat.StatType.EnergyCost, title = "Energy Cost", floatValue = energyCost, stringValue = energyCost.ToString() });

		if (passiveEffects != null && passiveEffects.Length > 0)
		{
			string allStatesInString = "";
			for (int i = 0; i < passiveEffects.Length; i++)
				allStatesInString += GameAssets.current.game.entityEffects[passiveEffects[i].enumID].displayName + (i + 1 < passiveEffects.Length ? ", " : "");
			description.Add(new() { ID = SecondaryStat.StatType.PassiveEffect, title = "Passive Effects", floatValue = 0, stringValue = allStatesInString });
		}
		if (knownedActions != null && knownedActions.Length > 0)
		{
			string allActionsInString = "";
			for (int i = 0; i < knownedActions.Length; i++)
			{
				if(knownedActions[i] != EntityActionEnumID.Unknowned && GameAssets.current.game.entityActionsData.ContainsKey(knownedActions[i]))
					allActionsInString += GameAssets.current.game.entityActionsData[knownedActions[i]].displayName + (i + 1 < knownedActions.Length ? ", " : "");
			}
			description.Add(new() { ID = SecondaryStat.StatType.Action, title = "Actions", floatValue = 0, stringValue = allActionsInString });
		}

		return description.ToArray();
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

	public bool TryGetEquipmentType ( out EquipmentType _type )
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
	public class SecondaryStat
	{
		public enum StatType
		{
			VisualCamo,
			RadarCamo, 
			ThermalCamo,
			BaseHp,
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

			EnergyCost,
			EnergyProduced,
			BaseDamage,
			VisionRange,
			VisionType,
			Action,
			PassiveEffect,
			States,
			ChipsetSlot,
			EquipmentSlot,
			ArmourySlot,
			OccultorSlot

		}

		public StatType type;
		public float value;

		public enum StatTypeFormat { Int, Percentage, Cell, String }

		public StatDescription GetDescription ()
		{
			string stringFormatedValue = null;
			switch (GameConfig.current.meta.formatPerStartTypeDictionary[type])
			{
				case StatTypeFormat.Int:
					stringFormatedValue = value.ToString();
					break;
				case StatTypeFormat.Percentage:
					stringFormatedValue = value * 100f + " %";
					break;
				case StatTypeFormat.Cell:
					stringFormatedValue = value + " C";
					break;
			}

			return new() {ID = type, title = type.ToString(), floatValue = value, stringValue = stringFormatedValue };
		}

	}

	[System.Serializable, ShowOdinSerializedPropertiesInInspector]
	public class StatBonusBuff
	{
		public SecondaryStat statBonus;
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

	public override void OnParse ( ImportedData _data )
	{
		/*List<EntityActionEnumID> actions = new();
		if(_data.TryGetValue("Actions", out EntityActionEnumID[] values))
			actions.AddRange(values);*/
	}

#endif
}
