using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

public class RepareUnitDisplayGrid : RepareUnitContainer
{
    [SerializeField] private Graphic m_highlight;

    private readonly List<RepareUnitDisplay> m_items = new();

    protected override Graphic HighlightGraphic => m_highlight;

    public override void Init ( RepareUnitContainer _container, Func<EntitySavedData, bool> _predicate, int _index = 0 )
    {
        base.Init(_container, _predicate, _index);

        if (_predicate != null)
            RefreshPredicate(_predicate);
        else
            m_predicate = null;
    }

    public RepareUnitDisplay CreateNewDisplay ( EntitySavedData _unitData )
	{
        RepareUnitDisplay newDisplay = Instantiate(GameAssets.current.ui.repareUnitDisplay, m_displayParent);
        newDisplay.Init(_unitData);

        m_items.Add(newDisplay);
        newDisplay.CurrentContainer = this;
        newDisplay.transform.localPosition = Vector3.zero;

        return newDisplay;
    }

    public void RefreshPredicate ( Func<EntitySavedData, bool> _newPredicate )
	{
        m_predicate = _newPredicate;

        foreach (RepareUnitDisplay display in m_items.ToArray())
		{
            if(m_predicate != null && !m_predicate(display.SavedData))
			{
                m_items.Remove(display);
                Destroy(display.gameObject);
			}
		}

        foreach (EntitySavedData unit in GameDatas.current.currentPlayerSave.allBuiltUnits)
            if (m_predicate != null && m_predicate(unit) && !m_items.Any(item => item.SavedData == unit))
                CreateNewDisplay(unit);
    }

    public void Cleanup ()
	{
        foreach (RepareUnitDisplay display in m_items)
            Destroy(display.gameObject);

        m_items.Clear();
    }

    public override GestureDisplay FindDisplay ( object _content )
    {
        return _content is EntitySavedData unit ? m_items.Find(item => item != null && item.SavedData == unit) : null;
    }

    public override void Reveal ( GestureDisplay _display )
    {
        RevealInScroll(_display);
    }

	#region DnD

	public override Gesture.Refusal GetRefusal ( GestureDisplay _display )
    {
        return _display is RepareUnitDisplay display && m_items.Contains(display) ? Gesture.Refusal.Silent : base.GetRefusal(_display);
    }

    protected override Gesture.Kind GetIncomingKind ( GestureDisplay _display )
    {
        return Gesture.Kind.TakeBack;
    }

    public override void RegisterInteraction ( RepareUnitDisplay _display )
    {
        m_items.Add(_display);
        _display.CurrentContainer = this;

        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
        base.RegisterInteraction(_display);
    }

	public override void RemoveDisplay ( RepareUnitDisplay _display )
	{
        if (m_items.Remove(_display))
            base.RemoveDisplay(_display);
	}

	#endregion

}
