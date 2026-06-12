using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class Entity : MonoBehaviour
{
	public Action onSelect;
	public Action onDeselect;
	public Action<AEntityAction> onStartPerformAction;
	public Action onEndPerformAction;
	public Action onNewRoundBegin;
	public Action<EntityStatusEnumID> onStatusAdded;
	public Action<EntityStatusEnumID> onStatusRemoved;
	public Action<EntityEquipmentData.StatBonusBuff> onStatBonusAdded;
	public Action<EntityEquipmentData.StatBonusBuff> onStatBonusRemoved;

	[Title("Depedencies")]
	[SerializeField] private GameObject m_skinParent;
	public GameObject SkinParent => m_skinParent;

	[SerializeField] private EntityDisplacementPlugin m_displacement;
	public EntityDisplacementPlugin Displacement => m_displacement;

	[SerializeField] private EntityEquipmentPlugin m_equipment;
	public EntityEquipmentPlugin Equipment => m_equipment;

	[SerializeField] private EntityAIPlugin m_ai;
	public EntityAIPlugin AI => m_ai;
	[SerializeField] private EntitySkinPlugin m_skin;
	public EntitySkinPlugin Skin => m_skin;

	[SerializeField] private EntityUIPlugin m_ui;
	public EntityUIPlugin UI => m_ui;

	[SerializeField] private EntitySavedData m_data;

	public EntitySavedData Data => m_data;

	private List<EntityActionEnumID> m_knownedActions = new();
	public List<EntityActionEnumID> KnownedActions => m_knownedActions;

	private Dictionary<EntityActionEnumID, List<string>> m_componentLinkedToAction;
	public Dictionary<EntityActionEnumID, List<string>> ComponentLinkedToAction => m_componentLinkedToAction;

	private Dictionary<EntityActionEnumID, List<AEntityPassiveEffect.PassiveEffectContainer>> m_knownedPassiveEffectsPerAction = new();
	public Dictionary<EntityActionEnumID, List<AEntityPassiveEffect.PassiveEffectContainer>> KnownedPassiveEffectsPerAction => m_knownedPassiveEffectsPerAction;
	private List<AEntityPassiveEffect.PassiveEffectContainer> m_allPassiveEffects = new();
	public List<AEntityPassiveEffect.PassiveEffectContainer> AllPassiveEffects => m_allPassiveEffects;

	private List<EntityState> m_knownedStates = new();
	public List<EntityState> KnownedStates => m_knownedStates;

	private EntityState m_state;
	public EntityState State => m_state;

	private List<EntityStatusEnumID> m_status = new();
	public List<EntityStatusEnumID> Status => m_status;
	private Dictionary<AEntityStatus, int> m_remainingDurationToActiveStatuses = new();
	private List<EntityEquipmentData.StatBonusBuff> m_statBuffs = new();
	public List<EntityEquipmentData.StatBonusBuff> StatBuffs => m_statBuffs;
	private Dictionary<EntityEquipmentData.StatBonus.StatType, float> m_activeStatBonusBuffs = new();

	private int m_ownerID;
	public int OwnerID => m_ownerID;
	//public EntityFaction Faction => m_data.FrameData.faction;

	[BoxGroup("Fix Stats")]
	private EntityActionData m_lastActionPerformed;
	public EntityActionData LastActionPerformedData => m_lastActionPerformed == null ? GameConfig.current.game.defaultStartAction : m_lastActionPerformed;

	public int ID;
	public int PlayerOwnerID;

	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;
	private NeuronalMembraneEquipmentData.VisionTypes m_howIsUnitVisible;
	public NeuronalMembraneEquipmentData.VisionTypes HowIsUnitVisible => m_howIsUnitVisible;

	[Serializable]
	public enum EntityState
	{
		Guarding,
		Patroling,
		Fleeing //to add
	}

	private void Awake ()
	{
		TurnManager.onNewRoundStart += OnRoundStart;
	}

	private void OnDestroy ()
	{
		TurnManager.onNewRoundStart -= OnRoundStart;
	}

	public void Init ( EntitySavedData _data, EntityAnchor.Spawn _spawn, int _id, int _playerID )
	{
		ID = _id;
		m_ownerID = _playerID;
		m_data = _data;
		Displacement.SetSpawn(_spawn);

		m_equipment.Init(_data);
		m_ui.Init(_data);
		m_ai.Init(_data);
		m_skin.Init(_data);

		m_componentLinkedToAction = GetActions();
		m_knownedActions = new(m_componentLinkedToAction.Keys);

		foreach (EntityActionEnumID actionID in m_knownedActions)
		{
			m_knownedPassiveEffectsPerAction.Add(actionID, _data.GetPassiveEffects(actionID));
		}

		m_knownedStates.AddRange(_data.BrainData.knownedStates);
	}

	private Dictionary<EntityActionEnumID, List<string>> GetActions ()
	{
		Dictionary<EntityActionEnumID, List<string>> actionsPerComponents = new();

		foreach (EntityActionEnumID actionID in m_data.FrameData.knownedActions)
		{
			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.FrameData.name);
		}
		foreach (EntityActionEnumID actionID in m_data.ReactorData.knownedActions)
		{
			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.ReactorData.name);
		}
		foreach (EntityActionEnumID actionID in m_data.NeuronalMembraneData.knownedActions)
		{
			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.NeuronalMembraneData.name);
		}
		foreach (EntityActionEnumID actionID in m_data.BrainData.knownedActions)
		{
			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.BrainData.name);
		}

		foreach (KeyValuePair<string, Weapon> pair in m_equipment.Weapons)
		{
			foreach (EntityActionEnumID actionID in pair.Value.Data.knownedActions)
			{
				if (!actionsPerComponents.ContainsKey(actionID))
					actionsPerComponents.Add(actionID, new());

				actionsPerComponents[actionID].Add(pair.Key);
			}
		}

		foreach (KeyValuePair<string, Tool> pair in m_equipment.Tools)
		{
			foreach (EntityActionEnumID actionID in pair.Value.Data.knownedActions)
			{
				if (!actionsPerComponents.ContainsKey(actionID))
					actionsPerComponents.Add(actionID, new());

				actionsPerComponents[actionID].Add(pair.Key);
			}
		}

		foreach (GameDatas.PlayerSave.Equipment container in m_data.auxiliar)
		{
			if (GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment)
			{
				foreach (EntityActionEnumID actionID in equipment.knownedActions)
				{
					if (actionsPerComponents.ContainsKey(actionID))
						continue;
					actionsPerComponents[actionID].Add(equipment.name);
				}
			}
		}
		foreach (GameDatas.PlayerSave.Equipment container in m_data.chipsets)
		{
			if (GameAssets.current.equipments[container.dataID] is EntityEquipmentData equipment)
			{
				foreach (EntityActionEnumID actionID in equipment.knownedActions)
				{
					if (actionsPerComponents.ContainsKey(actionID))
						continue;
					actionsPerComponents[actionID].Add(equipment.name);
				}
			}
		}

		return actionsPerComponents;
	}

	public void InitVisualOnly ( EntitySavedData _data )
	{
		m_data = _data;
		m_equipment.Init(_data);
		m_skin.Init(_data);
	}

	private void OnRoundStart ()
	{
		onNewRoundBegin?.Invoke();

		foreach (EntityStatusEnumID status in m_status.ToArray())
		{
			if (m_remainingDurationToActiveStatuses.ContainsKey(GameAssets.current.game.entityStatus[status])
				&& m_remainingDurationToActiveStatuses[GameAssets.current.game.entityStatus[status]] <= 0)
			{
				RemoveStatus(status);
			}

			GameAssets.current.game.entityStatus[status].ApplyStatusEffect(m_remainingDurationToActiveStatuses[GameAssets.current.game.entityStatus[status]]--, this);
		}

		foreach (EntityEquipmentData.StatBonusBuff buff in m_statBuffs.ToArray())
		{
			buff.duration--;
			if (buff.duration <= 0)
				RemoveAdditionaryStatBonus(buff);
		}
	}

	public void StartPerformAction ( AEntityAction _action, EntityState _state )
	{
		if (_action.Data.type != EntityActionData.ActionType.Rotation)
			m_lastActionPerformed = _action.Data;

		m_state = _state;
		onStartPerformAction?.Invoke(_action);
	}

	public void EndPerformAction ()
	{
		onEndPerformAction?.Invoke();
	}

	public bool IsAlliedTo ( int _playerOwnerId )
	{
		return m_ownerID == _playerOwnerId;
	}

	public void Select ()
	{
		onSelect?.Invoke();
	}

	public void Deselect ()
	{
		onDeselect?.Invoke();
	}

	public void SetVisibility ( bool _isVisible, NeuronalMembraneEquipmentData.VisionTypes _visionType)
	{
		m_isVisible = _isVisible;
		m_howIsUnitVisible = _visionType;

		m_ui.gameObject.SetActive(_isVisible);
		if (_isVisible)
			m_skin.Show();
		else
			m_skin.Hide();
	}

	public void AddStatus ( EntityStatusEnumID _statusID )
	{
		m_status.Add(_statusID);
		if (m_remainingDurationToActiveStatuses.ContainsKey(GameAssets.current.game.entityStatus[_statusID]))
			m_remainingDurationToActiveStatuses[GameAssets.current.game.entityStatus[_statusID]] = GameAssets.current.game.entityStatus[_statusID].duration;
		else
			m_remainingDurationToActiveStatuses.Add(GameAssets.current.game.entityStatus[_statusID], GameAssets.current.game.entityStatus[_statusID].duration);

		onStatusAdded?.Invoke(_statusID);
	}

	public void RemoveStatus ( EntityStatusEnumID _statusID )
	{
		GameAssets.current.game.entityStatus[_statusID].OnRemoveStatusEffect(this);
		m_status.Remove(_statusID);
		m_remainingDurationToActiveStatuses.Remove(GameAssets.current.game.entityStatus[_statusID]);

		onStatusRemoved?.Invoke(_statusID);
	}

	public void AddAdditionaryStatBonus ( EntityEquipmentData.StatBonusBuff _statBuff )
	{
		m_statBuffs.Add(_statBuff);
		if (m_activeStatBonusBuffs.ContainsKey(_statBuff.statBonus.type))
			m_activeStatBonusBuffs[_statBuff.statBonus.type] += _statBuff.statBonus.value;
		else
			m_activeStatBonusBuffs.Add(_statBuff.statBonus.type, _statBuff.statBonus.value);

		onStatBonusAdded?.Invoke(_statBuff);
	}

	public void RemoveAdditionaryStatBonus ( EntityEquipmentData.StatBonusBuff _statBuff )
	{
		m_statBuffs.Remove(_statBuff);
		m_activeStatBonusBuffs[_statBuff.statBonus.type] -= _statBuff.statBonus.value;

		onStatBonusRemoved?.Invoke(_statBuff);
	}

	public float GetAdditionaryStatBonus( EntityEquipmentData.StatBonus.StatType _type, AEntityAction _relatedAction )
	{
		float bonus = 0f;

		if (m_activeStatBonusBuffs.ContainsKey(_type))
			bonus += m_activeStatBonusBuffs[_type];

		foreach(GameDatas.PlayerSave.Equipment eq in m_data.chipsets)
		{
			if(eq.TryGetData(out ChipsetEquipmentData _chipsedData))
			{
				foreach(ChipsetEquipmentData.ConditionalStatBonus conditionalStatBonus in _chipsedData.statBonuses)
				{
					if (conditionalStatBonus.bonus.type == _type && conditionalStatBonus.UseConditionPredicate(_relatedAction, this, null))
						bonus += conditionalStatBonus.bonus.value;
				}
			}
		}

		return bonus;
	}

}
