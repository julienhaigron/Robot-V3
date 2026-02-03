using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
	public static Action<Tile> onTileleftClick;
	public static Action<Tile> onTileRightClick;
	public static Action<Tile> onTileHovered;

	private Vector3 m_mousePosition;

	private bool m_isLogConsoleOpen = false;

	public static bool IsPointerOverBlockingUI ()
	{
		if (EventSystem.current == null)
			return false;

		PointerEventData data = new PointerEventData(EventSystem.current);
		data.position = Mouse.current.position.ReadValue();

		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(data, results);

		foreach (var r in results)
		{
			var graphic = r.gameObject.GetComponent<Graphic>();
			if (graphic != null && graphic.raycastTarget)
				return true;
		}

		return false;
	}


	public void OnInteract ( InputAction.CallbackContext context )
	{
		if (context.started == false)
			return;

		if (IsPointerOverBlockingUI())
			return;

		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
		{

			if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
			{
				if (string.Equals(context.control.name, "leftButton"))
					onTileleftClick?.Invoke(tile);
				else if (string.Equals(context.control.name, "rightButton"))
					onTileRightClick?.Invoke(tile);
			}
		}
	}

	private void Update ()
	{
		if (Input.GetKeyDown(KeyCode.C))
		{
			if (m_isLogConsoleOpen)
				UIManager.Instance.ClosePopup<LogConsolePopup>();
			else
				UIManager.Instance.OpenPopup<LogConsolePopup>();

			m_isLogConsoleOpen = !m_isLogConsoleOpen;
		}


		if (m_mousePosition != null && Input.mousePosition == m_mousePosition)
			return;

		m_mousePosition = Input.mousePosition;
		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(m_mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
		{
			if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
			{
				onTileHovered?.Invoke(tile);
			}
		}

	}
}
