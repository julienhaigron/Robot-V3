using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class WeaponCone : MonoBehaviour
{

	[SerializeField] protected GameObject m_conesParent;
	[SerializeField] protected GameObject m_activeCone;
	[SerializeField] protected GameObject m_unactiveCone;

	protected float m_aimedRotation = 0;
	public float AimedRotation => m_aimedRotation;

	protected WeaponEquipmentData m_data;
	public WeaponEquipmentData Data => m_data;

	public virtual void Init ( Entity _user, WeaponEquipmentData _data, bool _isFirstSide )
	{
		m_data = _data;
		AimAtAngle(_isFirstSide ? 90 : -90f, true, null);

		//transform.localScale = _data.range * Vector3.one;
		DisableAllCones();
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

	public void DisableAllCones ()
	{
		m_activeCone.SetActive(false);
		m_unactiveCone.SetActive(false);
	}
}
