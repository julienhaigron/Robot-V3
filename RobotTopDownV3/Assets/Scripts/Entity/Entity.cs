using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class Entity : MonoBehaviour
{
    public Action onSelect;
    public Action onDeselect;

    [Title("Depedencies")]
    [SerializeField] private EntityDisplacementPlugin m_displacement;
    public EntityDisplacementPlugin Displacement => m_displacement;

    [SerializeField] private EntityEquipmentPlugin m_equipment;
	public EntityEquipmentPlugin Equipment => m_equipment;

    [SerializeField] private EntityAIPlugin m_ai;
    public EntityAIPlugin AI => m_ai;
    
    [SerializeField] private EntityUIPlugin m_ui;
    public EntityUIPlugin UI => m_ui;

    [SerializeField] private EntityData m_data;

    public EntityData Data => m_data;

    private EntityState m_state;
    public EntityState State => m_state;
    public EntityFaction Faction => m_data.faction;

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

    public void Init ( EntityData _data, EntityAnchor.Spawn _spawn, int _id, int _playerID )
    {
        ID = _id;
        PlayerOwnerID = _playerID;
        m_data = _data;
        Displacement.SetSpawn(_spawn);
        
        m_equipment.Init();
        m_ui.Init();
        m_ai.Init();
    }

    public bool IsAlliedTo (int _playerOwnerId)
	{
        if (m_data.faction == EntityFaction.Ally && !GameManager.Instance.IsOnline)
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

}
