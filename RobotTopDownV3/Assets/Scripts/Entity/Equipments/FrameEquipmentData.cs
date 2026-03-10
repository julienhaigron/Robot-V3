using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;
using System.Linq;


[CreateAssetMenu(fileName = "FrameData", menuName = "ScriptableObject/Equipment/FrameData", order = 1)]
public class FrameEquipmentData : EntityEquipmentData
{
	public Entity.EntityFaction faction;

	public Entity prefab;

	[Title("Action")]
	[Min(0)] public int visibilityRange = 8;

	[BoxGroup(GroupID = "Stat")]
	public int maxHealth;
	[BoxGroup(GroupID = "Stat")]
	public int armSlotAvailable = 2;
	[BoxGroup(GroupID = "Stat")]
	public int auxiliarSlotAvailable = 2;
	[BoxGroup(GroupID = "Stat")]
	public StatBonus[] statBonuses;

	//public float hpBonus = .3f; //is this stat variable?
	/*public float evasion = 2;
    public float camo = 2;*/
}

[System.Serializable]
public class EntitySavedData : INetworkSerializable
{
	public string name;
	public string frameID;
	public string reactorID;
	public string brainID;
	public StringContainer[] armsIds;
	public StringContainer[] auxiliarIds;
	public StringContainer[] chipsetsIds;

	public FrameEquipmentData FrameData => GameAssets.current.equipments[frameID] as FrameEquipmentData;
	public ReactorEquipmentData ReactorData => GameAssets.current.equipments[reactorID] as ReactorEquipmentData;
	public BrainEquipmentData BrainData => GameAssets.current.equipments[brainID] as BrainEquipmentData;

	public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
	{
		serializer.SerializeValue(ref name);
		serializer.SerializeValue(ref frameID);
		serializer.SerializeValue(ref reactorID);
		serializer.SerializeValue(ref brainID);
		serializer.SerializeValue(ref armsIds);
		serializer.SerializeValue(ref auxiliarIds);
		serializer.SerializeValue(ref chipsetsIds);
	}

	public float GetStatBonusFromAll ( EntityEquipmentData.StatBonus.StatType _stat)
	{
		return GetStatBonusFrom(_stat, true, true, true, true);
	}

