using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ComponentSlot : ComponentContainer
{
    public event Action<ComponentDisplay> onItemAdded;
    public event Action<ComponentDisplay> onItemRemoved;

    [SerializeField] private Transform m_displayParent;
    [SerializeField] private BaseButton m_openEntityConfigBtn;

    private EntitySavedData m_unitData;
    private EntityEquipmentData m_equipmentData;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, EntityEquipmentData _equipmentData, Func<EntityEquipmentData, bool> _predicate, ComponentDisplay.DisplayMode _displayMode )
    {
        base.Init(_container, _unitData, _equipmentData, _predicate, _displayMode);
        m_unitData = _unitData;
        m_equipmentData = _equipmentData;
        m_openEntityConfigBtn.onClick += OnClickOpenEntityConfigBtn;

        if (IsValid(_equipmentData))
        {
            ComponentDisplay newDisplay = Instantiate(GameAssets.current.ui.baseComponentDisplay, m_displayParent);
            newDisplay.Init(_unitData, null, _equipmentData, _displayMode);

            m_currentEquipment = newDisplay;
            newDisplay.CurrentContainer = this;
            newDisplay.transform.SetParent(transform);
            newDisplay.transform.localPosition = Vector3.zero;
        }

    }

    private void OnClickOpenEntityConfigBtn ()
    {
        UIManager.Instance.OpenPopup<EntityComponentConfigPopup>().Init(m_unitData, m_equipmentData);
    }

    #region DnD

    public override void RegisterInteraction ( ComponentDisplay _component )
    {
        if (m_currentEquipment != null)
        {
            Swap(_component);
        }
        else
        {
            SetEquipment(_component);
        }
    }

    public void SetEquipment ( ComponentDisplay equipment )
    {
        if (m_currentEquipment != null)
            onItemRemoved?.Invoke(m_currentEquipment);

        m_currentEquipment = equipment;

        equipment.CurrentContainer = this;

        equipment.transform.SetParent(transform);
        equipment.transform.localPosition = Vector3.zero;

        onItemAdded?.Invoke(equipment);
    }

    private void Swap ( ComponentDisplay incoming )
    {
        ComponentDisplay temp = m_currentEquipment;

        SetEquipment(incoming);

        if (incoming.CurrentContainer != null && incoming.CurrentContainer is ComponentSlot originSlot)
            originSlot.SetEquipment(temp);
        else
            temp.ReturnToOrigin();
    }

    public void Clear ()
    {
        if (m_currentEquipment != null)
        {
            onItemRemoved?.Invoke(m_currentEquipment);
            m_currentEquipment = null;
        }
    }

    #endregion
}
