using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ComponentSlot : ComponentContainer
{
    [SerializeField] private Image m_outline;
    [SerializeField] private GameObject m_timerSectionGO;
    [SerializeField] private TextMeshProUGUI m_timerTMP;
    [SerializeField] private Vector2 m_displaySize = new Vector2(124f, 124f);

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
    private GameDatas.PlayerSave.Component m_equipmentSavedData;
    public GameDatas.PlayerSave.Component Equipment => m_equipmentSavedData;
    private bool m_canInteract = true;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        base.Init(_container, _unitData, _componentSavedData, _predicate, _displayMode, _index);
        m_unitData = _unitData;

        if (m_predicate != null && m_predicate(_componentSavedData))
        {
            m_equipmentSavedData = _componentSavedData;
            m_equipmentData = _componentSavedData.GetData<EntityEquipmentData>();

            if(m_currentDisplay == null)
                m_currentDisplay = Instantiate(GameAssets.current.ui.baseComponentDisplay, m_displayParent);

            m_currentDisplay.Init(_unitData, _componentSavedData, _displayMode);
            m_currentDisplay.CurrentContainer = this;
            m_currentDisplay.transform.SetParent(m_displayParent);
            m_currentDisplay.transform.localPosition = Vector3.zero;
            (m_currentDisplay.transform as RectTransform).sizeDelta = m_displaySize;
        }

        /*if(_displayMode == ComponentDisplay.DisplayMode.RecyclingStation
            || _displayMode == ComponentDisplay.DisplayMode.RepairStation)
		{
            m_timerSectionGO.SetActive(true);
        }
		else
        {
            m_timerSectionGO.SetActive(false);
        }*/
    }

    public void SetInteractability (bool _canInterract)
	{
        m_canInteract = _canInterract;
    }

    public void SetOutlineColor(Color _color )
	{
        m_outline.color = _color;
    }

    public void InitRecyclingData( GameDatas.PlayerSave.DayData.RecyclingComponentData _recyclingData )
	{
        if (_recyclingData != null && _recyclingData.component != null && string.IsNullOrEmpty(_recyclingData.component.ID))
            m_timerTMP.text = "";
        else
            m_timerTMP.text = _recyclingData.remainingTime.ToString();
	}

    public override bool IsValid ( ComponentDisplay _display )
	{
		return m_currentDisplay != _display && base.IsValid(_display) && m_canInteract;
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
        base.RegisterInteraction(_display);
    }

    public void SetEquipment ( ComponentDisplay _display )
    {
        RemoveDisplay(CurrentDisplay);

        m_currentDisplay = _display;

        _display.CurrentContainer = this;

        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
        (m_currentDisplay.transform as RectTransform).sizeDelta = m_displaySize;
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
