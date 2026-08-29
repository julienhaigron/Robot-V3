using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class ComponentDisplayGrid : ComponentContainer
{
    private List<ComponentDisplay> m_items = new();
    public List<ComponentDisplay> Items => m_items;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        base.Init(_container, _unitData, _componentSavedData, _predicate, _displayMode, _index);

        if (_predicate != null)
            RefreshPredicate(_predicate);
        else
            m_predicate = null;
    }

    public ComponentDisplay CreateNewDisplay ( EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, ComponentDisplay.DisplayMode _displayMode )
	{
        ComponentDisplay newDisplay = Instantiate(_displayMode == ComponentDisplay.DisplayMode.ShopBuying 
            ? GameAssets.current.ui.shopComponentDisplay : GameAssets.current.ui.baseComponentDisplay, m_displayParent);
        newDisplay.Init(_unitData, _componentSavedData, _displayMode);

        m_items.Add(newDisplay);
        newDisplay.CurrentContainer = this;
        newDisplay.transform.localPosition = Vector3.zero;

        return newDisplay;
    }

    public void RefreshPredicate ( Func<GameDatas.PlayerSave.Component, bool> _newPredicate )
	{
        m_predicate = _newPredicate;

        foreach (ComponentDisplay display in m_items.ToArray())
		{
            if(m_predicate != null && !m_predicate(display.SavedData))
			{
                m_items.Remove(display);
                Destroy(display.gameObject);
			}
		}

        foreach (GameDatas.PlayerSave.Component eq in GameDatas.current.currentPlayerSave.equipmentInventory)
            if (m_predicate != null && m_predicate(eq) && !m_items.Any(item => string.Equals(item.SavedData.ID, eq)))
                CreateNewDisplay(m_unitData, eq, m_displayMode);
    }


    public void Cleanup ()
	{
        foreach (ComponentDisplay display in m_items)
            Destroy(display.gameObject);

        m_items.Clear();
    }

	#region DnD

	public override bool IsValid ( ComponentDisplay _display )
    {
        return !m_items.Contains(_display) && base.IsValid(_display);
    }

    public override void RegisterInteraction ( ComponentDisplay _component )
    {
        m_items.Add(_component);
        _component.CurrentContainer = this;

        _component.transform.SetParent(m_displayParent);
        _component.transform.localPosition = Vector3.zero;
        base.RegisterInteraction(_component);
    }

	public override void RemoveDisplay ( ComponentDisplay _display )
	{
        if (m_items.Remove(_display))
            base.RemoveDisplay(_display);
	}

	#endregion

}
