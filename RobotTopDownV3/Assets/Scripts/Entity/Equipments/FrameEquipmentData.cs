using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;
using System.Linq;


[CreateAssetMenu(fileName = "FrameData", menuName = "ScriptableObject/Equipment/FrameData", order = 1)]
public class FrameEquipmentData : EntityEquipmentData
{
	public Entity prefab;

	[BoxGroup(GroupID = "Stat"), Parsing("HP")]
	public int maxHealth;
	[BoxGroup(GroupID = "Stat"), Parsing("Armouring Slot")]
	public int auxiliarSlotAvailable = 2;
	/*[BoxGroup(GroupID = "Stat"), Parsing("Occultor Slot")]
	public int occultorSlotAvailable = 2;*/
	[BoxGroup(GroupID = "Stat")]
	public StatBonus[] statBonuses;

	public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		description.Add(new() { ID = StatBonus.StatType.BaseHp, title = "HP", floatValue = maxHealth, stringValue = maxHealth.ToString() });
		description.Add(new() { ID = StatBonus.StatType.AuxiliarSlot, title = "AuxiliarSlot", floatValue = auxiliarSlotAvailable, stringValue = null });
		foreach (StatBonus bonus in statBonuses)
		{
			description.Add(bonus.GetDescription());
		}

		return description.ToArray();
	}
}

[System.Serializable]
public class EntitySavedData : INetworkSerializable
{
	public string name;
	public GameDatas.PlayerSave.Equipment frame;
	public GameDatas.PlayerSave.Equipment reactor;
	public GameDatas.PlayerSave.Equipment neuronalMembrane;
	public GameDatas.PlayerSave.Equipment brain;
	public GameDatas.PlayerSave.Equipment[] arms;
	public GameDatas.PlayerSave.Equipment[] auxiliar;
	public GameDatas.PlayerSave.Equipment[] chipsets;

	public int currentHp;

	public FrameEquipmentData FrameData => frame == null || string.IsNullOrEmpty(frame.dataID) ? null : GameAssets.current.equipments[frame.dataID] as FrameEquipmentData;
	public ReactorEquipmentData ReactorData => reactor == null || string.IsNullOrEmpty(reactor.dataID) ? null : GameAssets.current.equipments[reactor.dataID] as ReactorEquipmentData;
	public NeuronalMembraneEquipmentData NeuronalMembraneData => neuronalMembrane == null || string.IsNullOrEmpty(neuronalMembrane.dataID) ? null : GameAssets.current.equipments[neuronalMembrane.dataID] as NeuronalMembraneEquipmentData;
	public BrainEquipmentData BrainData => brain == null || string.IsNullOrEmpty(brain.dataID) ? null : GameAssets.current.equipments[brain.dataID] as BrainEquipmentData;

	public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
	{
		serializer.SerializeValue(ref name);
		serializer.SerializeValue(ref frame);
		serializer.SerializeValue(ref reactor);
		serializer.SerializeValue(ref neuronalMembrane);
		serializer.SerializeValue(ref brain);
		serializer.SerializeValue(ref arms);
		serializer.SerializeValue(ref auxiliar);
		serializer.SerializeValue(ref chipsets);
		serializer.SerializeValue(ref currentHp);
	}

	public bool IsUnitValid ()
	{
		if (string.IsNullOrEmpty(frame.dataID) || string.IsNullOrEmpty(reactor.dataID) || string.IsNullOrEmpty(brain.dataID) || string.IsNullOrEmpty(neuronalMembrane.dataID))
			return false;

		int remainingEnergy = ReactorData.energyProduced;
		remainingEnergy -= GetTotalEnergyUsed();

		if (remainingEnergy < 0)
			return false;

		return true;
	}

	public int GetTotalEnergyUsed ()
	{
		int totalEnergyUsed = 0;

		if (FrameData == null || ReactorData == null || BrainData == null || NeuronalMembraneData == null)
			return totalEnergyUsed;

		totalEnergyUsed += FrameData.energyCost;
		totalEnergyUsed += BrainData.energyCost;
		totalEnergyUsed += NeuronalMembraneData.energyCost;
		foreach (GameDatas.PlayerSave.Equipment equipment in arms)
			totalEnergyUsed += GameAssets.current.equipments[equipment.dataID].energyCost;
		foreach (GameDatas.PlayerSave.Equipment equipment in auxiliar)
			totalEnergyUsed += GameAssets.current.equipments[equipment.dataID].energyCost;
		foreach (GameDatas.PlayerSave.Equipment equipment in chipsets)
			totalEnergyUsed += GameAssets.current.equipments[equipment.dataID].energyCost;

		return totalEnergyUsed;
	}

	public float GetStatBonusFromAll ( EntityEquipmentData.StatBonus.StatType _stat)
	{
		return GetStatBonusFrom(_stat, true, true, true, true);
	}

