using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Sirenix.OdinInspector;

public class UnitMacroDisplay : MonoBehaviour
{
	[SerializeField] private BaseButton m_btn;
	[SerializeField] private Image m_iconImg;
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	private Entity m_linkedEntity;

	private void Awake ()
	{
		m_btn.onClick += OnClickSelect;
	}

	private void OnDestroy ()
	{
		m_btn.onClick -= OnClickSelect;
	}

	public void Init ( Entity _entity)
	{
		m_iconImg.sprite = _entity.Data.FrameData.icon;
		m_linkedEntity = _entity;
	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}

	private void OnClickSelect ()
	{
		PlayerController.Instance.SelectEntity(m_linkedEntity);
	}
}
