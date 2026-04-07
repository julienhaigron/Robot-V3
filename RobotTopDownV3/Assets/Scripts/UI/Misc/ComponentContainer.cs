using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class ComponentContainer : MonoBehaviour, IDropHandler
{
    protected ComponentDisplay m_currentEquipment;
    public ComponentDisplay CurrentEquipment
    {
        get
        {
            return m_currentEquipment;
        }
        set
        {
            m_currentEquipment = value;
        }
    }
    protected Func<EntityEquipmentData, bool> m_predicate;

    protected ComponentContainer m_linkedContainer;
    public ComponentContainer LinkedContainer => m_linkedContainer;

    public virtual void Init ( ComponentContainer _container, EntitySavedData _unitData, EntityEquipmentData _equipmentData, Func<EntityEquipmentData, bool> _predicate, ComponentDisplay.DisplayMode _displayMode )
    {
        m_linkedContainer = _container;
        m_predicate = _predicate;
    }

    public bool IsValid ( EntityEquipmentData item )
    {
        if (item == null)
            return false;

        return m_predicate == null || m_predicate(item);
    }

    public void OnDrop ( PointerEventData eventData )
    {
        ComponentDisplay dropped = eventData.pointerDrag.GetComponent<ComponentDisplay>();
        if (dropped == null || !IsValid(dropped.ComponentData)) return;

        RemoveFromOrigin(dropped);

        RegisterInteraction(dropped);
    }

    public abstract void RegisterInteraction ( ComponentDisplay _component);

    public void RemoveFromOrigin ( ComponentDisplay item )
    {
        if (item.CurrentContainer != null)
            item.CurrentContainer.CurrentEquipment = null;
    }
}
