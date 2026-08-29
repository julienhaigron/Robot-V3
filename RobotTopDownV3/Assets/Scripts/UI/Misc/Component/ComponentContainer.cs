using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class ComponentContainer : MonoBehaviour, IDropHandler
{
    public Action<ComponentContainer, ComponentDisplay> onItemAdded;
    public Action<ComponentContainer, ComponentDisplay> onItemRemoved;

    [SerializeField] protected Transform m_displayParent;
    public Transform DisplayParent => m_displayParent;

    protected Func<GameDatas.PlayerSave.Component, bool> m_predicate;
    public Func<GameDatas.PlayerSave.Component, bool> Predicate => m_predicate;

    protected EntitySavedData m_unitData;
    protected ComponentDisplay.DisplayMode m_displayMode;

    protected ComponentContainer m_linkedContainer;
    public ComponentContainer LinkedContainer => m_linkedContainer;

    private int m_index;
    public int Index => m_index;

    public virtual void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        m_linkedContainer = _container;
        m_predicate = _predicate;
        m_unitData = _unitData;
        m_displayMode = _displayMode;
        m_index = _index;
    }

    public virtual bool IsValid ( ComponentDisplay _display )
    {
        if (_display.SavedData == null || _display.CurrentContainer == this)
            return false;

        return m_predicate == null || m_predicate(_display.SavedData);
    }

    public void OnDrop ( PointerEventData _eventData )
    {
        ComponentDisplay dropped = _eventData.pointerDrag.GetComponent<ComponentDisplay>();
        if (dropped == null || !IsValid(dropped)) return;

        RemoveFromOrigin(dropped);

        RegisterInteraction(dropped);
    }

    public virtual void RegisterInteraction ( ComponentDisplay _display )
	{
        onItemAdded?.Invoke(this, _display);
    }

    public void RemoveFromOrigin ( ComponentDisplay _display )
    {
        if (_display.CurrentContainer != null)
		{
            _display.CurrentContainer.RemoveDisplay( _display);
        }
    }

    public virtual void RemoveDisplay ( ComponentDisplay _display )
	{
        onItemRemoved?.Invoke(this, _display);
    }
}
