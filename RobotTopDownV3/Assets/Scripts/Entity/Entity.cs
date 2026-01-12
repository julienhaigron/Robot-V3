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

    private EntityState m_state;
    public EntityState State => m_state;

    private List<AEntityEffect> m_effects = new();
    public List<AEntityEffect> Effects => m_effects;
    private Dictionary<AEntityEffect, int> m_remainingDurationToActiveEffects = new();

    public EntityFaction Faction => m_data.FrameData.faction;

    private EntityActionData m_lastActionPerformed;
    public EntityActionData LastActionPerformedData => m_lastActionPerformed == null ? GameConfig.current.game.defaultStartAction : m_lastActionPerformed;

    public int ID;
    public int PlayerOwnerID;

    public enum EntityState 
    {
        Guarding,
        Patroling,
        Special //to add
    }
    public enum EntityFaction
	{
        Ally,
        Enemy
	}

    public void Init ( EntitySavedData _data, EntityAnchor.Spawn _spawn, int _id, int _playerID )
    {
        ID = _id;
        PlayerOwnerID = _playerID;
        m_data = _data;
        Displacement.SetSpawn(_spawn);
        
        m_equipment.Init(_data);
        m_ui.Init(_data);
        m_ai.Init(_data);
        m_skin.Init(_data);
    }

    public void OnPhaseStart ()
	{
        foreach(AEntityEffect effect in m_effects)
		{
            if (--m_remainingDurationToActiveEffects[effect] <= 0)
			{
                m_effects.Remove(effect);
                m_remainingDurationToActiveEffects.Remove(effect);
			}

            effect.ApplyEffect(this);
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
        if (!GameManager.Instance.IsOnline && m_data.FrameData.faction == EntityFaction.Ally)
            return true;
        else if (GameManager.Instance.IsOnline && PlayerOwnerID == _playerOwnerId)
            return true;
        else
            return false;
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
        m_skinParent.SetActive(_isVisible);
    }

    public void AddEffect(AEntityEffect _effect )
	{
        m_effects.Add(_effect);
        m_remainingDurationToActiveEffects.Add(_effect, _effect.duration);
    }

}
