using UnityEngine;

public class BillboardManager : MonoBehaviour
{
	private Quaternion m_lastTargetRot;

	public static Quaternion TargetRot
	{
		get
		{
			Camera camera = CameraManager.Instance != null ? CameraManager.Instance.Camera : Camera.main;

			return camera != null ? camera.transform.rotation : Quaternion.identity;
		}
	}

	private void OnEnable ()
	{
		m_lastTargetRot = TargetRot;
		UpdateAllBillboards();
	}

	private void LateUpdate ()
	{
		Quaternion targetRot = TargetRot;
		bool rotChanged = targetRot != m_lastTargetRot;
		m_lastTargetRot = targetRot;

		if (rotChanged)
		{
			UpdateAllBillboards();
			return;
		}

		foreach (Billboard billboard in Billboard.ActiveBillboards)
		{
			if (billboard.UpdateAtRuntime)
				billboard.SetRot();
		}
	}

	public static void UpdateAllBillboards ()
	{
		foreach (Billboard billboard in Billboard.ActiveBillboards)
			billboard.SetRot();
	}
}
