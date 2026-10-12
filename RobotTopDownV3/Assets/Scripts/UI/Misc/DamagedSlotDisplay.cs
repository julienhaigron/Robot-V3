using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DamagedSlotDisplay : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Image m_icon;

    public System.Action<GameDatas.PlayerSave.Component> onHovered;

    private GameDatas.PlayerSave.Component m_component;

	public void Init( Sprite _icon, bool _isDamaged, bool _hasSubDamaged, GameDatas.PlayerSave.Component _component = null )
	{
        m_component = _component;
        m_icon.sprite = _icon;
        m_icon.color = _isDamaged ? Color.red : _hasSubDamaged ? Color.yellow : Color.white;
        gameObject.SetActive(true);
    }

    public void Hide ()
	{
        m_component = null;
        gameObject.SetActive(false);
	}

    public void OnPointerEnter ( PointerEventData _eventData )
    {
        if (m_component != null)
            onHovered?.Invoke(m_component);
    }

}
