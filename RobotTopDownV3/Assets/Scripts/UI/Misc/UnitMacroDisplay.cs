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
	[SerializeField] private GameObject m_selectedHighlightGO;
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	private Entity m_linkedEntity;

	private void Awake ()
	{
		m_btn.onClick += OnClickSelect;
		PlayerController.onEntitySelected += OnEntitySelected;
	}

	private void OnDestroy ()
	{
		m_btn.onClick -= OnClickSelect;
		PlayerController.onEntitySelected -= OnEntitySelected;
	}

	public void Init ( Entity _entity)
	{
		m_selectedHighlightGO.SetActive(false);
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

	private void OnEntitySelected (int? _entityID)
	{
		if (!gameObject.activeInHierarchy)
			return;

		m_selectedHighlightGO.SetActive(_entityID != null && _entityID.HasValue && _entityID.Value == m_linkedEntity.ID);
	}
}
