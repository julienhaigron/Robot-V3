using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Linq;

public class HubManager : Singleton<HubManager>
{
	[Title("Depedencies")]
	[SerializeField] private GameObject m_squadEntitiesParent;
	[SerializeField] private Transform m_hangarCameraPosition;

	[Title("Parameters")]
	[SerializeField] private float m_unitSpacing = 1.5f;

	private SerializableDictionary<EntitySavedData, Entity> m_squadEntities = new();

    public void ShowHangar ()
	{
		CameraManager.Instance.TeleportCameraTo(m_hangarCameraPosition);

		RefreshSquadEntities();
	}

	public void HideHangar ()
	{
		CameraManager.Instance.ResetPosition();

		foreach (Entity entity in m_squadEntities.Values)
		{
			Destroy(entity.gameObject);
		}
		m_squadEntities.Clear();
	}

	public void SelectEntity(Entity _selectedEntity )
	{
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(_selectedEntity.Data, false);

		//RefreshEntitiesPosition();
	}

	public void RefreshSquadEntities ()
	{
		List<EntitySavedData> squadData = GameDatas.current.currentPlayerSave.GetSquadEntitiesData();
		foreach (EntitySavedData entityData in m_squadEntities.Keys.ToArray())
		{
			if (!squadData.Contains(entityData))
			{
				Destroy(m_squadEntities[entityData].gameObject);
				m_squadEntities.Remove(entityData);
			}
		}

		for(int i = 0; i < squadData.Count; i++) 
		{
			if(i >= m_squadEntities.Count)
				AddEntity(squadData[i]);

			//entity.RefreshHangarVisual ?
		}

		RefreshEntitiesPosition();
	}

	public void AddEntity (EntitySavedData _newEntity)
	{
		Entity entity = Instantiate(_newEntity.FrameData != null ? _newEntity.FrameData.prefab : GameAssets.current.game.defaultEntity, m_squadEntitiesParent.transform);
		m_squadEntities.Add(_newEntity, entity);
		entity.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
		entity.InitHangarMode(_newEntity);

		RefreshEntitiesPosition();
	}

	public void RefreshEntitiesPosition ()
	{
		List<EntitySavedData> keys = m_squadEntities.Keys.ToList();
		for (int i = 0; i < keys.Count; i++)
		{
			m_squadEntities[keys[i]].transform.localPosition = new Vector3(m_unitSpacing * i, 0f, 0f);
		}
	}

}
