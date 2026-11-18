using UnityEngine;

public class HangarEnvironment : MonoBehaviour
{
	[SerializeField] private Transform m_squadParent;
	[SerializeField] private Transform m_cameraPoint;

	public Transform SquadParent => m_squadParent != null ? m_squadParent : transform;
	public Transform CameraPoint => m_cameraPoint != null ? m_cameraPoint : transform;

	public void SetShown ( bool _isShown )
	{
		gameObject.SetActive(_isShown);
	}
}
