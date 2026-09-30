using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DamagedSlotDisplay : MonoBehaviour
{
    [SerializeField] private Image m_icon;

	public void Init( Sprite _icon, bool _isDamaged, bool _hasSubDamaged )
	{
        m_icon.sprite = _icon;
        m_icon.color = _isDamaged ? Color.red : _hasSubDamaged ? Color.yellow : Color.white;
        gameObject.SetActive(true);
    }

    public void Hide ()
	{
        gameObject.SetActive(false);
	}

}
