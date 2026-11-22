using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeywordBox : MonoBehaviour
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private GameObject m_tagRoot;
	[SerializeField] private Image m_tagFrame;
	[SerializeField] private TextMeshProUGUI m_tagTMP;
	[SerializeField] private TextMeshProUGUI m_textTMP;
	[SerializeField] private HexZoneGraphic m_zone;
	[SerializeField] private TextMeshProUGUI m_noteTMP;

	public void Show ( Sprite _icon, Color _color, string _title, string _tag, string _text, string _note, ActionKeyword? _zone )
	{
		gameObject.SetActive(true);

		m_icon.sprite = _icon;
		m_icon.color = _color;
		m_icon.enabled = _icon != null;
		m_titleTMP.text = _title;
		m_titleTMP.color = _color;

		m_tagRoot.SetActive(!string.IsNullOrEmpty(_tag));
		m_tagTMP.text = _tag;
		m_tagTMP.color = _color;
		m_tagFrame.color = new Color(_color.r, _color.g, _color.b, m_tagFrame.color.a);

		m_textTMP.text = _text;

		m_zone.gameObject.SetActive(_zone.HasValue);
		if (_zone.HasValue)
			m_zone.SetZone(_zone.Value);

		m_noteTMP.gameObject.SetActive(!string.IsNullOrEmpty(_note));
		m_noteTMP.text = _note;
	}
}
