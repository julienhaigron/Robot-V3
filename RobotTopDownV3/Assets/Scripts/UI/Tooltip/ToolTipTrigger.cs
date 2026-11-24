using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea, SerializeField] string m_tooltipTitle;
    [TextArea, SerializeField] string m_tooltipDescription;

    private Sprite m_icon;
    private Color? m_accent;

    public void Init ( string _title, string _description, Sprite _icon = null, Color? _accent = null )
	{
        m_tooltipTitle = _title;
        m_tooltipDescription = _description;
        m_icon = _icon;
        m_accent = _accent;
    }

    public void OnPointerEnter ( PointerEventData eventData )
    {
        ToolTipManager.Instance.Show(m_tooltipTitle, m_tooltipDescription, gameObject, m_icon, m_accent);
    }

    public void OnPointerExit ( PointerEventData eventData )
    {
        ToolTipManager.Instance.Hide();
    }
}
