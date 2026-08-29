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
	public int armouringSlotAvailable = 2;
	[BoxGroup(GroupID = "Stat"), Parsing("Occultor Slot")]
	public int occultorSlotAvailable = 2;
	[BoxGroup(GroupID = "Stat"), Parsing("SecondaryStat")]
	public SecondaryStat[] statBonuses;
	[Parsing("Is immortal")]
	public bool isImmortal;

	public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		description.Add(new() { ID = SecondaryStat.StatType.BaseHp, title = "HP", floatValue = maxHealth, stringValue = maxHealth.ToString() });
		description.Add(new() { ID = SecondaryStat.StatType.ArmourySlot, title = "ArmourySlot", floatValue = armouringSlotAvailable, stringValue = null });
		description.Add(new() { ID = SecondaryStat.StatType.OccultorSlot, title = "OccultorSlot", floatValue = occultorSlotAvailable, stringValue = null });
		foreach (SecondaryStat bonus in statBonuses)
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
	public int index;
	public bool isRepairing = false;
	public GameDatas.PlayerSave.Component frame;
	public GameDatas.PlayerSave.Component reactor;
	public GameDatas.PlayerSave.Component neuronalMembrane;
	public GameDatas.PlayerSave.Component brain;
	public GameDatas.PlayerSave.Component[] arms;
	public GameDatas.PlayerSave.Component[] auxiliar;
	public GameDatas.PlayerSave.Component[] chipsets;

	public int currentHp;

	public FrameEquipmentData FrameData => frame == null || string.IsNullOrEmpty(frame.dataID) ? null : GameAssets.current.equipments[frame.dataID] as FrameEquipmentData;
	public ReactorEquipmentData ReactorData => reactor == null || string.IsNullOrEmpty(reactor.dataID) ? null : GameAssets.current.equipments[reactor.dataID] as ReactorEquipmentData;
	public NeuronalMembraneEquipmentData NeuronalMembraneData => neuronalMembrane == null || string.IsNullOrEmpty(neuronalMembrane.dataID) ? null : GameAssets.current.equipments[neuronalMembrane.dataID] as NeuronalMembraneEquipmentData;
	public BrainEquipmentData BrainData => brain == null || string.IsNullOrEmpty(brain.dataID) ? null : GameAssets.current.equipments[brain.dataID] as BrainEquipmentData;

	public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
	{
		serializer.SerializeValue(ref name);
		serializer.SerializeValue(ref index);
		serializer.SerializeValue(ref isRepairing);
		serializer.SerializeValue(ref frame);
		serializer.SerializeValue(ref reactor);
		serializer.SerializeValue(ref neuronalMembrane);
		serializer.SerializeValue(ref brain);
		serializer.SerializeValue(ref arms);
		serializer.SerializeValue(ref auxiliar);
		serializer.SerializeValue(ref chipsets);
		serializer.SerializeValue(ref currentHp);
	}

	public bool CanAddToSquad ()
	{
		bool hasCapacity = GameDatas.current.currentPlayerSave.squadUnitsIndex.Count < GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxUnitAmount();
		return hasCapacity /*&& IsUnitValid()*/;
	}

	public bool IsUnitValid ()
	{
		if (FrameData == null || frame.isDamaged
			|| ReactorData == null || reactor.isDamaged
			|| NeuronalMembraneData == null || neuronalMembrane.isDamaged
			|| BrainData == null || brain.isDamaged
			|| isRepairing)
			return false;

		int remainingEnergy = ReactorData.energyProduced;
		remainingEnergy -= GetTotalEnergyUsed();

		if (remainingEnergy < 0)
			return false;

		return true;
	}

	public bool IsDamaged ()
	{
		foreach (GameDatas.PlayerSave.Component ep in GetAllEquipments())
			if (ep.isDamaged)
				return true;
		
		return false;
	}

	public int GetTotalEnergyUsed ()
	{
		int totalEnergyUsed = 0;

		if (FrameData == null || ReactorData == null || BrainData == null || NeuronalMembraneData == null)
			return totalEnergyUsed;

		totalEnergyUsed += FrameData.energyCost;
		totalEnergyUsed += BrainData.energyCost;
		totalEnergyUsed += NeuronalMembraneData.energyCost;
		foreach (GameDatas.PlayerSave.Component equipment in arms)
			totalEnergyUsed += GameAssets.current.equipments[equipment.dataID].energyCost;
		foreach (GameDatas.PlayerSave.Component equipment in auxiliar)
			totalEnergyUsed += GameAssets.current.equipments[equipment.dataID].energyCost;
		foreach (GameDatas.PlayerSave.Component equipment in chipsets)
			totalEnergyUsed += GameAssets.current.equipments[equipment.dataID].energyCost;

		return totalEnergyUsed;
	}

	public float GetStatBonusFromAll ( EntityEquipmentData.SecondaryStat.StatType _stat )
	{
		return GetStatBonusFrom(_stat, true, true, true, true);
	}

	public float GetStatBonusFrom ( EntityEquipmentData.SecondaryStat.StatType _stat, bool _frame = false/*, bool _brain = false*/, bool _arms = false, bool _auxiliar = false, bool _chipsets = false )
	{
		float totalBonus = 0;
		if (_frame && FrameData != null)
		{
			foreach (EntityEquipmentData.SecondaryStat statBonus in FrameData.statBonuses)
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
			foreach (GameDatas.PlayerSave.Component container in auxiliar)
			{
				if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
				{
					foreach (EntityEquipmentData.SecondaryStat statBonus in occultor.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
				else if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is ArmorEquipmentData armor)
				{
					foreach (EntityEquipmentData.SecondaryStat statBonus in armor.statBonuses)
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

	public List<AEntityPassiveEffect.PassiveEffectContainer> GetPassiveEffects ( EntityActionEnumID _actionID )
	{
		List<AEntityPassiveEffect.PassiveEffectContainer> passiveEffects = new();
		passiveEffects.AddRange(FrameData.passiveEffects);
		passiveEffects.AddRange(ReactorData.passiveEffects);
		passiveEffects.AddRange(NeuronalMembraneData.passiveEffects);
		passiveEffects.AddRange(BrainData.passiveEffects);
		if (_actionID != EntityActionEnumID.Unknowned && GameAssets.current.game.entityActionsData.ContainsKey(_actionID))
			passiveEffects.AddRange(GameAssets.current.game.entityActionsData[_actionID].passiveEffects);

		foreach (GameDatas.PlayerSave.Component container in arms)
		{
			if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment && equipment.knownedActions.Contains(_actionID))
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}
		foreach (GameDatas.PlayerSave.Component container in auxiliar)
		{
			if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment && equipment.knownedActions.Contains(_actionID))
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}
		foreach (GameDatas.PlayerSave.Component container in chipsets)
		{
			if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment)
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}


		return passiveEffects;
	}

	public List<GameDatas.PlayerSave.Component> GetAllEquipments ()
	{
		List<GameDatas.PlayerSave.Component> eqs = GetAllMainEquipments();
		eqs.AddRange(GetAllSubEquipments());
		return eqs;
	}

	public List<GameDatas.PlayerSave.Component> GetAllMainEquipments ()
	{
		List<GameDatas.PlayerSave.Component> equipments = new();

		if (frame != null)
			equipments.Add(frame);
		if (reactor != null)
			equipments.Add(reactor);
		if (neuronalMembrane != null)
			equipments.Add(neuronalMembrane);
		if (brain != null)
			equipments.Add(brain);

		return equipments;
	}

	public List<GameDatas.PlayerSave.Component> GetAllSubEquipments ()
	{
		List<GameDatas.PlayerSave.Component> equipments = new();

		if (arms != null)
		{
			foreach (var arm in arms)
				if (arm != null && !string.IsNullOrEmpty(arm.ID))
					equipments.Add(arm);
		}

		if (auxiliar != null)
		{
			foreach (var aux in auxiliar)
				if (aux != null && !string.IsNullOrEmpty(aux.ID))
					equipments.Add(aux);
		}

		if (chipsets != null)
		{
			foreach (var chipset in chipsets)
				if (chipset != null && !string.IsNullOrEmpty(chipset.ID))
					equipments.Add(chipset);
		}

		return equipments;
	}

	public EntityEquipmentData.EntityFaction GetDominentFaction (out float _percentage)
	{
		Dictionary<EntityEquipmentData.EntityFaction, int> count = new();
		foreach (GameDatas.PlayerSave.Component eq in GetAllEquipments())
		{
			if (!eq.TryGetData(out EntityEquipmentData data))
				continue;

			if (!count.ContainsKey(data.faction))
				count.Add(data.faction, 0);
			count[data.faction]++;
		}

		EntityEquipmentData.EntityFaction dominentFaction = EntityEquipmentData.EntityFaction.Noone;
		float biggestAmount = -1;
		float total = 0;
		foreach (EntityEquipmentData.EntityFaction faction in count.Keys)
		{
			if (count[faction] > biggestAmount)
			{
				dominentFaction = faction;
				biggestAmount = count[faction];
			}
			total += count[faction];
		}

		_percentage = biggestAmount / total;
		return dominentFaction;
	}

	public int GetMaxHealth ()
	{
		float bonus = 1 + GetStatBonusFrom(EntityEquipmentData.SecondaryStat.StatType.BaseHp);
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
		foreach (GameDatas.PlayerSave.Component container in auxiliar)
		{
			if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
			{
				foreach (EntityEquipmentData.SecondaryStat statBonus in occultor.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.SecondaryStat.StatType.VisualPerception && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.SecondaryStat.StatType.RadarPerception && !_isVisual)
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
		foreach (GameDatas.PlayerSave.Component container in auxiliar)
		{
			if (!container.isDamaged && GameAssets.current.equipments[container.dataID] is OccultorEquipmentData occultor)
			{
				if (_isVisual)
					result += occultor.visualCamo;
				else
					result += occultor.soundCamo;

				foreach (EntityEquipmentData.SecondaryStat statBonus in occultor.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.SecondaryStat.StatType.VisualCamo && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.SecondaryStat.StatType.RadarCamo && !_isVisual)
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

	public SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> GetStatsDesciptions ()
	{
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDictionary = new();
		if (FrameData != null)
		{
			foreach (EntityEquipmentData.StatDescription stat in FrameData.GetDesciption())
			{
				if (statsDictionary.ContainsKey(stat.ID))
					statsDictionary[stat.ID].Add(stat);
				else
					statsDictionary.Add(stat.ID, stat);
			}
		}
		if (ReactorData != null)
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
			foreach (GameDatas.PlayerSave.Component container in auxiliar)
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
			foreach (GameDatas.PlayerSave.Component container in arms)
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
			foreach (GameDatas.PlayerSave.Component container in chipsets)
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
