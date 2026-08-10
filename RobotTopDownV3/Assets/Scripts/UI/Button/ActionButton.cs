using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ActionButton : BaseButton, IPointerEnterHandler, IPointerExitHandler
{
	protected static ActionButton m_selectedBtn;
	public static ActionButton SelectedBtn => m_selectedBtn;

	[SerializeField] protected Image m_icon;
	/*[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_tokenCost;*/
	//[SerializeField] protected BaseButton m_modActionBtn;
	[SerializeField] protected GameObject m_selectionOutline;

	protected EntityActionEnumID m_actionType;
	protected string m_linkedEquipmentData;
	private bool m_isOnlyVisual = false;

	protected void Awake ()
	{
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionSelected += OnSelectAction;
		EntityActionDisplay.onSelect += OnEntityActionDisplaySelected;
	}

	protected void OnDestroy ()
	{
		TurnManager.onActionAdded -= OnActionAdded;
		TurnManager.onActionSelected -= OnSelectAction;
		EntityActionDisplay.onSelect -= OnEntityActionDisplaySelected;
	}

	public void Init( EntityActionEnumID _action, string _linkedEquipmentData )
	{
		m_actionType = _action;
		m_linkedEquipmentData = _linkedEquipmentData;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		/*m_name.text = data.displayName;
		m_tokenCost.text = data.GetTokenTotalCost(null, null, null).ToString();*/
		m_isOnlyVisual = false;
		RefreshVisual();
	}

	public void InitEntityConfigPanelMode( EntityActionEnumID _action )
	{
		m_actionType = _action;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		m_isOnlyVisual = true;
		SetInteractability(true);
	}

	protected void RefreshInteractability ()
	{
		if (PlayerController.Instance.SelectedEntity == null || m_actionType == EntityActionEnumID.Unknowned)
			return;

		int entityID = PlayerController.Instance.SelectedEntity.ID;
		int timeAtStart = TurnManager.Instance.RecordedActions.ContainsKey(entityID) && TurnManager.Instance.RecordedActions[entityID].Count > 0
			? TurnManager.Instance.RecordedActions[entityID].ToArray()[^1].action.TimeAtEnd : TurnManager.currentTick;

		AEntityAction action = TurnManager.Instance.GetAction(m_actionType, PlayerController.Instance.SelectedEntity.ID, m_linkedEquipmentData, timeAtStart);
		SetInteractability(Condition.UseConditionPredicate(action, PlayerController.Instance.SelectedEntity, null, action.Data.conditionType));

	}

	protected void RefreshVisual ()
	{
		RefreshInteractability();

		m_selectionOutline.SetActive(m_selectedBtn == this);
	}

	public override void SetInteractability ( bool _isInteractable )
	{
		base.SetInteractability(_isInteractable);
		m_icon.color = _isInteractable ? Color.white : Color.black;
	}

	private void OnActionAdded (TurnManager.RecordedAction _addedAction)
	{
		//refresh if is available
		RefreshVisual();
	}

	private void OnSelectAction (AEntityAction _action)
	{
		if (m_actionType == _action.enumID)
			Select();
	}

	private void OnEntityActionDisplaySelected ( EntityActionDisplay _display, bool _isModAction)
	{
		if (_display != null)
			Deselect();
	}

	protected override void OnClick ()
	{
		if (m_isOnlyVisual)
			return;

		Select();
		TurnManager.Instance.SetCurrentActionSelected(m_actionType, m_linkedEquipmentData, true);
		base.OnClick();
	}

	public void Select ()
	{
		if (m_selectedBtn != null)
			m_selectedBtn.Deselect();

		m_selectedBtn = this;
		RefreshVisual();
	}

	public void Deselect ()
	{
		if (m_selectedBtn != this)
			return;

		if (m_selectedBtn == this)
			m_selectedBtn = null;

		RefreshVisual();
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		EntityActionData data = GameAssets.current.game.entityActionsData[m_actionType];
		ToolTipManager.Instance.Show(data.displayName, data.GetDescription());
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		ToolTipManager.Instance.Hide();
	}
}
