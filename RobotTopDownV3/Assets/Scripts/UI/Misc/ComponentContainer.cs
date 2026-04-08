using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class ComponentContainer : MonoBehaviour, IDropHandler
{
    protected ComponentDisplay m_currentDisplay;
    public ComponentDisplay CurrentDisplay
    {
        get
        {
            return m_currentDisplay;
        }
        set
        {
            m_currentDisplay = value;
        }
    }
    protected Func<GameDatas.PlayerSave.Equipment, bool> m_predicate;

    protected ComponentContainer m_linkedContainer;
    public ComponentContainer LinkedContainer => m_linkedContainer;

    public virtual void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Equipment _componentSavedData, Func<GameDatas.PlayerSave.Equipment, bool> _predicate, ComponentDisplay.DisplayMode _displayMode )
    {
        m_linkedContainer = _container;
        m_predicate = _predicate;
    }

    public virtual bool IsValid ( GameDatas.PlayerSave.Equipment _savedData )
    {
        if (_savedData == null)
            return false;

        return m_predicate == null || m_predicate(_savedData);
    }

    public void OnDrop ( PointerEventData eventData )
    {
        ComponentDisplay dropped = eventData.pointerDrag.GetComponent<ComponentDisplay>();
        if (dropped == null || !IsValid(dropped.SavedData)) return;

        RemoveFromOrigin(dropped);

        RegisterInteraction(dropped);
    }

    public abstract void RegisterInteraction ( ComponentDisplay _component);

    public void RemoveFromOrigin ( ComponentDisplay item )
    {
        if (item.CurrentContainer != null)
            item.CurrentContainer.CurrentDisplay = null;
    }
}
