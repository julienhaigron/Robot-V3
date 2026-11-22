using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;

public readonly struct ComponentGesture
{
    public enum Kind { Buy, Sell, Equip, Swap, Remove, TakeBack, Deposit }
    public enum Refusal { None, Silent, TooExpensive, Damaged, DamagedSubComponent, OccupantLocked, TournamentLocked, NoFreeSlot, Recycling, WrongType }

    public readonly ComponentContainer target;
    public readonly Kind kind;
    public readonly Refusal refusal;

    public ComponentGesture ( ComponentContainer _target, Kind _kind, Refusal _refusal )
    {
        target = _target;
        kind = _kind;
        refusal = _refusal;
    }

    public static ComponentGesture Refused ( Refusal _refusal ) => new(null, default, _refusal);

    public bool IsValid => refusal == Refusal.None && target != null;
    public bool IsSilent => refusal == Refusal.Silent;
    public bool IsTransaction => kind == Kind.Buy || kind == Kind.Sell;

    public Tuple<CurrencyType, ulong> GetPrice ( EntityEquipmentData _data )
    {
        if (_data == null)
            return null;

        return kind switch
        {
            Kind.Buy => _data.GetShopBuyingPrice(),
            Kind.Sell => _data.GetShopSellingPrice(),
            Kind.Deposit => _data.GetRecyclingPrice(),
            _ => null
        };
    }

    public long GetSignedAmount ( EntityEquipmentData _data )
    {
        Tuple<CurrencyType, ulong> price = GetPrice(_data);
        return price == null ? 0 : kind == Kind.Buy ? -(long)price.Item2 : (long)price.Item2;
    }

    public string GetLabel ( EntityEquipmentData _data )
    {
        LocalizationManager localization = LocalizationManager.Instance;
        if (refusal != Refusal.None)
            return localization.Get(KeyOf(refusal));

        Tuple<CurrencyType, ulong> price = GetPrice(_data);
        return string.Format(localization.Get(KeyOf(kind)), price == null ? 0ul : price.Item2);
    }

    private static LocalizationKey KeyOf ( Kind _kind )
    {
        return _kind switch
        {
            Kind.Buy => LocalizationKey.component_gesture_buy,
            Kind.Sell => LocalizationKey.component_gesture_sell,
            Kind.Equip => LocalizationKey.component_gesture_equip,
            Kind.Swap => LocalizationKey.component_gesture_swap,
            Kind.Remove => LocalizationKey.component_gesture_remove,
            Kind.TakeBack => LocalizationKey.component_gesture_take_back,
            _ => LocalizationKey.component_gesture_deposit
        };
    }

    private static LocalizationKey KeyOf ( Refusal _refusal )
    {
        return _refusal switch
        {
            Refusal.TooExpensive => LocalizationKey.component_refusal_too_expensive,
            Refusal.Damaged => LocalizationKey.component_refusal_damaged,
            Refusal.DamagedSubComponent => LocalizationKey.component_refusal_damaged_sub_component,
            Refusal.OccupantLocked => LocalizationKey.component_refusal_occupant_locked,
            Refusal.TournamentLocked => LocalizationKey.component_refusal_tournament_locked,
            Refusal.NoFreeSlot => LocalizationKey.component_refusal_no_free_slot,
            Refusal.Recycling => LocalizationKey.component_refusal_recycling,
            _ => LocalizationKey.component_refusal_wrong_type
        };
    }
}