	public float GetStatBonusFrom ( EntityEquipmentData.StatBonus.StatType _stat, bool _frame = false/*, bool _brain = false*/, bool _arms = false, bool _auxiliar = false, bool _chipsets = false )
	{
		float totalBonus = 0;
		if (_frame && FrameData != null)
		{
			foreach (EntityEquipmentData.StatBonus statBonus in FrameData.statBonuses)
			{
				if (statBonus.type == _stat)
					totalBonus += statBonus.value;
			}
		}
		/*if (_brain && BrainData != null)
		{
			foreach (EntityEquipmentData.StatBonus statBonus in BrainData.statBonuses)
			{
				if (statBonus.type == _stat)
					totalBonus += statBonus.value;
			}
		}*/
		if (_auxiliar && auxiliar != null)
		{
			foreach (GameDatas.PlayerSave.Equipment container in auxiliar)
			{
				if (GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
				{
					foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
				else if (GameAssets.current.equipments[container.dataID] is ArmorEquipmentData armor)
				{
					foreach (EntityEquipmentData.StatBonus statBonus in armor.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
			}
		}
		/*if (_chipsets && chipsets != null)
		{
			foreach (GameDatas.PlayerSave.Equipment container in chipsets)
			{
				if (GameAssets.current.equipments[container.dataID] is ChipsetEquipmentData chipset)
				{
					foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
			}
		}*/

		return totalBonus;
	}

	public List<AEntityPassiveEffect.PassiveEffectContainer> GetPassiveEffects( EntityActionEnumID _actionID )
	{
		List<AEntityPassiveEffect.PassiveEffectContainer> passiveEffects = new();
		passiveEffects.AddRange(FrameData.passiveEffects);
		passiveEffects.AddRange(ReactorData.passiveEffects);
		passiveEffects.AddRange(NeuronalMembraneData.passiveEffects);
		passiveEffects.AddRange(BrainData.passiveEffects);
		passiveEffects.AddRange(GameAssets.current.game.entityActionsData[_actionID].passiveEffects);

		foreach (GameDatas.PlayerSave.Equipment container in arms)
		{
			if (GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment && equipment.knownedActions.Contains(_actionID))
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}
		foreach (GameDatas.PlayerSave.Equipment container in auxiliar)
		{
			if (GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment && equipment.knownedActions.Contains(_actionID))
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}
		foreach (GameDatas.PlayerSave.Equipment container in chipsets)
		{
			if (GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment)
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}


		return passiveEffects;
	}

	public List<GameDatas.PlayerSave.Equipment> GetAllMainEquipments ()
	{
		List<GameDatas.PlayerSave.Equipment> equipments = new();

		if (frame != null)
			equipments.Add(frame);
		if (reactor != null)
			equipments.Add(frame);
		if (neuronalMembrane != null)
			equipments.Add(frame);
		if (brain != null)
			equipments.Add(frame);

		return equipments;
	}

	public List<GameDatas.PlayerSave.Equipment> GetAllSubEquipments ()
	{
		List<GameDatas.PlayerSave.Equipment> equipments = new();
		
		foreach (var arm in arms)
			if (arm != null && !string.IsNullOrEmpty(arm.ID))
				equipments.Add(arm);

		foreach (var aux in auxiliar)
			if (aux != null && !string.IsNullOrEmpty(aux.ID))
				equipments.Add(aux);

		foreach (var chipset in chipsets)
			if (chipset != null && !string.IsNullOrEmpty(chipset.ID))
				equipments.Add(chipset);

		return equipments;
	}

	public int GetMaxHealth ()
	{
		float bonus = 1 + GetStatBonusFrom(EntityEquipmentData.StatBonus.StatType.Hp);
		float maxHealth = FrameData == null ? 0 : FrameData.maxHealth;

		return Mathf.RoundToInt(maxHealth * bonus);
	}

	public float GetStaticPerceptionBonus ( bool _isVisual )
	{
		float result = 0;

		/*if(NeuronalMembraneData != null)
		{
			foreach (EntityEquipmentData.StatBonus statBonus in NeuronalMembraneData.visionTypes)
			{
				if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualPerception && _isVisual)
					result += statBonus.value;
				else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundPerception && !_isVisual)
					result += statBonus.value;
			}
		}*/
		foreach (GameDatas.PlayerSave.Equipment container in auxiliar)
		{
			if (GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
			{
				foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualPerception && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundPerception && !_isVisual)
						result += statBonus.value;
				}
			}
		}
		/*foreach (GameDatas.PlayerSave.Equipment container in chipsets)
		{
			if (GameAssets.current.equipments[container.dataID] is ChipsetEquipmentData chipset)
			{
				foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualPerception && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundPerception && !_isVisual)
						result += statBonus.value;
				}
			}
		}*/
		return result;
	}

	public float GetStaticStealthBonus ( bool _isVisual )
	{
		float result = 0;
		foreach (GameDatas.PlayerSave.Equipment container in auxiliar)
		{
			if (GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
			{
				if (_isVisual)
					result += occultor.visualCamo;
				else
					result += occultor.soundCamo;

				foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualCamo && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.RadarCamo && !_isVisual)
						result += statBonus.value;
				}
			}
		}
		/*foreach (GameDatas.PlayerSave.Equipment container in chipsets)
		{
			if (GameAssets.current.equipments[container.dataID] is ChipsetEquipmentData chipset)
			{
				foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualCamo && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundCamo && !_isVisual)
						result += statBonus.value;
				}
			}
		}*/
		return result;

	}

	public SerializableDictionary<EntityEquipmentData.StatBonus.StatType, EntityEquipmentData.StatDescription> GetStatsDesciptions ()
	{
		SerializableDictionary<EntityEquipmentData.StatBonus.StatType, EntityEquipmentData.StatDescription> statsDictionary = new();
		if(FrameData != null)
		{
			foreach(EntityEquipmentData.StatDescription stat in FrameData.GetDesciption())
			{
				if (statsDictionary.ContainsKey(stat.ID))
					statsDictionary[stat.ID].Add(stat);
				else
					statsDictionary.Add(stat.ID, stat);
			}
		}
		if(ReactorData != null)
		{
			foreach (EntityEquipmentData.StatDescription stat in ReactorData.GetDesciption())
			{
				if (statsDictionary.ContainsKey(stat.ID))
					statsDictionary[stat.ID].Add(stat);
				else
					statsDictionary.Add(stat.ID, stat);
			}
		}
		if (NeuronalMembraneData != null)
		{
			foreach (EntityEquipmentData.StatDescription stat in NeuronalMembraneData.GetDesciption())
			{
				if (statsDictionary.ContainsKey(stat.ID))
					statsDictionary[stat.ID].Add(stat);
				else
					statsDictionary.Add(stat.ID, stat);
			}
		}
		if (BrainData != null)
		{
			foreach (EntityEquipmentData.StatDescription stat in BrainData.GetDesciption())
			{
				if (statsDictionary.ContainsKey(stat.ID))
					statsDictionary[stat.ID].Add(stat);
				else
					statsDictionary.Add(stat.ID, stat);
			}
		}

		if (auxiliar != null)
		{
			foreach (GameDatas.PlayerSave.Equipment container in auxiliar)
			{
				if (GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
				{
					foreach (EntityEquipmentData.StatDescription stat in occultor.GetDesciption())
					{
						if (statsDictionary.ContainsKey(stat.ID))
							statsDictionary[stat.ID].Add(stat);
						else
							statsDictionary.Add(stat.ID, stat);
					}
				}
				else if (GameAssets.current.equipments[container.dataID] is ArmorEquipmentData armor)
				{
					foreach (EntityEquipmentData.StatDescription stat in armor.GetDesciption())
					{
						if (statsDictionary.ContainsKey(stat.ID))
							statsDictionary[stat.ID].Add(stat);
						else
							statsDictionary.Add(stat.ID, stat);
					}
				}
			}
		}
		if (arms != null)
		{
			foreach (GameDatas.PlayerSave.Equipment container in arms)
			{
				if (GameAssets.current.equipments[container.dataID] is WeaponEquipmentData weapon)
				{
					foreach (EntityEquipmentData.StatDescription stat in weapon.GetDesciption())
					{
						if (statsDictionary.ContainsKey(stat.ID))
							statsDictionary[stat.ID].Add(stat);
						else
							statsDictionary.Add(stat.ID, stat);
					}
				}
				else if (GameAssets.current.equipments[container.dataID] is ToolEquipmentData tool)
				{
					foreach (EntityEquipmentData.StatDescription stat in tool.GetDesciption())
					{
						if (statsDictionary.ContainsKey(stat.ID))
							statsDictionary[stat.ID].Add(stat);
						else
							statsDictionary.Add(stat.ID, stat);
					}
				}
			}
		}
		if (chipsets != null)
		{
			foreach (GameDatas.PlayerSave.Equipment container in chipsets)
			{
				if (GameAssets.current.equipments[container.dataID] is ChipsetEquipmentData chipset)
				{
					foreach (EntityEquipmentData.StatDescription stat in chipset.GetDesciption())
					{
						if (statsDictionary.ContainsKey(stat.ID))
							statsDictionary[stat.ID].Add(stat);
						else
							statsDictionary.Add(stat.ID, stat);
					}
				}
			}
		}

		return statsDictionary;
	}

}

/*[System.Serializable]
public class StringContainer : INetworkSerializable
{
	public string value;

	public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
	{
		serializer.SerializeValue(ref value);
	}
}*/
