using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class EntityActionDisplay : MonoBehaviour
{
    [Title("Dependencies")]
    [SerializeField] private Image m_backgroundImg;
    [SerializeField] private Transform m_actionIconParent;
    [SerializeField] private Image m_actionIconImg;
    [SerializeField] private TextMeshProUGUI m_actionTmp;
    [SerializeField] private Image m_mainHachureLeft;
    [SerializeField] private Image m_mainHachureRight;
    [SerializeField] private Image m_preparationHachure;
    [SerializeField] private Image m_cooldownHachure;

    private TurnManager.RecordedAction m_recordedAction;
    public TurnManager.RecordedAction RecordedAction => m_recordedAction;

    [Title("Parameters")]
    [SerializeField] private float m_unitLenght = 135.4f;
    [SerializeField] private float m_offsetLenght = 2.2f;
    [SerializeField] private float m_firstUnitStartOffset = 15f;

    [SerializeField] private float m_iconBaseXPosition = 59.2f;
    [SerializeField] private float m_iconXOffsetPosition = 142f;

    [SerializeField] private float m_baseHatchedWidth = 20f;
    [SerializeField] private float m_greenHatchedOneTickWidth = 135.4f;
    [SerializeField] private float m_redHatchedOneTickWidth = 135.4f;

    public void Init (TurnManager.RecordedAction _recordedAction)
	{
        m_recordedAction = _recordedAction;
        m_actionTmp.text = GameAssets.current.game.entityActionsData[_recordedAction.type].displayName;

        RefreshVisual(m_recordedAction.action.timeAtStart, m_recordedAction.action.TotalDuration, m_recordedAction.action.preparationDuration, m_recordedAction.action.cooldownDuration);

        Show(false);
    }

    [Button]
    private void RefreshVisual(int _timeAtStart, int _totalDuration, int _preparationTime, int _cooldownTime )
	{
        int activeDuration = _totalDuration - _preparationTime - _cooldownTime;
        //bakcground
        Vector2 newSize = (m_backgroundImg.transform as RectTransform).sizeDelta;
        newSize.x = (m_unitLenght * _totalDuration)
            + (_totalDuration > 1 ? (m_offsetLenght * (_totalDuration - 1)) : 0f)
            + (_timeAtStart == 0 || m_recordedAction.action.TimeAtEnd == 10 ? m_firstUnitStartOffset : 0f);
        (m_backgroundImg.transform as RectTransform).sizeDelta = newSize;
        Vector2 newPos = (m_backgroundImg.transform as RectTransform).anchoredPosition;
        newPos.x = (m_unitLenght * _timeAtStart)
            + m_offsetLenght * _timeAtStart
            + (_timeAtStart > 0 ? m_firstUnitStartOffset : 0f);
        (m_backgroundImg.transform as RectTransform).anchoredPosition = newPos;

        //main hachuré
        float leftDuration = _preparationTime + ((float)_totalDuration / 2f);
        float rightDuration = _cooldownTime + ((float)_totalDuration / 2f);
        if (leftDuration > 1)
		{
            Vector2 newIconPos = (m_actionIconImg.transform as RectTransform).anchoredPosition;
            newIconPos.x = m_iconBaseXPosition + (m_iconXOffsetPosition * leftDuration);
            (m_actionIconImg.transform as RectTransform).anchoredPosition = newIconPos;

            Vector2 leftGreenHatchedWidth = (m_mainHachureLeft.transform as RectTransform).sizeDelta;
            leftGreenHatchedWidth.x = m_baseHatchedWidth + (m_greenHatchedOneTickWidth * ((float)_totalDuration / 2f));
            (m_mainHachureLeft.transform as RectTransform).sizeDelta = leftGreenHatchedWidth;

            Vector2 leftRedHatchedWidth = (m_preparationHachure.transform as RectTransform).sizeDelta;
            leftRedHatchedWidth.x = m_baseHatchedWidth + (m_redHatchedOneTickWidth * _preparationTime);
            (m_preparationHachure.transform as RectTransform).sizeDelta = leftRedHatchedWidth;

            Vector2 rightGreenHatchedWidth = (m_mainHachureRight.transform as RectTransform).sizeDelta;
            rightGreenHatchedWidth.x = m_baseHatchedWidth + (m_greenHatchedOneTickWidth * ((float)_totalDuration / 2f));
            (m_mainHachureRight.transform as RectTransform).sizeDelta = rightGreenHatchedWidth;

            Vector2 rightRedHatchedWidth = (m_cooldownHachure.transform as RectTransform).sizeDelta;
            rightRedHatchedWidth.x = m_baseHatchedWidth + (m_redHatchedOneTickWidth * _preparationTime);
            (m_cooldownHachure.transform as RectTransform).sizeDelta = rightRedHatchedWidth;
        }
    }

    public void Show (bool _isInstant)
	{
        gameObject.SetActive(true);
	}
    
    public void Hide ( bool _isInstant )
	{
        gameObject.SetActive(false);
	}
}
