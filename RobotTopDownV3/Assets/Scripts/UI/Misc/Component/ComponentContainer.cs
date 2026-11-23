using System;

public abstract class ComponentContainer : GestureContainer
{
    public Action<ComponentContainer, ComponentDisplay> onItemAdded;
    public Action<ComponentContainer, ComponentDisplay> onItemRemoved;

    protected Func<GameDatas.PlayerSave.Component, bool> m_predicate;
    protected EntitySavedData m_unitData;
    protected ComponentDisplay.DisplayMode m_displayMode;

    public virtual void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        Link(_container, _index);
        m_predicate = _predicate;
        m_unitData = _unitData;
        m_displayMode = _displayMode;
    }

    public bool Accepts ( GameDatas.PlayerSave.Component _component )
    {
        return m_predicate == null || m_predicate(_component);
    }

    protected override bool Accepts ( GestureDisplay _display )
    {
        return _display is ComponentDisplay display && Accepts(display.SavedData);
    }

    public sealed override void RegisterInteraction ( GestureDisplay _display )
    {
        RegisterInteraction((ComponentDisplay)_display);
    }

    public virtual void RegisterInteraction ( ComponentDisplay _display )
	{
        onItemAdded?.Invoke(this, _display);
    }

    public sealed override void RemoveDisplay ( GestureDisplay _display )
    {
        RemoveDisplay((ComponentDisplay)_display);
    }

    public virtual void RemoveDisplay ( ComponentDisplay _display )
	{
        onItemRemoved?.Invoke(this, _display);
    }
}
