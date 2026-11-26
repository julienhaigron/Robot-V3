using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PressMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] private Transform m_target;
	[SerializeField] private Selectable m_selectable;

	private bool m_isHovered;
	private bool m_isPressed;
	private bool m_isScaled;
	private Tween m_tween;

	private void OnDisable ()
	{
		m_isHovered = false;
		m_isPressed = false;
		if (!m_isScaled)
			return;

		m_tween?.Kill();
		m_target.localScale = Vector3.one;
		m_isScaled = false;
	}

	public void OnPointerEnter ( PointerEventData _eventData )
	{
		m_isHovered = true;
		Refresh();
	}

	public void OnPointerExit ( PointerEventData _eventData )
	{
		m_isHovered = false;
		m_isPressed = false;
		Refresh();
	}

	public void OnPointerDown ( PointerEventData _eventData )
	{
		m_isPressed = true;
		Refresh();
	}

	public void OnPointerUp ( PointerEventData _eventData )
	{
		m_isPressed = false;
		Refresh();
	}

	private void Refresh ()
	{
		GameConfig.PanelMotion settings = UIMotion.Settings;
		bool isInteractable = m_selectable == null || m_selectable.IsInteractable();
		float scale = !isInteractable ? 1f : m_isPressed ? settings.pressScale : m_isHovered ? settings.buttonHoverScale : 1f;
		if (!m_isScaled && Mathf.Approximately(scale, 1f))
			return;

		m_tween?.Kill();
		m_tween = UIMotion.Scale(m_target, scale, m_isPressed ? settings.pressDuration : settings.hoverDuration);
		m_isScaled = !Mathf.Approximately(scale, 1f);
	}
}
