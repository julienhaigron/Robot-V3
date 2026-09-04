using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using System;

public class RepareUnitSlot : RepareUnitContainer
{
    [SerializeField, FormerlySerializedAs("m_timerSectionGO")] private GameObject m_priceSectionGO;
    [SerializeField, FormerlySerializedAs("m_timerTMP")] private TextMeshProUGUI m_priceTMP;
    [SerializeField] private BaseButton m_repairBtn;
    [SerializeField] private Vector2 m_displaySize = new Vector2(124f, 124f);

    public Action<RepareUnitSlot> onRepairClicked;

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

        if (m_predicate != null && m_predicate(_unitData))
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

        RefreshRepairData();
    }

    public void SetInteractability (bool _canInterract)
	{
        m_canInteract = _canInterract;
        RefreshRepairData();
    }

    private void Awake ()
    {
        if (m_repairBtn != null)
            m_repairBtn.onClick += OnClickRepair;
    }

    private void OnDestroy ()
    {
        if (m_repairBtn != null)
            m_repairBtn.onClick -= OnClickRepair;
    }

    private void OnClickRepair ()
    {
        onRepairClicked?.Invoke(this);
    }

    public void RefreshRepairData ()
    {
        EntitySavedData unit = m_currentDisplay == null ? null : m_currentDisplay.SavedData;
        bool canRepair = m_canInteract && unit != null && !string.IsNullOrEmpty(unit.name) && unit.IsDamaged();

        if (m_priceSectionGO != null)
            m_priceSectionGO.SetActive(true);

        if (!canRepair)
        {
            m_priceTMP.text = "";

            if (m_repairBtn != null)
                m_repairBtn.SetInteractability(false);

            return;
        }

        Tuple<CurrencyType, ulong> price = unit.GetRepairPrice();
        m_priceTMP.text = price.Item2.ToString();

        if (m_repairBtn != null)
            m_repairBtn.SetInteractability(GameDatas.current.currentPlayerSave.currencies[price.Item1] >= price.Item2);
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
