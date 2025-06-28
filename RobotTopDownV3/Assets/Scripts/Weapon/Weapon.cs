using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class Weapon : MonoBehaviour
{

	private WeaponData m_data;
	public WeaponData Data => m_data;

	[SerializeField] private GameObject m_conesParent;
	[SerializeField] private GameObject m_activeCone;
	[SerializeField] private GameObject m_unactiveCone;

	private float m_aimedRotation = 0;
	public float AimedRotation => m_aimedRotation;

	public void Init ( WeaponData _data )
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

	public void AimAtAngle ( float _angle, bool _isInstant, Action _onComplete )
	{
		Quaternion rot = Quaternion.AngleAxis(_angle, Vector3.forward);
		m_aimedRotation = _angle;
		if (_isInstant)
		{
			m_activeCone.transform.localRotation = rot;
			m_unactiveCone.transform.localRotation = rot;
			_onComplete?.Invoke();
		}
		else
		{
			m_activeCone.transform.DOLocalRotateQuaternion(rot, 1.5f);
			m_unactiveCone.transform.DOLocalRotateQuaternion(rot, 1.5f).OnComplete(() => _onComplete?.Invoke());
		}
	}

	public void DisableAllCones ()
	{
		m_activeCone.SetActive(false);
		m_unactiveCone.SetActive(false);
	}
}
