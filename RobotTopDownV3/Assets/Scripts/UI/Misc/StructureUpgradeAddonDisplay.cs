using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureUpgradeAddonDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_title;
    [SerializeField] private TextMeshProUGUI m_beforeValueTMP;
    [SerializeField] private TextMeshProUGUI m_afterValueTMP;

    public void Init ( string _content, string _previousValue, string _difference )
    {
		m_title.text = _content;
        m_beforeValueTMP.text = _previousValue;
        m_afterValueTMP.text = _previousValue + "<color=green><size=75%>+" + _difference + "</size></color>";
    }
}
