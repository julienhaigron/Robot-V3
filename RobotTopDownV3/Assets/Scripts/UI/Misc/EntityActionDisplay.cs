using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

public class EntityActionDisplay :
	MonoBehaviour,
	IPointerClickHandler,
	IPointerEnterHandler,
	IPointerExitHandler
{
	public static System.Action<EntityActionDisplay, bool> onSelect;
	private static EntityActionDisplay m_selectedDisplay;
	public static EntityActionDisplay SelectedDisplay => m_selectedDisplay;

	[Title("Dependencies")]
	[SerializeField] private Image m_backgroundImg;
	[SerializeField] private GameObject m_actionBackgroundSelectionOutlineGO;
	[SerializeField] private Transform m_actionIconParent;
	[SerializeField] private Transform m_actionIconPivot;
	[SerializeField] private Image m_actionIconImg;
	[SerializeField] private GameObject m_actionIconSelectionOutlineGO;
	[SerializeField] private GameObject m_missingTargetIconGO;
	[SerializeField] private Image m_modActionIconImg;
	[SerializeField] private GameObject m_modActionIconSelectionOutlineGO;
	[SerializeField] private Image m_mainHachureLeft;
	[SerializeField] private Image m_mainHachureRight;
	[SerializeField] private Image m_preparationHachure;
	[SerializeField] private Image m_cooldownHachure;
	[SerializeField] private GameObject m_leftGreenOutline;
	[SerializeField] private GameObject m_leftRedOutline;
	[SerializeField] private Sprite m_hexagoneSprite;
	[SerializeField] private Sprite m_hexagoneAndRectangleSprite;

	private TurnManager.RecordedAction m_recordedAction;
	public TurnManager.RecordedAction RecordedAction => m_recordedAction;

	[Title("Background Parameters")]
	[SerializeField] private float m_unitLenght = 135.4f;
	[SerializeField] private float m_offsetLenght = 2.2f;
	[SerializeField] private float m_firstUnitStartOffset = 15f;
	[SerializeField] private float m_firstUnitStartRectangleShapeOffset = 20f;

	[Title("Icon Parameters")]
	[SerializeField] private float m_iconBaseXPosition = 74.5f;
	[SerializeField] private float m_iconOffsetBaseXPosition = -11f;
	[SerializeField] private float m_iconFirstORLastElemOffset = 7.5f;
	[SerializeField] private float m_iconTimeFactorWidth = 136.87f;

	[Title("Hatched Parameters")]
	[SerializeField] private float m_baseGreenHatchedWidth = 69f;
	[SerializeField] private float m_greenHatchedOneTickWidth = 134.2f;
	[SerializeField] private float m_redHatchedOneTickWidth = 134.2f;
	[SerializeField] private float m_greenHatchedOneActivationAndHasCooldownOrPreparationOffset = 9.4f;

	private bool m_hasModActionSelected = false;
	private bool m_isLeftAngleRectangle;

	public void Init ( TurnManager.RecordedAction _recordedAction, bool _isLeftAngleRectangle )
	{
		m_hasModActionSelected = false;
		m_recordedAction = _recordedAction;
		m_isLeftAngleRectangle = _isLeftAngleRectangle;

		m_actionIconImg.sprite = _recordedAction.action.Data.icon;
		m_modActionIconImg.sprite = _recordedAction.freeActionType != EntityActionEnumID.Unknowned && _recordedAction.freeActionType != EntityActionEnumID.Wait
			? _recordedAction.freeAction.Data.icon : null;
		RefreshVisual(m_recordedAction.action.timeAtStart, m_recordedAction.action.TotalDuration, m_recordedAction.action.preparationDuration, m_recordedAction.action.cooldownDuration, _isLeftAngleRectangle);

		Show(false);
	}


	#region Interractions

	public void OnPointerClick ( PointerEventData eventData )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording || !gameObject.activeInHierarchy)
			return;

		bool clickedModAction = eventData.pointerPressRaycast.gameObject == m_modActionIconImg.gameObject;

		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (clickedModAction)
			{
				m_recordedAction.freeAction = null;
				m_recordedAction.freeActionType = EntityActionEnumID.Wait;

				RefreshVisual(m_recordedAction.action.timeAtStart, m_recordedAction.action.TotalDuration, m_recordedAction.action.preparationDuration, m_recordedAction.action.cooldownDuration, m_isLeftAngleRectangle);
				//TurnManager.Instance.RefreshActionDisplay(m_recordedAction.action.performingEntityID, true);
			}
			else
			{
				Deselect();

				//TODO: confirmation window?
				int posInQueue = 0;
				foreach (TurnManager.RecordedAction recordedAction in TurnManager.Instance.RecordedActions[m_recordedAction.action.performingEntityID])
				{
					if (m_recordedAction == recordedAction)
						TurnManager.Instance.RemoveActionFrom(recordedAction, posInQueue);
					else
						posInQueue++;
				}
				return;
			}
		}

		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (m_selectedDisplay == this && (clickedModAction == m_hasModActionSelected))
		{
			Deselect();
			return;
		}

		if (clickedModAction)
			SelectModAction();
		else
			SelectAction();
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		if (m_recordedAction == null || !gameObject.activeInHierarchy)
			return;

		EntityActionData data = m_recordedAction.action.Data;
		ToolTipManager.Instance.Show(data.displayName, data.GetDescription());

		if (m_selectedDisplay == this || TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		TurnManager.Instance.RefreshActionDisplay(m_recordedAction.action.performingEntityID, false, m_recordedAction.action.TimeAtEnd);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		ToolTipManager.Instance.Hide();

		if (m_selectedDisplay == this || m_recordedAction == null || TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording || !gameObject.activeInHierarchy)
			return;
		else if (m_selectedDisplay == null)
			TurnManager.Instance.RefreshActionDisplay(m_recordedAction.action.performingEntityID, false, TurnManager.Instance.RecordedActions[m_recordedAction.action.performingEntityID].ToArray()[^1].action.TimeAtEnd);
		else if (m_selectedDisplay != this)
			TurnManager.Instance.RefreshActionDisplay(m_selectedDisplay.RecordedAction.action.performingEntityID, false, m_selectedDisplay.RecordedAction.action.TimeAtEnd);
	}

	#endregion

	public void SelectAction ()
	{
		if (m_selectedDisplay != null)
			m_selectedDisplay.Deselect();

		m_hasModActionSelected = false;
		m_selectedDisplay = this;
		onSelect?.Invoke(this, false);
		RefreshSelectionOutlines();
	}

	public void SelectModAction ()
	{
		if (m_selectedDisplay != null)
			m_selectedDisplay.Deselect();

		m_hasModActionSelected = true;
		m_selectedDisplay = this;
		onSelect?.Invoke(this, true);
		RefreshSelectionOutlines();
	}

	public void Deselect ()
	{
		if (m_selectedDisplay != this)
			return;

		if (m_selectedDisplay == this)
			m_selectedDisplay = null;

		m_hasModActionSelected = false;
		onSelect?.Invoke(null, false);
		RefreshSelectionOutlines();
	}

	[Button]
	public void RefreshVisual ( int _timeAtStart, int _totalDuration, int _preparationTime, int _cooldownTime, bool _isLeftAngleRectangle )
	{
		m_backgroundImg.sprite = _isLeftAngleRectangle ? m_hexagoneAndRectangleSprite : m_hexagoneSprite;
		m_leftGreenOutline.SetActive(!_isLeftAngleRectangle || _preparationTime > 0);
		m_leftRedOutline.SetActive(!_isLeftAngleRectangle);

		//background
		Vector2 newSize = (m_backgroundImg.transform as RectTransform).sizeDelta;
		newSize.x = (m_unitLenght * _totalDuration)
			+ (_totalDuration > 1 ? (m_offsetLenght * (_totalDuration - 1)) : 0f)
			+ (_timeAtStart == 0 || (_timeAtStart + _totalDuration == 10) ? m_firstUnitStartOffset : 0f)
			- (_isLeftAngleRectangle && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset : 0f);
		(m_backgroundImg.transform as RectTransform).sizeDelta = newSize;
		Vector2 newPos = (m_backgroundImg.transform as RectTransform).anchoredPosition;
		newPos.x = (m_unitLenght * _timeAtStart)
			+ m_offsetLenght * _timeAtStart
			+ (_timeAtStart > 0 ? m_firstUnitStartOffset : 0f)
			+ (_isLeftAngleRectangle && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset : 0f);
		(m_backgroundImg.transform as RectTransform).anchoredPosition = newPos;

		float activeDuration = _totalDuration - _preparationTime - _cooldownTime;

		//icon pos
		float timeFactor = _preparationTime + ((activeDuration / 2f));
		Vector2 newIconPos = (m_actionIconParent.transform as RectTransform).anchoredPosition;
		newIconPos.x = (activeDuration > 1 || _preparationTime > 0 ? m_iconTimeFactorWidth * timeFactor : m_iconBaseXPosition)
			- (_isLeftAngleRectangle && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset / 2f : 0f);
		(m_actionIconParent as RectTransform).anchoredPosition = newIconPos;

		//icon offset
		Vector2 newIconPos2 = (m_actionIconPivot.transform as RectTransform).anchoredPosition;
		newIconPos2.x = m_iconOffsetBaseXPosition
			- (_timeAtStart != 0 && (_timeAtStart + _totalDuration != 10) ? m_iconFirstORLastElemOffset : 0f);
		(m_actionIconPivot as RectTransform).anchoredPosition = newIconPos2;

		//hachur�
		float greenHatchedWidth = (activeDuration > 1 ? (m_greenHatchedOneTickWidth * ((float)activeDuration / 2f)) : m_baseGreenHatchedWidth);

		Vector2 leftGreenHatchedWidth = (m_mainHachureLeft.transform as RectTransform).sizeDelta;
		leftGreenHatchedWidth.x = greenHatchedWidth
			- (_isLeftAngleRectangle && _preparationTime == 0 && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset / 2f : 0f);
		/*+ (activeDuration == 1 && _preparationTime > 0 ? m_greenHatchedOneActivationAndHasCooldownOrPreparationOffset : 0f);*/
		(m_mainHachureLeft.transform as RectTransform).sizeDelta = leftGreenHatchedWidth;

		if (_preparationTime > 0)
		{
			m_preparationHachure.gameObject.SetActive(true);
			Vector2 leftRedHatchedWidth = (m_preparationHachure.transform as RectTransform).sizeDelta;
			leftRedHatchedWidth.x = m_redHatchedOneTickWidth * _preparationTime
				- (_isLeftAngleRectangle && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset / 2f : 0f)
				/*- (activeDuration == 1 && _preparationTime > 0 ? m_greenHatchedOneActivationAndHasCooldownOrPreparationOffset : 0f)*/;
			(m_preparationHachure.transform as RectTransform).sizeDelta = leftRedHatchedWidth;
		}
		else
			m_preparationHachure.gameObject.SetActive(false);

		m_mainHachureRight.gameObject.SetActive(true);
		Vector2 rightGreenHatchedWidth = (m_mainHachureRight.transform as RectTransform).sizeDelta;
		rightGreenHatchedWidth.x = greenHatchedWidth
			- (_timeAtStart != 0 && (_timeAtStart + _totalDuration != 10) && activeDuration == 1 && _cooldownTime == 0 ? m_firstUnitStartOffset : 0f)
			- (_isLeftAngleRectangle && _cooldownTime == 0 && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset / 2f : 0f);
		//+ (activeDuration == 1 && _cooldownTime > 0 ? m_greenHatchedOneActivationAndHasCooldownOrPreparationOffset : 0f);
		(m_mainHachureRight.transform as RectTransform).sizeDelta = rightGreenHatchedWidth;

		if (_cooldownTime > 0)
		{
			m_cooldownHachure.gameObject.SetActive(true);
			Vector2 rightRedHatchedWidth = (m_cooldownHachure.transform as RectTransform).sizeDelta;
			rightRedHatchedWidth.x = m_redHatchedOneTickWidth * _cooldownTime
				+ (activeDuration == 1 && _cooldownTime > 0 ? m_greenHatchedOneActivationAndHasCooldownOrPreparationOffset : 0f)
				- (_timeAtStart != 0 && (_timeAtStart + _totalDuration != 10) && activeDuration == 1 && _cooldownTime > 0 ? m_firstUnitStartOffset : 0f)
				- (_isLeftAngleRectangle && _timeAtStart == 0 ? m_firstUnitStartRectangleShapeOffset / 2f : 0f);
			(m_cooldownHachure.transform as RectTransform).sizeDelta = rightRedHatchedWidth;
		}
		else
			m_cooldownHachure.gameObject.SetActive(false);


		RefreshSelectionOutlines();
		RefreshMissingTargetWarning();
	}

	public void RefreshMissingTargetWarning ()
	{
		if (m_recordedAction == null || m_recordedAction.action == null)
			return;

		bool isMissingTarget = m_recordedAction.action.IsMissingTarget();
		Sprite warningIcon = GameAssets.current.ui.missingTargetIcon;

		if (m_missingTargetIconGO != null)
			m_missingTargetIconGO.SetActive(isMissingTarget);

		m_actionIconImg.sprite = isMissingTarget && warningIcon != null ? warningIcon : m_recordedAction.action.Data.icon;
		m_actionIconImg.color = isMissingTarget ? GameAssets.current.ui.missingTargetColor : Color.white;
	}

	private void RefreshSelectionOutlines ()
	{
		//m_actionBackgroundSelectionOutlineGO.SetActive(m_isSelected);
		m_actionIconSelectionOutlineGO.SetActive(m_selectedDisplay == this && !TurnManager.Instance.hasModActionSelected);
		m_modActionIconSelectionOutlineGO.SetActive(m_selectedDisplay == this && TurnManager.Instance.hasModActionSelected);
	}

	public void Show ( bool _isInstant )
	{
		gameObject.SetActive(true);
	}

	public void Hide ( bool _isInstant )
	{
		gameObject.SetActive(false);
	}
}
