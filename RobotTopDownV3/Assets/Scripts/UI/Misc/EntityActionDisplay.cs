using UnityEngine;
using TMPro;

public class EntityActionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_actionTmp;
    [SerializeField] private TextMeshProUGUI m_entityStateTmp;

    public void Init (TurnManager.RecordedAction _recordedAction)
	{
        m_actionTmp.text = GameAssets.current.game.entityActionsData[_recordedAction.type].displayName;
        m_entityStateTmp.text = _recordedAction.entityState.ToString();
    }

    public void Hide ()
	{
        gameObject.SetActive(false);
	}
}
