using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private GameObject m_cameraParent;
    public GameObject CameraParent => m_cameraParent;
    [SerializeField] private Camera m_camera;
    public Camera Camera => m_camera;

    [SerializeField] private Transform m_defaultCameraPosition;


    public void ResetPosition ()
	{
        TeleportCameraTo(m_defaultCameraPosition);
	}

    public void TeleportCameraTo(Transform _to )
	{
        TeleportCameraTo(_to.position, _to.rotation);
    }

    public void TeleportCameraTo(Vector3 _position, Quaternion _rotation )
	{
        m_cameraParent.transform.position = _position;
        m_cameraParent.transform.rotation = _rotation;
    }

}
