using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{

    private WeaponData m_data;
    public WeaponData Data => m_data;

    [SerializeField] private GameObject m_conesParent;
    [SerializeField] private GameObject m_activeCone;
    [SerializeField] private GameObject m_unactiveCone;

    private float m_aimedRotation = 0;
    public float AimedRotation => m_aimedRotation;
    
    public void Init (WeaponData _data)
	{
        m_data = _data;

        ActivateUnactiveCone();
    }

    public void ActivateActiveCone ()
	{
        m_activeCone.SetActive(true);
        m_unactiveCone.SetActive(false);
    }

    public void ActivateUnactiveCone ()
	{
        m_activeCone.SetActive(false);
        m_unactiveCone.SetActive(true);
    }

    public void AimAtAngle ( float _angle )
    {
        Vector3 currentRot = m_activeCone.transform.localRotation.eulerAngles;
        Vector3 newRot = new Vector3(currentRot.x, currentRot.y, _angle);
        Quaternion rot = Quaternion.Euler(newRot);
        m_conesParent.transform.localRotation = rot;
        m_aimedRotation = _angle;
    }

    public void DisableAllCones ()
	{
        m_activeCone.SetActive(false);
        m_unactiveCone.SetActive(false);
	}
}
