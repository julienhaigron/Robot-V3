using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class RepareUnitDisplayGrid : RepareUnitContainer
{
    private List<RepareUnitDisplay> m_items = new();
    public List<RepareUnitDisplay> Items => m_items;

    public override void Init ( RepareUnitContainer _container, EntitySavedData _unitData, Func<EntitySavedData, bool> _predicate
        , int _index = 0 )
    {
        base.Init(_container, _unitData, _predicate, _index);

        if (_predicate != null)
            RefreshPredicate(_predicate);
        else
            m_predicate = null;
    }

    public RepareUnitDisplay CreateNewDisplay ( EntitySavedData _unitData)
	{
        RepareUnitDisplay newDisplay = Instantiate(GameAssets.current.ui.repareUnitDisplay, m_displayParent);
        newDisplay.Init(_unitData, false);

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
                CreateNewDisplay(m_unitData);
    }


    public void Cleanup ()
	{
        foreach (RepareUnitDisplay display in m_items)
            Destroy(display.gameObject);

        m_items.Clear();
    }

	#region DnD

	public override bool IsValid ( RepareUnitDisplay _display )
    {
        return !m_items.Contains(_display) && base.IsValid(_display);
    }

    public override void RegisterInteraction ( RepareUnitDisplay _component )
    {
        m_items.Add(_component);
        _component.CurrentContainer = this;

        _component.transform.SetParent(m_displayParent);
        _component.transform.localPosition = Vector3.zero;
    }

	public override void RemoveDisplay ( RepareUnitDisplay _display )
	{
        if (m_items.Remove(_display))
            base.RemoveDisplay(_display);
	}

	#endregion

}
