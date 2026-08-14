using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogPoolElement : PoolElement
{
    [SerializeField] private TextMeshProUGUI m_text;
	[SerializeField] private LayoutElement m_layoutElement;
	[SerializeField] private Vector2 m_preferedTextSizePrefreredValues;

	public void Init(string _content )
	{
		m_text.SetText(_content);

        //float preferredHeight = m_preferedTextSizePrefreredValues.y;
        //m_layoutElement.preferredHeight = preferredHeight + 8f;
        m_layoutElement.preferredHeight = m_preferedTextSizePrefreredValues.y;
    }
}
