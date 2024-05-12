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


	public void OnInteract ( InputAction.CallbackContext context )
	{
		if (context.started == false)
			return;

		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.game.input.interactionRayCastLength, GameConfig.current.game.input.interactionRayCastLayer))
		{
			if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
			{
				onTileSelected?.Invoke(tile);
			}
		}
	}

	private void Update ()
	{
		if (m_mousePosition != null && Input.mousePosition == m_mousePosition)
			return;

		m_mousePosition = Input.mousePosition;
		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(m_mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.game.input.interactionRayCastLength, GameConfig.current.game.input.interactionRayCastLayer))
		{
			if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
			{
				onTileHovered?.Invoke(tile);
			}
		}
	}
}
