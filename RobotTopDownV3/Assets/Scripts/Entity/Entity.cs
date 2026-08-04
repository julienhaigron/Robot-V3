using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;

public class Entity : MonoBehaviour
{
	public Action onSelect;
	public Action onDeselect;
	public Action<AEntityAction> onStartPerformAction;
	public Action onEndPerformAction;
	public Action onEndTick;
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

	private List<EntityActionEnumID> m_knowedModActions = new();
	public List<EntityActionEnumID> KnownedModActions => m_knowedModActions;

	private Dictionary<EntityActionEnumID, List<string>> m_componentLinkedToAction = new();
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
	private Dictionary<EntityEquipmentData.SecondaryStat.StatType, float> m_activeStatBonusBuffs = new();

	private int m_ownerID;
	public int OwnerID => m_ownerID;
	//public EntityFaction Faction => m_data.FrameData.faction;

	[SerializeField, ReadOnly] private EntityActionData m_lastActionPerformed;
	public EntityActionData LastActionPerformedData => m_lastActionPerformed == null ? GameConfig.current.game.defaultStartAction : m_lastActionPerformed;
	[SerializeField, ReadOnly] private bool m_isPerforming = false;

	private HashSet<EntityActionEnumID> m_usedActionsThisGame = new();
	public HashSet<EntityActionEnumID> UsedActionsThisGame => m_usedActionsThisGame;

	public int ID;
	//public int PlayerOwnerID;

	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;
	private NeuronalMembraneEquipmentData.VisionTypes m_howIsUnitVisible;
	public NeuronalMembraneEquipmentData.VisionTypes HowIsUnitVisible => m_howIsUnitVisible;

	[Serializable]
	public enum EntityState
	{
		NoAIChange,
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
		m_skin.Init(_data);

		m_componentLinkedToAction = GetAllActions();
		m_knownedActions = GetActions().Keys.ToList();
		m_knowedModActions = GetModActions();

		m_ai.Init(_data);
		foreach (EntityActionEnumID actionID in m_knownedActions)
		{
			m_knownedPassiveEffectsPerAction.Add(actionID, _data.GetPassiveEffects(actionID));
		}

		foreach (GameDatas.PlayerSave.Equipment eq in _data.GetAllEquipments())
		{
			m_allPassiveEffects.AddRange(eq.GetData<EntityEquipmentData>().passiveEffects);
		}

		m_knownedStates.AddRange(GameAssets.current.game.states);
		m_usedActionsThisGame.Clear();
	}

