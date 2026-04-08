using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea, SerializeField] string m_tooltipTitle;
    [TextArea, SerializeField] string m_tooltipDescription;

    public void OnPointerEnter ( PointerEventData eventData )
    {
        ToolTipManager.Instance.Show(m_tooltipTitle, m_tooltipDescription);
    }

    public void OnPointerExit ( PointerEventData eventData )
    {
        ToolTipManager.Instance.Hide();
    }
}