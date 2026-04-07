using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;

public class ComponentDisplayGrid : ComponentContainer
{
    public event Action<ComponentDisplay> onItemAdded;
    public event Action<ComponentDisplay> onItemRemoved;

    [SerializeField] private Transform m_itemsParent;

    private List<ComponentDisplay> m_items = new();
    public List<ComponentDisplay> Items => m_items;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, EntityEquipmentData _equipmentData, Func<EntityEquipmentData, bool> _predicate, ComponentDisplay.DisplayMode _displayMode )
    {
        base.Init(_container, _unitData, _equipmentData, _predicate, _displayMode);

        foreach (GameDatas.PlayerSave.Equipment eq in GameDatas.current.currentPlayerSave.equipmentInventory)
		{
            EntityEquipmentData data = eq.GetData<EntityEquipmentData>();
			if (IsValid(data))
            {
                ComponentDisplay newDisplay = Instantiate(GameAssets.current.ui.baseComponentDisplay, m_itemsParent);
                newDisplay.Init(_unitData, eq, data, _displayMode);

                m_items.Add(newDisplay);
                newDisplay.CurrentContainer = this;
                newDisplay.transform.SetParent(transform);
                newDisplay.transform.localPosition = Vector3.zero;
            }

        }
    }

    public void Cleanup ()
	{
        foreach (ComponentDisplay display in m_items)
            Destroy(display);

        m_items.Clear();

    }

    #region DnD

    public override void RegisterInteraction ( ComponentDisplay _component )
    {
        m_items.Add(_component);
        _component.CurrentContainer = this;

        _component.transform.SetParent(transform);
        _component.transform.localPosition = Vector3.zero;

        onItemAdded?.Invoke(_component);
    }

    public void RemoveItem ( ComponentDisplay item )
    {
        if (Items.Remove(item))
            onItemRemoved?.Invoke(item);
    }

	#endregion

}
