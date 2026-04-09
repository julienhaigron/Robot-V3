using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class ComponentContainer : MonoBehaviour, IDropHandler
{
    public event Action<ComponentDisplay> onItemAdded;
    public event Action<ComponentDisplay> onItemRemoved;

    [SerializeField] protected Transform m_displayParent;
    public Transform DisplayParent => m_displayParent;

    protected Func<GameDatas.PlayerSave.Equipment, bool> m_predicate;

    protected ComponentContainer m_linkedContainer;
    public ComponentContainer LinkedContainer => m_linkedContainer;

    public virtual void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Equipment _componentSavedData, Func<GameDatas.PlayerSave.Equipment, bool> _predicate, ComponentDisplay.DisplayMode _displayMode )
    {
        m_linkedContainer = _container;
        m_predicate = _predicate;
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

        onItemAdded.Invoke(dropped);
    }

    public abstract void RegisterInteraction ( ComponentDisplay _display);

    public void RemoveFromOrigin ( ComponentDisplay _display )
    {
        if (_display.CurrentContainer != null)
		{
            _display.CurrentContainer.RemoveDisplay( _display);
        }
    }

    public virtual void RemoveDisplay ( ComponentDisplay _display )
	{
        onItemRemoved?.Invoke(_display);
    }
}
