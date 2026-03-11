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

    private Dictionary<EntityActionEnumID, string> m_componentLinkedToAction;
    public Dictionary<EntityActionEnumID, string> ComponentLinkedToAction => m_componentLinkedToAction;
    
    private Dictionary<EntityActionEnumID, List<EntityPassiveEffectEnumID>> m_knownedPassiveEffectsPerAction = new();
    public Dictionary<EntityActionEnumID, List<EntityPassiveEffectEnumID>> KnownedPassiveEffectsPerAction => m_knownedPassiveEffectsPerAction;
    private List<EntityPassiveEffectEnumID> m_allPassiveEffects = new();
    public List<EntityPassiveEffectEnumID> AllPassiveEffects => m_allPassiveEffects;

    private List<EntityState> m_knownedStates = new();
    public List<EntityState> KnownedStates => m_knownedStates;

    private EntityState m_state;
    public EntityState State => m_state;

    private List<EntityStatusEnumID> m_status = new();
    public List<EntityStatusEnumID> Status => m_status;
    private Dictionary<AEntityStatus, int> m_remainingDurationToActiveEffects = new();

    private int m_ownerID;
    public int OwnerID => m_ownerID;
    //public EntityFaction Faction => m_data.FrameData.faction;

    [BoxGroup("Fix Stats")]
    


    private EntityActionData m_lastActionPerformed;
    public EntityActionData LastActionPerformedData => m_lastActionPerformed == null ? GameConfig.current.game.defaultStartAction : m_lastActionPerformed;

    public int ID;
    //public int PlayerOwnerID;

    public enum EntityState 
    {
        Guarding,
        Patroling,
        Special //to add
    }
    public enum EntityFaction
	{
        Scout,
        Psy
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

        m_componentLinkedToAction = _data.GetActions();
        m_knownedActions = new(m_componentLinkedToAction.Keys);

        foreach (EntityActionEnumID actionID in m_knownedActions)
		{
            m_knownedPassiveEffectsPerAction.Add(actionID, _data.GetPassiveEffects(actionID));
        }

        m_knownedStates.AddRange(_data.BrainData.knownedStates);
    }

    private void OnRoundStart ()
	{
        onNewRoundBegin?.Invoke();

        foreach (EntityStatusEnumID status in m_status)
		{
            if (--m_remainingDurationToActiveEffects[GameAssets.current.game.entityStatus[status]] <= 0)
			{
                m_status.Remove(status);
                m_remainingDurationToActiveEffects.Remove(GameAssets.current.game.entityStatus[status]);
			}

            GameAssets.current.game.entityStatus[status].ApplyStatusEffect(this);
		}
	}

    public void StartPerformAction ( AEntityAction _action)
	{
        if(_action.Data.type != EntityActionData.ActionType.Rotation)
            m_lastActionPerformed = _action.Data;

        onStartPerformAction?.Invoke(_action);
    }
    
    public void EndPerformAction ( )
	{
        onEndPerformAction?.Invoke();
	}

    public bool IsAlliedTo (int _playerOwnerId)
	{
        return m_ownerID == _playerOwnerId;
        /*if (!GameManager.Instance.IsOnline && m_data.FrameData.faction == EntityFaction.Scout)
            return true;
        else if (GameManager.Instance.IsOnline && m_ownerID == _playerOwnerId)
            return true;
        else
            return false;*/
	}

    public void Select ()
	{
        onSelect?.Invoke();
    }

    public void Deselect ()
    {
        onDeselect?.Invoke();
    }

    public void SetVisibility(bool _isVisible )
	{
        m_ui.gameObject.SetActive(_isVisible);
        if (_isVisible)
            m_skin.Show();
        else
            m_skin.Hide();
    }

    public void AddStatus(EntityStatusEnumID _statusID )
	{
        m_status.Add(_statusID);
        m_remainingDurationToActiveEffects.Add(GameAssets.current.game.entityStatus[_statusID], GameAssets.current.game.entityStatus[_statusID].duration);
    }
    
    public void RemoveStatus ( EntityStatusEnumID _statusID )
	{
        m_status.Remove(_statusID);
        m_remainingDurationToActiveEffects.Remove(GameAssets.current.game.entityStatus[_statusID]);
    }

}
