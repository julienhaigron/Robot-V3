using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using DG.Tweening;

public class ComponentDisplayGrid : ComponentContainer
{
    [SerializeField] private Graphic m_highlight;
    [SerializeField] private float m_revealMargin = 8f;

    private List<ComponentDisplay> m_items = new();
    public List<ComponentDisplay> Items => m_items;

    private bool m_areMainComponentsHighlighted;
    private int m_capacity;
    private Tween m_revealTween;

    protected override Graphic HighlightGraphic => m_highlight;

    public override void Init ( ComponentContainer _container, EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, Func<GameDatas.PlayerSave.Component, bool> _predicate
        , ComponentDisplay.DisplayMode _displayMode, int _index = 0 )
    {
        base.Init(_container, _unitData, _componentSavedData, _predicate, _displayMode, _index);

        if (_predicate != null)
            RefreshPredicate(_predicate);
        else
            m_predicate = null;
    }

    protected override void OnDisable ()
    {
        base.OnDisable();
        m_revealTween?.Kill();
    }

    public ComponentDisplay CreateNewDisplay ( EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, ComponentDisplay.DisplayMode _displayMode )
	{
        ComponentDisplay newDisplay = Instantiate(_displayMode == ComponentDisplay.DisplayMode.ShopBuying
            ? GameAssets.current.ui.shopComponentDisplay : GameAssets.current.ui.baseComponentDisplay, m_displayParent);
        newDisplay.Init(_unitData, _componentSavedData, _displayMode);

        m_items.Add(newDisplay);
        newDisplay.CurrentContainer = this;
        newDisplay.transform.localPosition = Vector3.zero;

        SortByAcquisitionDate();
        RefreshMainComponentHighlight(newDisplay);

        return newDisplay;
    }

    public void Pad ( int _capacity )
    {
        m_capacity = _capacity;
        while (m_items.Count < m_capacity)
            CreateNewDisplay(null, null, ComponentDisplay.DisplayMode.Empty);
    }

    public void SetMainComponentsHighlighted ( bool _isHighlighted )
    {
        m_areMainComponentsHighlighted = _isHighlighted;

        foreach (ComponentDisplay display in m_items)
            RefreshMainComponentHighlight(display);
    }

    private void RefreshMainComponentHighlight ( ComponentDisplay _display )
    {
        if (_display != null)
            _display.SetTutorialHighlighted(m_areMainComponentsHighlighted && _display.ComponentData != null && _display.ComponentData.IsMainComponent());
    }

    public void SortByAcquisitionDate ()
    {
        m_items.Sort(( _a, _b ) =>
        {
            int dateComparison = GetAcquisitionDate(_a).CompareTo(GetAcquisitionDate(_b));
            return dateComparison != 0 ? dateComparison : _a.transform.GetSiblingIndex().CompareTo(_b.transform.GetSiblingIndex());
        });

        for (int i = 0; i < m_items.Count; i++)
            m_items[i].transform.SetSiblingIndex(i);
    }

    private long GetAcquisitionDate ( ComponentDisplay _display )
    {
        if (_display == null || _display.SavedData == null)
            return long.MaxValue;

        return _display.SavedData.acquisitionDateTicks > 0 ? _display.SavedData.acquisitionDateTicks : long.MaxValue - 1;
    }

    public void RefreshPredicate ( Func<GameDatas.PlayerSave.Component, bool> _newPredicate )
	{
        m_predicate = _newPredicate;

        foreach (ComponentDisplay display in m_items.ToArray())
		{
            if(m_predicate != null && display != null && display.SavedData != null && !m_predicate(display.SavedData))
			{
                m_items.Remove(display);
                Destroy(display.gameObject);
			}
		}

        //eq.ID, not eq: comparing a string against a Component resolves to object.Equals(object, object), which
        //is always false, so the "already displayed" guard never matched and every refresh re-created the whole
        //inventory on top of the displays already there.
        foreach (GameDatas.PlayerSave.Component eq in GameDatas.current.currentPlayerSave.equipmentInventory)
            if (m_predicate != null && m_predicate(eq) && !m_items.Any(item => string.Equals(item.SavedData.ID, eq.ID)))
                CreateNewDisplay(m_unitData, eq, m_displayMode);
    }


    public void Cleanup ()
	{
        foreach (ComponentDisplay display in m_items)
            Destroy(display.gameObject);

        m_items.Clear();
        m_capacity = 0;
    }

    public override ComponentDisplay FindDisplay ( GameDatas.PlayerSave.Component _component )
    {
        return _component == null ? null : m_items.Find(item => item != null && item.SavedData == _component);
    }

    public override void Reveal ( ComponentDisplay _display )
    {
        ScrollRect scroll = m_displayParent != null ? m_displayParent.GetComponentInParent<ScrollRect>() : null;
        if (scroll == null || scroll.content == null || _display == null || !m_items.Contains(_display))
            return;

        RectTransform content = scroll.content;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, _display.transform);
        Rect view = viewport.rect;
        float offset = bounds.max.y > view.yMax ? bounds.max.y - view.yMax + m_revealMargin
            : bounds.min.y < view.yMin ? bounds.min.y - view.yMin - m_revealMargin : 0f;
        if (Mathf.Approximately(offset, 0f))
            return;

        scroll.StopMovement();
        m_revealTween?.Kill();
        m_revealTween = content.DOAnchorPosY(content.anchoredPosition.y - offset, ComponentGestureFeedback.RevealDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    public override void ReturnDisplay ( ComponentDisplay _display )
    {
        _display.transform.SetParent(m_displayParent);
        _display.transform.localPosition = Vector3.zero;
        SortByAcquisitionDate();
    }

	#region DnD

	public override ComponentGesture.Refusal GetRefusal ( ComponentDisplay _display )
    {
        return m_items.Contains(_display) ? ComponentGesture.Refusal.Silent : base.GetRefusal(_display);
    }

    protected override ComponentGesture.Kind GetIncomingKind ( ComponentDisplay _display )
    {
        return m_displayMode switch
        {
            ComponentDisplay.DisplayMode.ShopBuying => ComponentGesture.Kind.Sell,
            ComponentDisplay.DisplayMode.ShopSelling => ComponentGesture.Kind.Buy,
            ComponentDisplay.DisplayMode.RecyclingStation => ComponentGesture.Kind.TakeBack,
            _ => ComponentGesture.Kind.Remove
        };
    }

    public override void RegisterInteraction ( ComponentDisplay _component )
    {
        m_items.Add(_component);
        _component.CurrentContainer = this;

        _component.transform.SetParent(m_displayParent);
        _component.transform.localPosition = Vector3.zero;
        RefreshMainComponentHighlight(_component);
        base.RegisterInteraction(_component);

        if (!m_items.Contains(_component))
            return;

        ConsumePadding();
        SortByAcquisitionDate();
    }

    private void ConsumePadding ()
    {
        if (m_items.Count <= m_capacity)
            return;

        ComponentDisplay empty = m_items.FindLast(item => item != null && item.SavedData == null);
        if (empty == null)
            return;

        m_items.Remove(empty);
        Destroy(empty.gameObject);
    }

	public override void RemoveDisplay ( ComponentDisplay _display )
	{
        if (m_items.Remove(_display))
        {
            _display.SetTutorialHighlighted(false);
            base.RemoveDisplay(_display);

            if (m_items.Count < m_capacity)
                CreateNewDisplay(null, null, ComponentDisplay.DisplayMode.Empty);
        }
	}

	#endregion

}
