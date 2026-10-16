using UnityEngine;
using UnityEngine.EventSystems;

public class PointerHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public System.Action<bool> onHoverChanged;

	private bool m_isHovered;
	public bool IsHovered => m_isHovered;

	public void OnPointerEnter ( PointerEventData eventData )
	{
		SetHovered(true);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		SetHovered(false);
	}

	private void OnDisable ()
	{
		SetHovered(false);
	}

	private void SetHovered ( bool _isHovered )
	{
		if (m_isHovered == _isHovered)
			return;

		m_isHovered = _isHovered;
		onHoverChanged?.Invoke(_isHovered);
	}
}
