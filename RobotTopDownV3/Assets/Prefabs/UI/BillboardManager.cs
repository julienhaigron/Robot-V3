using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardManager : MonoBehaviour
{
	private HashSet<Billboard> m_currentBillboards = new();

	private static Quaternion m_targetRot;
	public static Quaternion TargetRot
	{
		get
		{
			if (m_targetRot.eulerAngles == Vector3.zero)
				m_targetRot = Camera.main.transform.rotation;

			return m_targetRot;
		}
	}

	private void OnEnable ()
	{
		Billboard.onBillboardEnable += OnBillboardEnable;
		Billboard.onBillboardDisable += OnBillboardDisable;
	}

	private void OnDestroy ()
	{
		Billboard.onBillboardEnable -= OnBillboardEnable;
		Billboard.onBillboardDisable -= OnBillboardDisable;
	}

	private void OnBillboardDisable ( Billboard _billboard )
	{
		m_currentBillboards.Remove(_billboard);
	}

	private void OnBillboardEnable ( Billboard _billboard )
	{
		m_currentBillboards.Add(_billboard);
	}

	private void Update ()
	{
		foreach (Billboard billboard in m_currentBillboards)
		{
			billboard.SetRot();
		}
	}
}