using UnityEngine;

[RequireComponent(typeof(Light))]
public class CameraRelativeLight : MonoBehaviour
{
	[SerializeField] private float m_pitch = 30f;
	[SerializeField] private float m_yawFromCamera = 180f;

	private void LateUpdate ()
	{
		Camera camera = CameraManager.Instance != null ? CameraManager.Instance.Camera : null;
		if (camera == null)
			return;

		transform.rotation = Quaternion.Euler(m_pitch, camera.transform.eulerAngles.y + m_yawFromCamera, 0f);
	}
}
