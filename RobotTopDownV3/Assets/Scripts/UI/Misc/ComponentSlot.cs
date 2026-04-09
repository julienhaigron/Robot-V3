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
    private GameDatas.PlayerSave.Equipment m_equipmentSavedData;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Equipment _componentSavedData, Func<GameDatas.PlayerSave.Equipment, bool> _predicate, ComponentDisplay.DisplayMode _displayMode )
    {
        base.Init(_container, _unitData, _componentSavedData, _predicate, _displayMode);
        m_unitData = _unitData;

        if (m_predicate != null && m_predicate(_componentSavedData))
        {
            m_equipmentSavedData = _componentSavedData;
            m_openEntityConfigBtn.onClick = null;
            m_equipmentData = _componentSavedData.GetData<EntityEquipmentData>();
            m_openEntityConfigBtn.onClick += OnClickOpenEntityConfigBtn;
            m_openEntityConfigBtn.gameObject.SetActive(m_equipmentData != null && (m_equipmentData.GetEquipmentType() == EntityEquipmentData.EquipmentType.Frame
                || m_equipmentData.GetEquipmentType() == EntityEquipmentData.EquipmentType.Brain || m_equipmentData.GetEquipmentType() == EntityEquipmentData.EquipmentType.NeuronalMembrane));

            ComponentDisplay newDisplay = Instantiate(GameAssets.current.ui.baseComponentDisplay, m_displayParent);
            newDisplay.Init(_unitData, _componentSavedData, _displayMode);

            m_currentDisplay = newDisplay;
            newDisplay.CurrentContainer = this;
            newDisplay.transform.SetParent(m_displayParent);
            newDisplay.transform.localPosition = Vector3.zero;
        }
        else
            m_openEntityConfigBtn.gameObject.SetActive(false);
    }

    private void OnClickOpenEntityConfigBtn ()
    {
        UIManager.Instance.OpenPanel<EntityComponentConfigPanel>().Init(m_unitData, m_equipmentSavedData);
    }

    public void Cleanup ()
    {
        if(m_currentDisplay != null)
		{
            Destroy(m_currentDisplay.gameObject);
            m_equipmentData = null;
            m_equipmentSavedData = null;
            m_unitData = null;
        }
    }

    #region DnD

    public override void RegisterInteraction ( ComponentDisplay _component )
    {
        if (m_currentDisplay != null)
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
        if (m_currentDisplay != null)
            onItemRemoved?.Invoke(m_currentDisplay);

        m_currentDisplay = equipment;

        equipment.CurrentContainer = this;

        equipment.transform.SetParent(m_displayParent);
        equipment.transform.localPosition = Vector3.zero;

        onItemAdded?.Invoke(equipment);
    }

    private void Swap ( ComponentDisplay incoming )
    {
        ComponentDisplay temp = m_currentDisplay;

        SetEquipment(incoming);

        if (incoming.CurrentContainer != null && incoming.CurrentContainer is ComponentSlot originSlot)
            originSlot.SetEquipment(temp);
        else
            temp.ReturnToOrigin();
    }

    public void Clear ()
    {
        if (m_currentDisplay != null)
        {
            onItemRemoved?.Invoke(m_currentDisplay);
            m_currentDisplay = null;
        }
    }

    #endregion
}
