using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntityStatusDisplay : MonoBehaviour
{
	[SerializeField] private Image m_icon;
	[SerializeField] private Image m_border;
	[SerializeField] private GameObject m_durationRoot;
	[SerializeField] private TextMeshProUGUI m_durationText;

	private bool m_isActive = false;
	public bool IsActive => m_isActive;

	private EntityStatusEnumID m_statusID;
	public EntityStatusEnumID StatusID => m_statusID;

	public void SetStatus ( EntityStatusEnumID _statusID )
	{
		m_statusID = _statusID;

		AEntityStatus status = GameAssets.current.game.entityStatus[_statusID];
		m_icon.sprite = status.icon;
		m_icon.color = status.color;
		m_border.color = status.color;
		m_isActive = true;
		gameObject.SetActive(true);
	}

	public void SetDuration ( int _remaining )
	{
		m_durationRoot.SetActive(_remaining > 0);
		m_durationText.text = _remaining.ToString();
	}

	public void Hide ()
	{
		m_isActive = false;
		gameObject.SetActive(false);
	}
}
