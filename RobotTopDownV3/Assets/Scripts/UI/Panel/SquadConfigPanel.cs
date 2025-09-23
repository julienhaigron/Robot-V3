using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SquadConfigPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloCampainPanel>();
	}
}
