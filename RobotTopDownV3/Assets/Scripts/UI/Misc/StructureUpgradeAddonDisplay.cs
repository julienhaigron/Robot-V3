using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureUpgradeAddonDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_title;

    public void Init ( string _content )
    {
		m_title.text = _content;
    }
}
