using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Sirenix.OdinInspector;

public class ComponentSlot : ComponentContainer
{
    [SerializeField] private Image m_outline;
    [SerializeField] private Vector2 m_displaySize = new Vector2(124f, 124f);

    [Title("SubComponent")]
    [SerializeField] private Image m_subComponentTypeIconIMG;

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
            RefreshSubComponentTypeIcon();
        }
    }

    private EntityEquipmentData m_equipmentData;
    private GameDatas.PlayerSave.Component m_equipmentSavedData;
    public GameDatas.PlayerSave.Component Equipment => m_equipmentSavedData;
    private bool m_canInteract = true;
    private Func<ComponentDisplay, bool> m_releasePredicate;
    private bool m_isSwapping;
    public bool IsSwapping => m_isSwapping;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        base.Init(_container, _unitData, _componentSavedData, _predicate, _displayMode, _index);
        m_unitData = _unitData;

        if (_componentSavedData != null && !string.IsNullOrEmpty(_componentSavedData.ID))
        {
            m_equipmentSavedData = _componentSavedData;
            m_equipmentData = _componentSavedData.GetData<EntityEquipmentData>();

            if(m_currentDisplay == null)
                m_currentDisplay = Instantiate(GameAssets.current.ui.baseComponentDisplay, m_displayParent);

            m_currentDisplay.Init(_unitData, _componentSavedData, _displayMode);
            m_currentDisplay.CurrentContainer = this;
            m_currentDisplay.transform.SetParent(m_displayParent);
            (m_currentDisplay.transform as RectTransform).anchorMax = new Vector2(.5f, .5f);
            (m_currentDisplay.transform as RectTransform).anchorMin = new Vector2(.5f, .5f);
            m_currentDisplay.transform.localPosition = Vector3.zero;
            (m_currentDisplay.transform as RectTransform).sizeDelta = m_displaySize;
            RefreshSubComponentTypeIcon();
        }
        else
            Cleanup();
    }

    public void SetSubComponentType(EntityEquipmentData.EquipmentType _type )
	{
        m_subComponentTypeIconIMG.sprite = GameAssets.current.ui.componentIcons[_type];
        RefreshSubComponentTypeIcon();
    }

    private void RefreshSubComponentTypeIcon ()
    {
        if (m_subComponentTypeIconIMG != null)
            m_subComponentTypeIconIMG.enabled = m_currentDisplay == null;
    }

    public void SetReleasePredicate ( Func<ComponentDisplay, bool> _releasePredicate )
	{
        m_releasePredicate = _releasePredicate;
    }

    public override bool CanRelease ( ComponentDisplay _display )
	{
        return m_releasePredicate == null || m_releasePredicate(_display);
    }

    public void SetInteractability (bool _canInterract)
	{
        m_canInteract = _canInterract;
    }

    public void SetOutlineColor(Color _color )
	{
        m_outline.color = _color;
    }

    public override bool IsValid ( ComponentDisplay _display )
	{
		return m_currentDisplay != _display && base.IsValid(_display) && m_canInteract && CanRelease(m_currentDisplay);
	}

    public void Cleanup ()
    {
        if (m_currentDisplay != null)
            Destroy(m_currentDisplay.gameObject);

        m_currentDisplay = null;
        m_equipmentData = null;
        m_equipmentSavedData = null;
        m_unitData = null;
        RefreshSubComponentTypeIcon();
    }

    #region DnD

    public override void RegisterInteraction ( ComponentDisplay _display )
    {
        m_isSwapping = m_currentDisplay != null;

        try
        {
            if (m_isSwapping)
            {
                Swap(_display);
            }
            else
            {
                SetEquipment(_display);
            }
            base.RegisterInteraction(_display);
        }
        finally
        {
            m_isSwapping = false;
        }
    }

    public void SetEquipment ( ComponentDisplay _display )
    {
        RemoveDisplay(CurrentDisplay);

        m_currentDisplay = _display;

        _display.CurrentContainer = this;

        _display.transform.SetParent(m_displayParent);
        (_display.transform as RectTransform).anchorMax = new Vector2(.5f, .5f);
        (_display.transform as RectTransform).anchorMin = new Vector2(.5f, .5f);
        _display.transform.localPosition = Vector3.zero;
        (m_currentDisplay.transform as RectTransform).sizeDelta = m_displaySize;
        RefreshSubComponentTypeIcon();
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
            RefreshSubComponentTypeIcon();
		    base.RemoveDisplay(_display);
        }
	}

    #endregion
}
