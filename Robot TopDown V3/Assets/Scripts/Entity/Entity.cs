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

    [SerializeField] private EntityData m_data;
    public EntityData Data => m_data;

    private EntityState m_state;
    public EntityState State => m_state;

    public EntityFaction m_faction;
    public EntityFaction Faction => m_faction;

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

    public void Init ( EntityData _data, RobotAnchor.Spawn _spawn )
    {
        m_data = _data;
        Displacement.Init(_spawn);
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
