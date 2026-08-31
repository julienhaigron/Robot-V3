using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class RepareUnitContainer : MonoBehaviour, IDropHandler
{
    public Action<RepareUnitContainer, RepareUnitDisplay> onItemAdded;
    public Action<RepareUnitContainer, RepareUnitDisplay> onItemRemoved;

    [SerializeField] protected Transform m_displayParent;
    public Transform DisplayParent => m_displayParent;

    protected Func<EntitySavedData, bool> m_predicate;
    public Func<EntitySavedData, bool> Predicate => m_predicate;

    protected EntitySavedData m_unitData;

    protected RepareUnitContainer m_linkedContainer;
    public RepareUnitContainer LinkedContainer => m_linkedContainer;

    private int m_index;
    public int Index => m_index;

    public virtual void Init ( RepareUnitContainer _container, EntitySavedData _unitData, Func<EntitySavedData, bool> _predicate
        , int _index = 0 )
    {
        m_linkedContainer = _container;
        m_predicate = _predicate;
        m_unitData = _unitData;
        m_index = _index;
    }

    public virtual bool IsValid ( RepareUnitDisplay _display )
    {
        if (_display.SavedData == null || _display.CurrentContainer == this)
            return false;

        return m_predicate == null || m_predicate(_display.SavedData);
    }

    public void OnDrop ( PointerEventData _eventData )
    {
        RepareUnitDisplay dropped = _eventData.pointerDrag.GetComponent<RepareUnitDisplay>();
        if (dropped == null || !IsValid(dropped)) return;

        RemoveFromOrigin(dropped);

        RegisterInteraction(dropped);
    }

    public virtual void RegisterInteraction ( RepareUnitDisplay _display )
	{
        onItemAdded?.Invoke(this, _display);
    }

    public void RemoveFromOrigin ( RepareUnitDisplay _display )
    {
        if (_display.CurrentContainer != null)
		{
            _display.CurrentContainer.RemoveDisplay( _display);
        }
    }

    public virtual void RemoveDisplay ( RepareUnitDisplay _display )
	{
        onItemRemoved?.Invoke(this, _display);
    }
}
