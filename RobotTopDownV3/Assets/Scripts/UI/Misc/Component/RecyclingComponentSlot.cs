using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RecyclingComponentSlot : ComponentSlot
{
	[SerializeField] private BaseButton m_recycleBtn;

	private void Awake ()
	{
		m_recycleBtn.onClick += OnClickRecycle;
	}

	private void OnClickRecycle ()
	{
		//TODO : move stuff from RecyclePanel here
	}

}
