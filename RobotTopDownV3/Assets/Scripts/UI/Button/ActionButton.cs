using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ActionButton : BaseButton, IPointerExitHandler
{
	protected static ActionButton m_selectedBtn;

	[SerializeField] protected Image m_icon;
	/*[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_tokenCost;*/
	//[SerializeField] protected BaseButton m_modActionBtn;
	[SerializeField] protected GameObject m_selectionOutline;
	[SerializeField] private TutorialHighlightZone m_zone;
	[SerializeField] private GameObject m_invocationCountRoot;
	[SerializeField] private TextMeshProUGUI m_invocationCountTMP;
	[SerializeField] private Color m_invocationCountColor = Color.white;
	[SerializeField] private Color m_invocationPlannedColor = new(.45f, .82f, 1f, 1f);
	[SerializeField] private Color m_invocationFullColor = new(1f, .42f, .35f, 1f);

	protected EntityActionEnumID m_actionType;
	public EntityActionEnumID ActionEnumID => m_actionType;
	public string HighlightZoneID => m_zone == null ? null : m_zone.ID;

	protected string m_linkedEquipmentData;
	private bool m_isOnlyVisual = false;

	protected void Awake ()
	{
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionRemoved += OnActionRemoved;
		TurnManager.onActionSelected += OnSelectAction;
		EntityActionDisplay.onSelect += OnEntityActionDisplaySelected;
	}

	protected void OnDestroy ()
	{
		TurnManager.onActionAdded -= OnActionAdded;
		TurnManager.onActionRemoved -= OnActionRemoved;
		TurnManager.onActionSelected -= OnSelectAction;
		EntityActionDisplay.onSelect -= OnEntityActionDisplaySelected;
	}

	public void Init( EntityActionEnumID _action, string _linkedEquipmentData )
	{
		m_actionType = _action;
		m_linkedEquipmentData = _linkedEquipmentData;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		m_zone.SetId(gameObject.name + "_" + _action);
		/*m_name.text = data.displayName;
		m_tokenCost.text = data.GetTokenTotalCost(null, null, null).ToString();*/
		m_isOnlyVisual = false;
		SyncSelectionWithTurnManager();
		RefreshVisual();
	}

	public void InitEntityConfigPanelMode( EntityActionEnumID _action )
	{
		m_actionType = _action;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		m_isOnlyVisual = true;
		if (m_selectedBtn == this)
			m_selectedBtn = null;
		m_selectionOutline.SetActive(false);
		SetInvocationCountVisible(false);
		SetInteractability(true);
	}

	private void SyncSelectionWithTurnManager ()
	{
		AEntityAction currentAction = TurnManager.Instance == null ? null : TurnManager.Instance.CurrentActionSelected;
		bool isCurrentAction = currentAction != null && currentAction.enumID == m_actionType && EntityActionDisplay.SelectedDisplay == null;

		if (isCurrentAction)
		{
			if (m_selectedBtn != null && m_selectedBtn != this)
				m_selectedBtn.Deselect();
			m_selectedBtn = this;
		}
		else if (m_selectedBtn == this)
			m_selectedBtn = null;
	}

	protected void RefreshInteractability ()
	{
		if (PlayerController.Instance.SelectedEntity == null || m_actionType == EntityActionEnumID.Unknowned)
			return;

		int entityID = PlayerController.Instance.SelectedEntity.ID;
		int timeAtStart = TurnManager.Instance.RecordedActions.ContainsKey(entityID) && TurnManager.Instance.RecordedActions[entityID].Count > 0
			? TurnManager.Instance.RecordedActions[entityID].ToArray()[^1].action.TimeAtEnd : TurnManager.currentTick;

		AEntityAction action = TurnManager.Instance.GetAction(m_actionType, PlayerController.Instance.SelectedEntity.ID, m_linkedEquipmentData, timeAtStart);

		bool hasEnoughToken = TurnManager.Instance.RemainingActionToken.TryGetValue(entityID, out int remainingToken)
			&& remainingToken >= action.Data.GetTokenTotalCost(action, PlayerController.Instance.SelectedEntity, null);
		bool canInvoke = action.Data.CanInvokeMoreItems(PlayerController.Instance.SelectedEntity, m_linkedEquipmentData);

		RefreshInvocationCount(action.Data, PlayerController.Instance.SelectedEntity);
		SetInteractability(hasEnoughToken && canInvoke && Condition.UseConditionPredicate(action, PlayerController.Instance.SelectedEntity, null, action.Data.conditionType));
	}

	private void RefreshInvocationCount ( EntityActionData _data, Entity _entity )
	{
		int max = _data.invocationCountLimit;
		if (m_invocationCountTMP == null || max <= 0 || !_data.TryGetInvocationTool(_entity, m_linkedEquipmentData, out ToolEquipment tool))
		{
			SetInvocationCountVisible(false);
			return;
		}

		int placed = 0;
		int planned = 0;
		if (_entity.Equipment.ItemsLinkedDataDictionary.TryGetValue(tool.ID, out AItemLinkedData linkedData))
		{
			placed = linkedData.PlacedInvocationCount;
			planned = linkedData.plannedInvocationCount;
		}

		string plannedText = planned > 0 ? "<color=#" + ColorUtility.ToHtmlStringRGB(m_invocationPlannedColor) + ">+" + planned + "</color>" : string.Empty;
		m_invocationCountTMP.text = placed + plannedText + "/" + max;
		m_invocationCountTMP.color = placed + planned >= max ? m_invocationFullColor : m_invocationCountColor;
		SetInvocationCountVisible(true);
	}

	private void SetInvocationCountVisible ( bool _isVisible )
	{
		if (m_invocationCountRoot != null && m_invocationCountRoot.activeSelf != _isVisible)
			m_invocationCountRoot.SetActive(_isVisible);
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

	private void OnActionRemoved ( TurnManager.RecordedAction _removedAction )
	{
		RefreshVisual();
	}

	private void OnSelectAction (AEntityAction _action)
	{
		if (m_isOnlyVisual || !gameObject.activeInHierarchy || _action == null)
			return;

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
		SelectActionInTurnManager();
		if (TurnManager.Instance.TryRegisterActionWithoutTarget())
			DisplayRangePreview(GameAssets.current.game.entityActionsData[m_actionType]);

		base.OnClick();
	}

	protected virtual void SelectActionInTurnManager ()
	{
		TurnManager.Instance.SetCurrentActionSelected(m_actionType, m_linkedEquipmentData, true);
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

	public override void OnPointerEnter ( PointerEventData eventData )
	{
		base.OnPointerEnter(eventData);

		//GridManager.Instance.ClearTileOutile();
		EntityActionData data = GameAssets.current.game.entityActionsData[m_actionType];
		string description = data.DoesResolveItsOwnTarget()
			? data.GetDescription() + "\n" + LocalizationManager.Instance.Get(LocalizationKey.action_desc_designation_hint)
			: data.GetDescription();
		ToolTipManager.Instance.Show(data.GetLocalizedName(), description, gameObject);

		if (PlayerController.Instance != null)
		{
			PlayerController.Instance.ClearHoverPreviews();
			PlayerController.Instance.SetInteractableOutlinesHidden(true);
		}

		DisplayRangePreview(data);
	}

	public override void OnPointerExit ( PointerEventData eventData )
	{
		if (RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, eventData.position, eventData.enterEventCamera))
			return;

		base.OnPointerExit(eventData);

		ToolTipManager.Instance.Hide();

		if (PlayerController.Instance != null)
		{
			PlayerController.Instance.ClearRangePreviewOutlines();
			PlayerController.Instance.SetInteractableOutlinesHidden(false);
		}
	}

	private void DisplayRangePreview ( EntityActionData _data )
	{
		if (m_isOnlyVisual || PlayerController.Instance == null || PlayerController.Instance.SelectedEntity == null)
			return;

		Entity entity = PlayerController.Instance.SelectedEntity;
		int timeAtStart = TurnManager.Instance.RecordedActions.ContainsKey(entity.ID) && TurnManager.Instance.RecordedActions[entity.ID].Count > 0
			? TurnManager.Instance.RecordedActions[entity.ID].ToArray()[^1].action.TimeAtEnd : TurnManager.currentTick;

		AEntityAction action = TurnManager.Instance.GetAction(m_actionType, entity.ID, m_linkedEquipmentData, timeAtStart);
		if (action == null)
			return;

		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(entity.ID)];
		PlayerController.Instance.SetRangePreviewOutlines(GetTilesInRangeOf(action, entity, from), _data.GetMainActionType());
	}

	private IEnumerable<Tile> GetTilesInRangeOf ( AEntityAction _action, Entity _entity, Tile _from )
	{
		if (_action.Data.GetMainActionType() != EntityActionData.MainActionType.Movement)
			return _entity.Equipment.GetTilesInReach(_action, _from, true, true);

		List<Tile> tilesInRange = new();
		_action.OnSelectActionTileInteractPredicatePrewarm();

		foreach (Tile tile in GridManager.Instance.Tiles)
			if (_action.TileInteractPredicate(tile))
				tilesInRange.Add(tile);

		return tilesInRange;
	}
}
