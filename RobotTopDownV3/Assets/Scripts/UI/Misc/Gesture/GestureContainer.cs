using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;

public abstract class GestureContainer : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum Highlight { None, Target, DropValid, DropInvalid, DropOver }

    private static readonly List<GestureContainer> s_enabledContainers = new();

    [SerializeField] protected Transform m_displayParent;
    public Transform DisplayParent => m_displayParent;

    private Gesture.Refusal m_predicateRefusal = Gesture.Refusal.WrongType;
    private Func<GestureDisplay, GestureContainer> m_gestureTargetProvider;
    private GestureContainer m_linkedContainer;
    private int m_index;
    public int Index => m_index;

    private CanvasGroup m_canvasGroup;
    private AUIPanel m_panel;
    private Highlight m_highlightState;
    private Color m_targetColor;
    private Tween m_pulseTween;
    private Tween m_revealTween;

    protected virtual Graphic HighlightGraphic => null;
    protected virtual Color? RestHighlightColor => null;
    public virtual GestureDisplay Occupant => null;

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
        m_revealTween?.Kill();
        SetHighlight(Highlight.None);
    }

    protected void Link ( GestureContainer _linkedContainer, int _index )
    {
        m_linkedContainer = _linkedContainer;
        m_index = _index;
    }

    public void SetPredicateRefusal ( Gesture.Refusal _refusal )
    {
        m_predicateRefusal = _refusal;
    }

    public void SetGestureTargetProvider ( Func<GestureDisplay, GestureContainer> _provider )
    {
        m_gestureTargetProvider = _provider;
    }

    protected abstract bool Accepts ( GestureDisplay _display );

    public virtual Gesture.Refusal GetReleaseRefusal ( GestureDisplay _display )
    {
        return Gesture.Refusal.None;
    }

    public bool CanRelease ( GestureDisplay _display )
    {
        return GetReleaseRefusal(_display) == Gesture.Refusal.None;
    }

    public virtual Gesture.Refusal GetRefusal ( GestureDisplay _display )
    {
        if (_display.IsPlaceholder || _display.CurrentContainer == this)
            return Gesture.Refusal.Silent;

        if (_display.CurrentContainer != null)
        {
            Gesture.Refusal release = _display.CurrentContainer.GetReleaseRefusal(_display);
            if (release != Gesture.Refusal.None)
                return release;
        }

        return Accepts(_display) ? Gesture.Refusal.None : m_predicateRefusal;
    }

    public bool IsValid ( GestureDisplay _display )
    {
        return GetRefusal(_display) == Gesture.Refusal.None;
    }

    protected abstract Gesture.Kind GetIncomingKind ( GestureDisplay _display );

    public Gesture Evaluate ( GestureDisplay _display )
    {
        return new Gesture(this, GetIncomingKind(_display), GetRefusal(_display));
    }

    public Gesture ResolveGesture ( GestureDisplay _display )
    {
        GestureContainer target = m_linkedContainer != null ? m_linkedContainer : m_gestureTargetProvider?.Invoke(_display);
        return target != null ? target.Evaluate(_display) : Gesture.Refused(Gesture.Refusal.NoFreeSlot);
    }

    public virtual GestureDisplay FindDisplay ( object _content )
    {
        return null;
    }

    public virtual void Reveal ( GestureDisplay _display )
    {
    }

    protected void RevealInScroll ( GestureDisplay _display )
    {
        ScrollRect scroll = m_displayParent != null ? m_displayParent.GetComponentInParent<ScrollRect>() : null;
        if (scroll == null || scroll.content == null || _display == null || _display.transform.parent != m_displayParent)
            return;

        RectTransform content = scroll.content;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, _display.transform);
        Rect view = viewport.rect;
        float margin = GestureFeedback.RevealMargin;
        float offset = bounds.max.y > view.yMax ? bounds.max.y - view.yMax + margin
            : bounds.min.y < view.yMin ? bounds.min.y - view.yMin - margin : 0f;
        if (Mathf.Approximately(offset, 0f))
            return;

        scroll.StopMovement();
        m_revealTween?.Kill();
        m_revealTween = content.DOAnchorPosY(content.anchoredPosition.y - offset, GestureFeedback.RevealDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    public virtual void ReturnDisplay ( GestureDisplay _display )
    {
        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
        _display.transform.SetAsFirstSibling();
    }

    public void OnDrop ( PointerEventData _eventData )
    {
        GestureDisplay dropped = _eventData.pointerDrag != null ? _eventData.pointerDrag.GetComponent<GestureDisplay>() : null;
        if (dropped != null && dropped.IsDragging)
            dropped.ReceiveDrop(Evaluate(dropped));
    }

    public abstract void RegisterInteraction ( GestureDisplay _display );

    public void RemoveFromOrigin ( GestureDisplay _display )
    {
        if (_display.CurrentContainer != null)
            _display.CurrentContainer.RemoveDisplay(_display);
    }

    public abstract void RemoveDisplay ( GestureDisplay _display );

    public void Discard ( GestureDisplay _display )
    {
        RemoveDisplay(_display);
        _display.Discard();
    }

    #region Feedback

    public void OnPointerEnter ( PointerEventData _eventData )
    {
        if (m_highlightState == Highlight.DropValid)
            SetHighlight(Highlight.DropOver);
    }

    public void OnPointerExit ( PointerEventData _eventData )
    {
        if (m_highlightState == Highlight.DropOver)
            SetHighlight(Highlight.DropValid);
    }

    public static void ShowDropTargets ( GestureDisplay _display )
    {
        GestureContainer origin = _display.CurrentContainer;
        if (origin == null)
            return;

        foreach (GestureContainer container in s_enabledContainers)
        {
            if (container != origin && container.m_panel == origin.m_panel)
                container.SetHighlight(container.IsValid(_display) ? Highlight.DropValid : Highlight.DropInvalid);
        }
    }

    public static void ClearDropTargets ()
    {
        foreach (GestureContainer container in s_enabledContainers)
        {
            if (container.m_highlightState >= Highlight.DropValid)
                container.SetHighlight(Highlight.None);
        }
    }

    public void SetHighlight ( Highlight _highlight, Color _color = default )
    {
        m_highlightState = _highlight;
        m_targetColor = _color;
        RefreshHighlight();
    }

    public void ClearTargetHighlight ()
    {
        if (m_highlightState == Highlight.Target)
            SetHighlight(Highlight.None);
    }

    protected void RefreshHighlight ()
    {
        if (m_canvasGroup != null)
            m_canvasGroup.alpha = m_highlightState == Highlight.DropInvalid ? GestureFeedback.DimmedAlpha : 1f;

        Graphic graphic = HighlightGraphic;
        if (graphic == null || m_pulseTween != null)
            return;

        Color? color = m_highlightState switch
        {
            Highlight.Target => m_targetColor,
            Highlight.DropValid => GestureFeedback.DropValidColor,
            Highlight.DropOver => GestureFeedback.DropOverColor,
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
        m_pulseTween = graphic.DOColor(rest, GestureFeedback.PulseDuration).SetEase(Ease.InQuad).SetUpdate(true)
            .OnKill(() => m_pulseTween = null)
            .OnComplete(() =>
            {
                m_pulseTween = null;
                RefreshHighlight();
            });
    }

    #endregion
}