	private Dictionary<EntityActionEnumID, List<string>> GetAllActions ()
	{
		Dictionary<EntityActionEnumID, List<string>> actionsPerComponents = new();

		foreach (EntityActionEnumID actionID in m_data.FrameData.knownedActions)
		{
			if (actionID == EntityActionEnumID.Unknowned)
				continue;

			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.FrameData.name);
		}
		foreach (EntityActionEnumID actionID in m_data.ReactorData.knownedActions)
		{
			if (actionID == EntityActionEnumID.Unknowned)
				continue;

			if (actionID != EntityActionEnumID.Unknowned && !actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.ReactorData.name);
		}
		foreach (EntityActionEnumID actionID in m_data.NeuronalMembraneData.knownedActions)
		{
			if (actionID == EntityActionEnumID.Unknowned)
				continue;

			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.NeuronalMembraneData.name);
		}
		foreach (EntityActionEnumID actionID in m_data.BrainData.knownedActions)
		{
			if (actionID == EntityActionEnumID.Unknowned)
				continue;

			if (!actionsPerComponents.ContainsKey(actionID))
				actionsPerComponents.Add(actionID, new());

			actionsPerComponents[actionID].Add(m_data.BrainData.name);
		}

		foreach (KeyValuePair<string, Weapon> pair in m_equipment.Weapons)
		{
			foreach (EntityActionEnumID actionID in pair.Value.Data.knownedActions)
			{
				if (actionID == EntityActionEnumID.Unknowned)
					continue;

				if (!actionsPerComponents.ContainsKey(actionID))
					actionsPerComponents.Add(actionID, new());

				actionsPerComponents[actionID].Add(pair.Key);
			}
		}

		foreach (KeyValuePair<string, Tool> pair in m_equipment.Tools)
		{
			foreach (EntityActionEnumID actionID in pair.Value.Data.knownedActions)
			{
				if (actionID == EntityActionEnumID.Unknowned)
					continue;

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
					if (actionID == EntityActionEnumID.Unknowned || actionsPerComponents.ContainsKey(actionID))
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
					if (actionID == EntityActionEnumID.Unknowned || actionsPerComponents.ContainsKey(actionID))
						continue;
					actionsPerComponents[actionID].Add(equipment.name);
				}
			}
		}

		return actionsPerComponents;
	}

	private Dictionary<EntityActionEnumID, List<string>> GetActions ()
	{
		Dictionary<EntityActionEnumID, List<string>> actionsPerComponents = GetAllActions();
		foreach (EntityActionEnumID actionID in actionsPerComponents.Keys.ToArray())
		{
			if (actionID == EntityActionEnumID.Unknowned || !GameAssets.current.game.entityActionsData.ContainsKey(actionID) || GameAssets.current.game.entityActionsData[actionID].isModAction)
				actionsPerComponents.Remove(actionID);
		}

		return actionsPerComponents;
	}

	private List<EntityActionEnumID> GetModActions ()
	{
		List<EntityActionEnumID> actions = GetAllActions().Keys.ToList();

		foreach (EntityActionEnumID actionID in actions.ToArray())
		{
			if (actionID == EntityActionEnumID.Unknowned || !GameAssets.current.game.entityActionsData.ContainsKey(actionID) || !GameAssets.current.game.entityActionsData[actionID].isModAction)
				actions.Remove(actionID);
		}

		return actions;
	}

	/*public List<EntityActionEnumID> GetReplacementActionFor ( EntityActionEnumID _actionID )
	{
		EntityActionData data = GameAssets.current.game.entityActionsData[_actionID];
		List<EntityActionEnumID> replacements = new();
		foreach (EntityActionEnumID actionID in m_knownedActions)
		{
			switch (GameAssets.current.game.entityActionsData[_actionID].type)
			{
				case EntityActionData.ActionType.DistanceAttack:
				case EntityActionData.ActionType.MeleeAttack:
					if (data.type == EntityActionData.ActionType.DistanceAttack || data.type == EntityActionData.ActionType.MeleeAttack)
						replacements.Add(actionID);
					break;
				case EntityActionData.ActionType.Movement:
					if (data.type == EntityActionData.ActionType.Movement)
						replacements.Add(actionID);
					break;
				case EntityActionData.ActionType.Rotation:
					if (data.type == EntityActionData.ActionType.Rotation)
						replacements.Add(actionID);
					break;
				case EntityActionData.ActionType.Special:
					if (data.type == EntityActionData.ActionType.Special)
						replacements.Add(actionID);
					break;
			}
		}

		return replacements;
	}*/

	public void InitHangarMode ( EntitySavedData _data )
	{
		m_data = _data;
		m_equipment.Init(_data);
		m_skin.Init(_data);
		m_ui.InitHangarMode(_data);
	}

	private void OnRoundStart ()
	{
		onNewRoundBegin?.Invoke();

		foreach (EntityStatusEnumID statusID in m_status.ToArray())
		{
			AEntityStatus status = GameAssets.current.game.entityStatus[statusID];
			if (m_remainingDurationToActiveStatuses.ContainsKey(status)
				&& m_remainingDurationToActiveStatuses[status] <= 0)
			{
				RemoveStatus(statusID);
			}
			else
				GameAssets.current.game.entityStatus[statusID].ApplyStatusEffect(m_remainingDurationToActiveStatuses.ContainsKey(status) ? m_remainingDurationToActiveStatuses[GameAssets.current.game.entityStatus[statusID]]-- : 0, this);
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
		if (!m_usedActionsThisGame.Contains(_action.enumID))
			m_usedActionsThisGame.Add(_action.enumID);

		if (_action.Data.type != EntityActionData.ActionType.Rotation)
			m_lastActionPerformed = _action.Data;

		m_isPerforming = true;
		m_state = _state;
		onStartPerformAction?.Invoke(_action);
	}

	public void EndTick ()
	{
		onEndTick?.Invoke();
	}

	public void EndPerformAction ()
	{
		m_isPerforming = true;
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

	public void SetVisibility ( bool _isVisible, NeuronalMembraneEquipmentData.VisionTypes _visionType )
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

	public float GetAdditionaryStatBonus ( EntityEquipmentData.SecondaryStat.StatType _type, AEntityAction _relatedAction )
	{
		float bonus = 0f;

		if (m_activeStatBonusBuffs.ContainsKey(_type))
			bonus += m_activeStatBonusBuffs[_type];

		foreach (GameDatas.PlayerSave.Equipment eq in m_data.chipsets)
		{
			if (eq.TryGetData(out ChipsetEquipmentData _chipsedData))
			{
				foreach (ChipsetEquipmentData.ConditionalStatBonus conditionalStatBonus in _chipsedData.statBonuses)
				{
					if (conditionalStatBonus.bonus.type == _type && Condition.UseConditionPredicate(_relatedAction, this, null, conditionalStatBonus.conditionType))
						bonus += conditionalStatBonus.bonus.value;
				}
			}
		}

		return bonus;
	}

}
