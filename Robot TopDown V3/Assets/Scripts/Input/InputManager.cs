using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

	private Vector3 m_mousePosition;


	public void OnInteract ( InputAction.CallbackContext context )
	{
		if (context.started == false)
			return;

		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.game.input.interactionRayCastLength, GameConfig.current.game.input.interactionRayCastLayer))
		{
			if (hitInfo.transform.TryGetComponent(out Tile tile))
			{
				GameManager.Instance.Grid.onTileSelected?.Invoke(tile);
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
			if (hitInfo.transform.TryGetComponent(out Tile tile))
			{
				GameManager.Instance.Grid.onTileHovered?.Invoke(tile);
			}
		}
	}
}
