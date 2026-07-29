using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DamagedSlotDisplay : MonoBehaviour
{
    [SerializeField] private Image m_icon;
    [SerializeField] private GameObject m_damagedGO;

	public void Init( Sprite _icon, bool _isDamaged )
	{
        m_icon.sprite = _icon;
        m_damagedGO.SetActive(_isDamaged);
        gameObject.SetActive(true);
    }

    public void Hide ()
	{
        gameObject.SetActive(false);
	}

}
