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
		EntityEquipmentPlugin.onAnyEntityDeath += OnAnyEntityDeath;
	}

	private void OnDestroy ()
	{
		m_btn.onClick -= OnClickSelect;
		PlayerController.onEntitySelected -= OnEntitySelected;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnAnyEntityDeath;
	}

	public void Init ( Entity _entity)
	{
		m_selectedHighlightGO.SetActive(false);
		m_iconImg.sprite = _entity.Data.FrameData.icon;
		m_linkedEntity = _entity;
		m_btn.SetInteractability(!_entity.Equipment.IsDead);
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

	private void OnAnyEntityDeath ( Entity _entity )
	{
		if (m_linkedEntity == null || _entity != m_linkedEntity)
			return;

		m_selectedHighlightGO.SetActive(false);
		m_btn.SetInteractability(false);
	}

	private void OnEntitySelected (int? _entityID)
	{
		if (!gameObject.activeInHierarchy)
			return;

		m_selectedHighlightGO.SetActive(_entityID != null && _entityID.HasValue && _entityID.Value == m_linkedEntity.ID);
	}
}
