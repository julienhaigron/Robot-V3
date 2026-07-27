using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusDisplay : MonoBehaviour
{
	[SerializeField] private Image m_iconImg;

	public void Init ( AEntityStatus _status )
	{
		m_iconImg.sprite = _status.icon;
	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}
}
