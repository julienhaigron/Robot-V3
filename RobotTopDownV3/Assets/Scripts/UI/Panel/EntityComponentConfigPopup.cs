using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityComponentConfigPopup : AUIPopup
{
	[SerializeField] private BaseButton m_closeBtn;

	private EntitySavedData m_currentEntity;
	private EntityEquipmentData m_currentEquipment; 

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
	}

	public void Init(EntitySavedData _data, EntityEquipmentData _equipmentData )
	{
		m_currentEntity = _data;
		m_currentEquipment = _equipmentData;

		switch (_equipmentData.GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				FrameEquipmentData chassisData = GameAssets.current.equipments[_data.frameID] as FrameEquipmentData;

				break;
			case EntityEquipmentData.EquipmentType.Brain:

				break;
			case EntityEquipmentData.EquipmentType.Reactor:

				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:

				break;
		}

		
	}

	private void OnClickClose ()
	{
		Close();
	}
}
