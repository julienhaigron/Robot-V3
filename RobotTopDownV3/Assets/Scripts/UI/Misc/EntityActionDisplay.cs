using UnityEngine;
using TMPro;

public class EntityActionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_actionTmp;
    [SerializeField] private TextMeshProUGUI m_entityStateTmp;

    private TurnManager.RecordedAction m_recordedAction;
    public TurnManager.RecordedAction RecordedAction => m_recordedAction;

    public void Init (TurnManager.RecordedAction _recordedAction)
	{
        m_recordedAction = _recordedAction;
        m_actionTmp.text = GameAssets.current.game.entityActionsData[_recordedAction.type].displayName;
        m_entityStateTmp.text = _recordedAction.entityState.ToString();

        //change size depending on cost
        Show(false);
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
