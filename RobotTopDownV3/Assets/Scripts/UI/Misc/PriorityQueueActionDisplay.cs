using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class PriorityQueueActionDisplay : MonoBehaviour
{
    [Title("Dependencies")]
    [SerializeField] private Image m_backgroundImg;

    private EntityActionEnumID m_actionEnumID;
    public EntityActionEnumID ActionEnumId => m_actionEnumID;

    public void Init (EntityActionEnumID _actionID)
	{
        m_actionEnumID = _actionID;
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
