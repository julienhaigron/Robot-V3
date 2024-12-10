using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	public static Action<Billboard> onBillboardEnable;
	public static Action<Billboard> onBillboardDisable;

	[SerializeField] private bool m_updateAtRuntime;
	[SerializeField] private bool m_updateOnEnable = true;

	private void OnEnable ()
	{
		if (m_updateOnEnable)
			SetRot();
		else if (m_updateAtRuntime)
			onBillboardEnable?.Invoke(this);
	}

	private void OnDisable ()
	{
		if (m_updateAtRuntime)
			onBillboardDisable?.Invoke(this);
	}

	[Button]
	public void SetRot ()
	{
		transform.rotation = BillboardManager.TargetRot;
	}
}