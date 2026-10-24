using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using System;

public class RepareUnitSlot : RepareUnitContainer
{
    [SerializeField, FormerlySerializedAs("m_repairBtn")] private BaseButton m_singleRepairBtn;
    [SerializeField] private GameObject m_singleRepairCurrencyIconGO;
    [SerializeField] private BaseButton m_fullRepairBtn;
    [SerializeField] private GameObject m_fullRepairCurrencyIconGO;
    [SerializeField] private SlidingDoors m_lockDoors;
    [SerializeField] private Vector2 m_displaySize = new Vector2(124f, 124f);

    public Action<RepareUnitSlot, bool> onRepairClicked;

    private bool m_isSingleRepairBtnHovered;
    private bool m_isFullRepairBtnHovered;

    protected RepareUnitDisplay m_currentDisplay;
    public RepareUnitDisplay CurrentDisplay
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

    private EntitySavedData m_unitSavedData;
    public EntitySavedData Unit => m_unitSavedData;
    private bool m_canInteract = true;

    public override void Init ( RepareUnitContainer _container, EntitySavedData _unitData, Func<EntitySavedData, bool> _predicate
        , int _index = 0 )
    {
        base.Init(_container, _unitData, _predicate, _index);
        m_unitData = _unitData;

        if (_unitData != null && !string.IsNullOrEmpty(_unitData.name))
        {
            m_unitSavedData = _unitData;

            if(m_currentDisplay == null)
                m_currentDisplay = Instantiate(GameAssets.current.ui.repareUnitDisplay, m_displayParent);

            m_currentDisplay.Init(_unitData, false);
            m_currentDisplay.CurrentContainer = this;
            m_currentDisplay.transform.SetParent(m_displayParent);
            m_currentDisplay.transform.localPosition = Vector3.zero;
            (m_currentDisplay.transform as RectTransform).sizeDelta = m_displaySize;
        }
        else
            Cleanup();

        RefreshRepairData();
    }

    public void SetInteractability (bool _canInterract, bool _isInstant = true)
	{
        m_canInteract = _canInterract;

        if (m_lockDoors != null)
            m_lockDoors.SetClosed(!_canInterract, _isInstant);

        RefreshRepairData();
    }

    private void Awake ()
    {
        if (m_singleRepairBtn != null)
        {
            m_singleRepairBtn.onClick += OnClickSingleRepair;
            m_singleRepairBtn.onHoverChanged += OnSingleRepairBtnHoverChanged;
        }

        if (m_fullRepairBtn != null)
        {
            m_fullRepairBtn.onClick += OnClickFullRepair;
            m_fullRepairBtn.onHoverChanged += OnFullRepairBtnHoverChanged;
        }
    }

    private void OnDestroy ()
    {
        if (m_singleRepairBtn != null)
        {
            m_singleRepairBtn.onClick -= OnClickSingleRepair;
            m_singleRepairBtn.onHoverChanged -= OnSingleRepairBtnHoverChanged;
        }

        if (m_fullRepairBtn != null)
        {
            m_fullRepairBtn.onClick -= OnClickFullRepair;
            m_fullRepairBtn.onHoverChanged -= OnFullRepairBtnHoverChanged;
        }
    }

    private void OnEnable ()
    {
        LocalizationManager.onLanguageChanged += RefreshRepairData;
    }

    private void OnDisable ()
    {
        LocalizationManager.onLanguageChanged -= RefreshRepairData;
        m_isSingleRepairBtnHovered = false;
        m_isFullRepairBtnHovered = false;
    }

    private void OnClickSingleRepair ()
    {
        onRepairClicked?.Invoke(this, false);
    }

    private void OnClickFullRepair ()
    {
        onRepairClicked?.Invoke(this, true);
    }

    private void OnSingleRepairBtnHoverChanged ( bool _isHovered )
    {
        m_isSingleRepairBtnHovered = _isHovered;
        RefreshRepairData();
    }

    private void OnFullRepairBtnHoverChanged ( bool _isHovered )
    {
        m_isFullRepairBtnHovered = _isHovered;
        RefreshRepairData();
    }

    public void RefreshRepairData ()
    {
        EntitySavedData unit = m_currentDisplay == null ? null : m_currentDisplay.SavedData;
        bool canRepair = m_canInteract && unit != null && !string.IsNullOrEmpty(unit.name) && unit.IsDamaged();

        RefreshRepairButton(m_singleRepairBtn, m_singleRepairCurrencyIconGO, LocalizationKey.hub_single_repair, m_isSingleRepairBtnHovered, canRepair ? unit : null, false);
        RefreshRepairButton(m_fullRepairBtn, m_fullRepairCurrencyIconGO, LocalizationKey.hub_full_repair, m_isFullRepairBtnHovered, canRepair ? unit : null, true);
    }

    private void RefreshRepairButton ( BaseButton _button, GameObject _currencyIconGO, LocalizationKey _labelKey, bool _isHovered, EntitySavedData _unit, bool _isFullRepair )
    {
        if (_button == null)
            return;

        Tuple<CurrencyType, ulong> price = _unit == null || _unit.GetComponentsToRepair(_isFullRepair).Count == 0 ? null : _unit.GetRepairPrice(_isFullRepair);
        bool isShowingPrice = price != null && _isHovered;

        _button.SetLabel(isShowingPrice ? price.Item2.ToString() : LocalizationManager.Instance.Get(_labelKey));

        if (_currencyIconGO != null)
            _currencyIconGO.SetActive(isShowingPrice);

        _button.SetInteractability(price != null && GameDatas.current.currentPlayerSave.currencies[price.Item1] >= price.Item2);
    }

    public override bool IsValid ( RepareUnitDisplay _display )
	{
		return m_currentDisplay != _display && base.IsValid(_display) && m_canInteract;
	}

    public void Cleanup ()
    {
        if(m_currentDisplay != null)
		{
            Destroy(m_currentDisplay.gameObject);
            m_currentDisplay = null;
            m_unitSavedData = null;
            m_unitData = null;
        }
        RefreshRepairData();
    }

    #region DnD

    public override void RegisterInteraction ( RepareUnitDisplay _display )
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

    public void SetEquipment ( RepareUnitDisplay _display )
    {
        RemoveDisplay(CurrentDisplay);

        m_currentDisplay = _display;

        _display.CurrentContainer = this;

        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
    }

    private void Swap ( RepareUnitDisplay _display )
    {
        RepareUnitDisplay temp = m_currentDisplay;
        RepareUnitContainer previousContainer = _display.CurrentContainer;
        RemoveDisplay(CurrentDisplay);

        SetEquipment(_display);

        if (previousContainer != null)
            previousContainer.RegisterInteraction(temp);
        else
            temp.ReturnToOrigin();
    }

	public override void RemoveDisplay ( RepareUnitDisplay _display )
	{
        if (m_currentDisplay == _display && _display != null)
        {
            m_currentDisplay = null;
		    base.RemoveDisplay(_display);
        }
	}

    #endregion
}
