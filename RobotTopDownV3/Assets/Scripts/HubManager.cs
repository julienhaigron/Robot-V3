using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

public class HubManager : Singleton<HubManager>
{
	[Title("Depedencies")]
	[SerializeField] private GameObject m_squadEntitiesParent;
	[SerializeField] private Transform m_hangarCameraPosition;

	[Title("Parameters")]
	[SerializeField] private float m_unitSpacing = 1.5f;

	private List<Entity> m_squadEntities = new();

	private Entity m_selectedEntity;

    public void ShowHangar ()
	{
		CameraManager.Instance.TeleportCameraTo(m_hangarCameraPosition);

		foreach (EntitySavedData entitySavedData in GameDatas.current.currentPlayerSave.squadUnits)
		{
			AddEntity(entitySavedData);
		}
	}

	public void HideHangar ()
	{
		CameraManager.Instance.ResetPosition();

		foreach (Entity entity in m_squadEntities)
		{
			Destroy(entity.gameObject);
		}
		m_squadEntities.Clear();
	}

	public void SelectEntity(Entity _selectedEntity )
	{
		m_selectedEntity = _selectedEntity;

		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(_selectedEntity.Data);

		//RefreshEntitiesPosition();
	}

	public void AddEntity (EntitySavedData _newEntity)
	{
		Entity entity = Instantiate(_newEntity.FrameData != null ? _newEntity.FrameData.prefab : GameAssets.current.game.defaultEntity, m_squadEntitiesParent.transform);
		m_squadEntities.Add(entity);
		entity.InitVisualOnly(_newEntity);
		entity.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

		RefreshEntitiesPosition();
	}

	public void RefreshEntitiesPosition ()
	{
		for(int i = 0; i < m_squadEntities.Count; i++)
		{
			m_squadEntities[i].transform.localPosition = new Vector3(m_unitSpacing * i, 0f, 0f);
		}
	}

}
