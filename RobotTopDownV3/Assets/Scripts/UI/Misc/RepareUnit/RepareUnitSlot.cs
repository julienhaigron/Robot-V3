using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System;
using DG.Tweening;

public class RepareUnitSlot : RepareUnitContainer
{
    [SerializeField, FormerlySerializedAs("m_repairBtn")] private BaseButton m_singleRepairBtn;
    [SerializeField] private GameObject m_singleRepairCurrencyIconGO;
    [SerializeField] private BaseButton m_fullRepairBtn;
    [SerializeField] private GameObject m_fullRepairCurrencyIconGO;
    [SerializeField] private SlidingDoors m_lockDoors;
    [SerializeField] private Vector2 m_displaySize = new Vector2(124f, 124f);
    [SerializeField] private Graphic m_highlight;

    public Action<RepareUnitSlot, bool> onRepairClicked;

    private bool m_isSingleRepairBtnHovered;
    private bool m_isFullRepairBtnHovered;
    private Tween m_singleRepairCall;
    private Tween m_fullRepairCall;

    private RepareUnitDisplay m_currentDisplay;
    public RepareUnitDisplay CurrentDisplay => m_currentDisplay;

    private bool m_canInteract = true;

    protected override Graphic HighlightGraphic => m_highlight;
    protected override Color? RestHighlightColor => m_currentDisplay != null ? GestureFeedback.PendingColor : null;

    public void Init ( RepareUnitContainer _container, EntitySavedData _unitData, int _index )
    {
        base.Init(_container, null, _index);

        if (_unitData != null && !string.IsNullOrEmpty(_unitData.name))
        {
            if (m_currentDisplay == null)
                m_currentDisplay = Instantiate(GameAssets.current.ui.repareUnitDisplay, m_displayParent);

            m_currentDisplay.Init(_unitData);
            Place(m_currentDisplay);
            RefreshHighlight();
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

    protected override void Awake ()
    {
        base.Awake();

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

    protected override void OnEnable ()
    {
        base.OnEnable();
        LocalizationManager.onLanguageChanged += RefreshRepairData;
    }

    protected override void OnDisable ()
    {
        base.OnDisable();
        LocalizationManager.onLanguageChanged -= RefreshRepairData;
        m_isSingleRepairBtnHovered = false;
        m_isFullRepairBtnHovered = false;
        if (m_singleRepairBtn != null)
            GestureFeedback.SetCalling(m_singleRepairBtn.transform, ref m_singleRepairCall, false);
        if (m_fullRepairBtn != null)
            GestureFeedback.SetCalling(m_fullRepairBtn.transform, ref m_fullRepairCall, false);
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
        EntitySavedData repairable = m_canInteract && unit != null && !string.IsNullOrEmpty(unit.name) && unit.IsDamaged() ? unit : null;

        RefreshRepairButton(m_singleRepairBtn, m_singleRepairCurrencyIconGO, LocalizationKey.hub_single_repair, m_isSingleRepairBtnHovered, repairable, false, ref m_singleRepairCall);
        RefreshRepairButton(m_fullRepairBtn, m_fullRepairCurrencyIconGO, LocalizationKey.hub_full_repair, m_isFullRepairBtnHovered, repairable, true, ref m_fullRepairCall);
    }

    private void RefreshRepairButton ( BaseButton _button, GameObject _currencyIconGO, LocalizationKey _labelKey, bool _isHovered, EntitySavedData _unit, bool _isFullRepair, ref Tween _call )
    {
        if (_button == null)
            return;

        Tuple<CurrencyType, ulong> price = _unit == null || _unit.GetComponentsToRepair(_isFullRepair).Count == 0 ? null : _unit.GetRepairPrice(_isFullRepair);
        bool isAffordable = price != null && GameDatas.current.currentPlayerSave.currencies[price.Item1] >= price.Item2;
        bool isShowingPrice = price != null && _isHovered;

        _button.SetLabel(!isShowingPrice ? LocalizationManager.Instance.Get(_labelKey)
            : isAffordable ? price.Item2.ToString()
            : "<color=#" + ColorUtility.ToHtmlStringRGB(GestureFeedback.RefusalColor) + ">" + price.Item2 + "</color>");

        if (_currencyIconGO != null)
            _currencyIconGO.SetActive(isShowingPrice);

        _button.SetInteractability(isAffordable);
        GestureFeedback.SetCalling(_button.transform, ref _call, isAffordable && isActiveAndEnabled);
    }

    public override Gesture.Refusal GetRefusal ( GestureDisplay _display )
	{
        if (m_currentDisplay == _display)
            return Gesture.Refusal.Silent;

        if (!m_canInteract)
            return Gesture.Refusal.UsedToday;

        return m_currentDisplay != null ? Gesture.Refusal.SlotOccupied : base.GetRefusal(_display);
	}

    protected override Gesture.Kind GetIncomingKind ( GestureDisplay _display )
    {
        return Gesture.Kind.Deposit;
    }

    public override GestureDisplay FindDisplay ( object _content )
    {
        return _content is EntitySavedData unit && m_currentDisplay != null && m_currentDisplay.SavedData == unit ? m_currentDisplay : null;
    }

    public override void Reveal ( GestureDisplay _display )
    {
        RevealInScroll(_display);
    }

    public void Cleanup ()
    {
        if (m_currentDisplay != null)
            Destroy(m_currentDisplay.gameObject);

        m_currentDisplay = null;
        RefreshHighlight();
        RefreshRepairData();
    }

    private void Place ( RepareUnitDisplay _display )
    {
        _display.CurrentContainer = this;
        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
        ((RectTransform)_display.transform).sizeDelta = m_displaySize;
    }

    #region DnD

    public override void RegisterInteraction ( RepareUnitDisplay _display )
    {
        m_currentDisplay = _display;
        Place(_display);
        RefreshHighlight();
        base.RegisterInteraction(_display);
    }

	public override void RemoveDisplay ( RepareUnitDisplay _display )
	{
        if (m_currentDisplay != _display || _display == null)
            return;

        m_currentDisplay = null;
        RefreshHighlight();
        base.RemoveDisplay(_display);
	}

    #endregion
}
