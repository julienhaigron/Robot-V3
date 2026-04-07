using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityComponentConfigPopup : AUIPopup
{
	[SerializeField] private BaseButton m_closeBtn;

	private EntitySavedData m_currentEntity;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
	}

	public void Init(EntitySavedData _data, EntityEquipmentData _equipmentData )
	{
		m_currentEntity = _data;

		FrameEquipmentData chassisData = GameAssets.current.equipments[_data.frameID] as FrameEquipmentData;

		
	}

	private void OnClickClose ()
	{
		Close();
	}
}
