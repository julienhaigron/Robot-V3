using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	private static readonly HashSet<Billboard> m_activeBillboards = new();
	public static IReadOnlyCollection<Billboard> ActiveBillboards => m_activeBillboards;

	[SerializeField] private bool m_updateAtRuntime;
	[SerializeField] private bool m_updateOnEnable = true;

	public bool UpdateAtRuntime => m_updateAtRuntime;

	private void OnEnable ()
	{
		m_activeBillboards.Add(this);

		if (m_updateOnEnable || m_updateAtRuntime)
			SetRot();
	}

	private void OnDisable ()
	{
		m_activeBillboards.Remove(this);
	}

	[Button]
	public void SetRot ()
	{
		transform.rotation = BillboardManager.TargetRot;
	}
}
