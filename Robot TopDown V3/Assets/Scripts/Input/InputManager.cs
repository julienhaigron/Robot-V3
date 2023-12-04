using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{


	private void OnMouseUp ()
	{
		Ray ray = CameraManager.Instance.Camera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.game.input.interactionRayCastLength, GameConfig.current.game.input.interactionRayCastLayer))
		{
			if (hitInfo.transform.TryGetComponent(out Tile interractable))
			{
				interractable.
			}
		}
	}
}