public abstract class ComponentContainer : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum Highlight { None, Target, DropValid, DropInvalid, DropOver }

    private static readonly List<ComponentContainer> s_enabledContainers = new();

    public Action<ComponentContainer, ComponentDisplay> onItemAdded;
    public Action<ComponentContainer, ComponentDisplay> onItemRemoved;

    [SerializeField] protected Transform m_displayParent;
    public Transform DisplayParent => m_displayParent;

    protected Func<GameDatas.PlayerSave.Component, bool> m_predicate;
    private ComponentGesture.Refusal m_predicateRefusal = ComponentGesture.Refusal.WrongType;
    private Func<ComponentDisplay, ComponentContainer> m_gestureTargetProvider;

    protected EntitySavedData m_unitData;
    protected ComponentDisplay.DisplayMode m_displayMode;

    protected ComponentContainer m_linkedContainer;
    public ComponentContainer LinkedContainer => m_linkedContainer;

    private int m_index;
    public int Index => m_index;

    private CanvasGroup m_canvasGroup;
    private AUIPanel m_panel;
    private Highlight m_highlight;
    private Color m_targetColor;
    private Tween m_pulseTween;

    protected virtual Graphic HighlightGraphic => null;
    protected virtual Color? RestHighlightColor => null;
    public virtual ComponentDisplay Occupant => null;

    protected virtual void Awake ()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_panel = GetComponentInParent<AUIPanel>(true);
    }

    protected virtual void OnEnable ()
    {
        s_enabledContainers.Add(this);
    }

    protected virtual void OnDisable ()
    {
        s_enabledContainers.Remove(this);
        m_pulseTween?.Kill();
        m_pulseTween = null;
        SetHighlight(Highlight.None);
    }

    public virtual void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        m_linkedContainer = _container;
        m_predicate = _predicate;
        m_unitData = _unitData;
        m_displayMode = _displayMode;
        m_index = _index;
    }

    public void SetPredicateRefusal ( ComponentGesture.Refusal _refusal )
    {
        m_predicateRefusal = _refusal;
    }

    public void SetGestureTargetProvider ( Func<ComponentDisplay, ComponentContainer> _provider )
    {
        m_gestureTargetProvider = _provider;
    }

    public bool Accepts ( GameDatas.PlayerSave.Component _component )
    {
        return m_predicate == null || m_predicate(_component);
    }

    public virtual ComponentGesture.Refusal GetReleaseRefusal ( ComponentDisplay _display )
    {
        return ComponentGesture.Refusal.None;
    }

    public bool CanRelease ( ComponentDisplay _display )
    {
        return GetReleaseRefusal(_display) == ComponentGesture.Refusal.None;
    }

    public virtual ComponentGesture.Refusal GetRefusal ( ComponentDisplay _display )
    {
        if (_display.SavedData == null || _display.CurrentContainer == this)
            return ComponentGesture.Refusal.Silent;

        if (_display.CurrentContainer != null)
        {
            ComponentGesture.Refusal release = _display.CurrentContainer.GetReleaseRefusal(_display);
            if (release != ComponentGesture.Refusal.None)
                return release;
        }

        return Accepts(_display.SavedData) ? ComponentGesture.Refusal.None : m_predicateRefusal;
    }

    public bool IsValid ( ComponentDisplay _display )
    {
        return GetRefusal(_display) == ComponentGesture.Refusal.None;
    }

    protected abstract ComponentGesture.Kind GetIncomingKind ( ComponentDisplay _display );

    public ComponentGesture Evaluate ( ComponentDisplay _display )
    {
        return new ComponentGesture(this, GetIncomingKind(_display), GetRefusal(_display));
    }

    public ComponentGesture ResolveGesture ( ComponentDisplay _display )
    {
        ComponentContainer target = m_linkedContainer != null ? m_linkedContainer : m_gestureTargetProvider?.Invoke(_display);
        return target != null ? target.Evaluate(_display) : ComponentGesture.Refused(ComponentGesture.Refusal.NoFreeSlot);
    }

    public virtual ComponentDisplay FindDisplay ( GameDatas.PlayerSave.Component _component )
    {
        return null;
    }

    public virtual void Reveal ( ComponentDisplay _display )
    {
    }

    public virtual void ReturnDisplay ( ComponentDisplay _display )
    {
        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
        _display.transform.SetAsFirstSibling();
    }

    public void OnDrop ( PointerEventData _eventData )
    {
        ComponentDisplay dropped = _eventData.pointerDrag != null ? _eventData.pointerDrag.GetComponent<ComponentDisplay>() : null;
        if (dropped != null && dropped.IsDragging)
            dropped.ReceiveDrop(Evaluate(dropped));
    }

    public virtual void RegisterInteraction ( ComponentDisplay _display )
	{
        onItemAdded?.Invoke(this, _display);
    }

    public void RemoveFromOrigin ( ComponentDisplay _display )
    {
        if (_display.CurrentContainer != null)
		{
            _display.CurrentContainer.RemoveDisplay( _display);
        }
    }

    public virtual void RemoveDisplay ( ComponentDisplay _display )
	{
        onItemRemoved?.Invoke(this, _display);
    }

    public void Discard ( ComponentDisplay _display )
    {
        RemoveDisplay(_display);
        _display.Discard();
    }

    #region Feedback

    public void OnPointerEnter ( PointerEventData _eventData )
    {
        if (m_highlight == Highlight.DropValid)
            SetHighlight(Highlight.DropOver);
    }

    public void OnPointerExit ( PointerEventData _eventData )
    {
        if (m_highlight == Highlight.DropOver)
            SetHighlight(Highlight.DropValid);
    }

    public static void ShowDropTargets ( ComponentDisplay _display )
    {
        ComponentContainer origin = _display.CurrentContainer;
        if (origin == null)
            return;

        foreach (ComponentContainer container in s_enabledContainers)
        {
            if (container != origin && container.m_panel == origin.m_panel)
                container.SetHighlight(container.IsValid(_display) ? Highlight.DropValid : Highlight.DropInvalid);
        }
    }

    public static void ClearDropTargets ()
    {
        foreach (ComponentContainer container in s_enabledContainers)
        {
            if (container.m_highlight >= Highlight.DropValid)
                container.SetHighlight(Highlight.None);
        }
    }

    public void SetHighlight ( Highlight _highlight, Color _color = default )
    {
        m_highlight = _highlight;
        m_targetColor = _color;
        RefreshHighlight();
    }

    public void ClearTargetHighlight ()
    {
        if (m_highlight == Highlight.Target)
            SetHighlight(Highlight.None);
    }

    protected void RefreshHighlight ()
    {
        if (m_canvasGroup != null)
            m_canvasGroup.alpha = m_highlight == Highlight.DropInvalid ? ComponentGestureFeedback.DimmedAlpha : 1f;

        Graphic graphic = HighlightGraphic;
        if (graphic == null || m_pulseTween != null)
            return;

        Color? color = m_highlight switch
        {
            Highlight.Target => m_targetColor,
            Highlight.DropValid => ComponentGestureFeedback.DropValidColor,
            Highlight.DropOver => ComponentGestureFeedback.DropOverColor,
            _ => RestHighlightColor
        };
        graphic.enabled = color.HasValue;
        if (color.HasValue)
            graphic.color = color.Value;
    }

    public void Pulse ( Color _color )
    {
        Graphic graphic = HighlightGraphic;
        if (graphic == null || !isActiveAndEnabled)
            return;

        m_pulseTween?.Kill();
        Color rest = RestHighlightColor ?? new Color(_color.r, _color.g, _color.b, 0f);
        graphic.enabled = true;
        graphic.color = _color;
        m_pulseTween = graphic.DOColor(rest, ComponentGestureFeedback.PulseDuration).SetEase(Ease.InQuad).SetUpdate(true)
            .OnKill(() => m_pulseTween = null)
            .OnComplete(() =>
            {
                m_pulseTween = null;
                RefreshHighlight();
            });
    }

    #endregion
}
