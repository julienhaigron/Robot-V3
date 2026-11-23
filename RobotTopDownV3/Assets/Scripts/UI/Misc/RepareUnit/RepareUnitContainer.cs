using System;

public abstract class RepareUnitContainer : GestureContainer
{
    public Action<RepareUnitContainer, RepareUnitDisplay> onItemAdded;
    public Action<RepareUnitContainer, RepareUnitDisplay> onItemRemoved;

    protected Func<EntitySavedData, bool> m_predicate;

    public virtual void Init ( RepareUnitContainer _container, Func<EntitySavedData, bool> _predicate, int _index = 0 )
    {
        Link(_container, _index);
        m_predicate = _predicate;
    }

    protected override bool Accepts ( GestureDisplay _display )
    {
        return _display is RepareUnitDisplay display && (m_predicate == null || m_predicate(display.SavedData));
    }

    public sealed override void RegisterInteraction ( GestureDisplay _display )
    {
        RegisterInteraction((RepareUnitDisplay)_display);
    }

    public virtual void RegisterInteraction ( RepareUnitDisplay _display )
	{
        onItemAdded?.Invoke(this, _display);
    }

    public sealed override void RemoveDisplay ( GestureDisplay _display )
    {
        RemoveDisplay((RepareUnitDisplay)_display);
    }

    public virtual void RemoveDisplay ( RepareUnitDisplay _display )
	{
        onItemRemoved?.Invoke(this, _display);
    }
}
