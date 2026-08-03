using UnityEngine;
using TMPro;

public class LogPoolElement : PoolElement
{
    [SerializeField] private TextMeshProUGUI m_text;

    public void Init(string _content )
	{
		m_text.text = _content;
	}
}
