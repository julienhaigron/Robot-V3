using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
	public static Action<Tile> onTileSelected;
	public static Action<Tile> onTileHovered;

	private Vector3 m_mousePosition;

	private bool m_isLogConsoleOpen = false;

	public void OnInteract ( InputAction.CallbackContext context )
	{
		if (context.started == false)
			return;

		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
		{
			if (hitInfo.transform.gameObject.layer == GameConfig.current.input.uiLayer.value)
				return;

			if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
			{
				onTileSelected?.Invoke(tile);
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