	public float GetStatBonusFrom ( EntityEquipmentData.StatBonus.StatType _stat, bool _frame = false/*, bool _brain = false*/, bool _arms = false, bool _auxiliar = false, bool _chipsets = false )
	{
		float totalBonus = 0;
		if (_frame && GameAssets.current.equipments[frameID] is FrameEquipmentData frame)
		{
			foreach (EntityEquipmentData.StatBonus statBonus in frame.statBonuses)
			{
				if (statBonus.type == _stat)
					totalBonus += statBonus.value;
			}
		}
		/*if (_frame && GameAssets.current.equipments[frameID] is BrainEquipmentData brain)
        {
            foreach (EntityEquipmentData.StatBonus statBonus in brain.statBonuses)
            {
                if (statBonus.type == _stat)
                    totalBonus += statBonus.value;
            }
        }*/
		if (_auxiliar)
		{
			foreach (StringContainer container in auxiliarIds)
			{
				if (GameAssets.current.equipments[container.value] is OccultorEquipmentData occultor)
				{
					foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
				else if (GameAssets.current.equipments[container.value] is ArmorEquipmentData armor)
				{
					foreach (EntityEquipmentData.StatBonus statBonus in armor.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
			}
		}
		if (_chipsets)
		{
			foreach (StringContainer container in chipsetsIds)
			{
				if (GameAssets.current.equipments[container.value] is ChipsetEquipmentData chipset)
				{
					foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
					{
						if (statBonus.type == _stat)
							totalBonus += statBonus.value;
					}
				}
			}
		}

		return totalBonus;
	}

	public Dictionary<EntityActionEnumID, string> GetActions ()
	{
		Dictionary<EntityActionEnumID, string> actionsPerComponents = new();
		
		foreach(EntityActionEnumID actionID in FrameData.knownedActions)
		{
			actionsPerComponents.Add(actionID, FrameData.name);
		}
		foreach (EntityActionEnumID actionID in ReactorData.knownedActions)
		{
			actionsPerComponents.Add(actionID, ReactorData.name);
		}
		foreach (EntityActionEnumID actionID in BrainData.knownedActions)
		{
			actionsPerComponents.Add(actionID, BrainData.name);
		}

		foreach (StringContainer container in armsIds)
		{
			if (GameAssets.current.equipments[container.value] is EntityEquipmentData equipment)
			{
				foreach (EntityActionEnumID actionID in equipment.knownedActions)
				{
					actionsPerComponents.Add(actionID, equipment.name);
				}
			}
		}
		foreach (StringContainer container in auxiliarIds)
		{
			if (GameAssets.current.equipments[container.value] is EntityEquipmentData equipment)
			{
				foreach (EntityActionEnumID actionID in equipment.knownedActions)
				{
					actionsPerComponents.Add(actionID, equipment.name);
				}
			}
		}
		foreach (StringContainer container in chipsetsIds)
		{
			if (GameAssets.current.equipments[container.value] is EntityEquipmentData equipment)
			{
				foreach (EntityActionEnumID actionID in equipment.knownedActions)
				{
					actionsPerComponents.Add(actionID, equipment.name);
				}
			}
		}

		return actionsPerComponents;
	}

	public List<EntityPassiveEffectEnumID> GetPassiveEffects( EntityActionEnumID _actionID )
	{
		List<EntityPassiveEffectEnumID> passiveEffects = new();
		passiveEffects.AddRange(FrameData.passiveEffects);
		passiveEffects.AddRange(ReactorData.passiveEffects);
		passiveEffects.AddRange(BrainData.passiveEffects);
		passiveEffects.AddRange(GameAssets.current.game.entityActionsData[_actionID].passiveEffects);

		foreach (StringContainer container in armsIds)
		{
			if (GameAssets.current.equipments[container.value] is EntityEquipmentData equipment && equipment.knownedActions.Contains(_actionID))
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}
		foreach (StringContainer container in auxiliarIds)
		{
			if (GameAssets.current.equipments[container.value] is EntityEquipmentData equipment && equipment.knownedActions.Contains(_actionID))
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}
		foreach (StringContainer container in chipsetsIds)
		{
			if (GameAssets.current.equipments[container.value] is EntityEquipmentData equipment)
			{
				passiveEffects.AddRange(equipment.passiveEffects);
			}
		}


		return passiveEffects;
	}

	public int GetMaxHealth ()
	{
		float bonus = 1 + GetStatBonusFrom(EntityEquipmentData.StatBonus.StatType.Hp);
		float maxHealth = FrameData.maxHealth;

		return Mathf.RoundToInt(maxHealth * bonus);
	}

	public float GetStaticPerceptionBonus ( bool _isVisual )
	{
		float result = 0;
		foreach (StringContainer container in auxiliarIds)
		{
			if (GameAssets.current.equipments[container.value] is OccultorEquipmentData occultor)
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
		foreach (StringContainer container in chipsetsIds)
		{
			if (GameAssets.current.equipments[container.value] is ChipsetEquipmentData chipset)
			{
				foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualPerception && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundPerception && !_isVisual)
						result += statBonus.value;
				}
			}
		}
		return result;
	}

	public float GetStaticStealthBonus ( bool _isVisual )
	{
		float result = 0;
		foreach (StringContainer container in auxiliarIds)
		{
			if (GameAssets.current.equipments[container.value] is OccultorEquipmentData occultor)
			{
				if (_isVisual)
					result += occultor.visualCamo;
				else
					result += occultor.soundCamo;

				foreach (EntityEquipmentData.StatBonus statBonus in occultor.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualCamo && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundCamo && !_isVisual)
						result += statBonus.value;
				}
			}
		}
		foreach (StringContainer container in chipsetsIds)
		{
			if (GameAssets.current.equipments[container.value] is ChipsetEquipmentData chipset)
			{
				foreach (EntityEquipmentData.StatBonus statBonus in chipset.statBonuses)
				{
					if (statBonus.type == EntityEquipmentData.StatBonus.StatType.VisualCamo && _isVisual)
						result += statBonus.value;
					else if (statBonus.type == EntityEquipmentData.StatBonus.StatType.SoundCamo && !_isVisual)
						result += statBonus.value;
				}
			}
		}
		return result;

	}
}

[System.Serializable]
public class StringContainer : INetworkSerializable
{
	public string value;

	public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
	{
		serializer.SerializeValue(ref value);
	}
}
