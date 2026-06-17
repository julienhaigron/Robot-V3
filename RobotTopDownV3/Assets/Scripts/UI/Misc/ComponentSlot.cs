using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ComponentSlot : ComponentContainer
{
    [SerializeField] private ComponentSlot[] m_subSlots;

    protected ComponentDisplay m_currentDisplay;
    public ComponentDisplay CurrentDisplay
    {
        get
        {
            return m_currentDisplay;
        }
        set
        {
            m_currentDisplay = value;
        }
    }

    private EntityEquipmentData m_equipmentData;
    private GameDatas.PlayerSave.Equipment m_equipmentSavedData;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Equipment _componentSavedData, Func<GameDatas.PlayerSave.Equipment, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode )
    {
        base.Init(_container, _unitData, _componentSavedData, _predicate, _displayMode);
        m_unitData = _unitData;

        if (m_predicate != null && m_predicate(_componentSavedData))
        {
            m_equipmentSavedData = _componentSavedData;
            m_equipmentData = _componentSavedData.GetData<EntityEquipmentData>();

            ComponentDisplay newDisplay = Instantiate(GameAssets.current.ui.baseComponentDisplay, m_displayParent);
            newDisplay.Init(_unitData, _componentSavedData, _displayMode);

            m_currentDisplay = newDisplay;
            newDisplay.CurrentContainer = this;
            newDisplay.transform.SetParent(m_displayParent);
            newDisplay.transform.localPosition = Vector3.zero;
        }

        if(_displayMode == ComponentDisplay.DisplayMode.Hangar)
		{
            //handle sub slot => done in EntityConfigPanel
		}
        else if (_displayMode == ComponentDisplay.DisplayMode.ShopSelling)
		{
            //TODO : handle reroll btn
		}
    }

	public override bool IsValid ( ComponentDisplay _display )
	{
		return m_currentDisplay != _display && base.IsValid(_display);
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

    public override void RegisterInteraction ( ComponentDisplay _display )
    {
        if (m_currentDisplay != null)
        {
            Swap(_display);
        }
        else
        {
            SetEquipment(_display);
        }
    }

    public void SetEquipment ( ComponentDisplay _display )
    {
        RemoveDisplay(CurrentDisplay);

        m_currentDisplay = _display;

        _display.CurrentContainer = this;

        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
    }

    private void Swap ( ComponentDisplay _display )
    {
        ComponentDisplay temp = m_currentDisplay;
        ComponentContainer previousContainer = _display.CurrentContainer;
        RemoveDisplay(CurrentDisplay);

        SetEquipment(_display);

        if (previousContainer != null)
            previousContainer.RegisterInteraction(temp);
        else
            temp.ReturnToOrigin();
    }

	public override void RemoveDisplay ( ComponentDisplay _display )
	{
        if (m_currentDisplay == _display && _display != null)
        {
            m_currentDisplay = null;
		    base.RemoveDisplay(_display);
        }
	}

    #endregion
}
