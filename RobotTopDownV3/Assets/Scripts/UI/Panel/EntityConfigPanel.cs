using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityConfigPanel : AUIPanel
{
    [SerializeField] private EntityEquipmentDisplay m_chassisDisplay;
    [SerializeField] private List<EntityEquipmentDisplay> m_armSlot;
    [SerializeField] private EntityEquipmentDisplay m_brain;

	private EntitySavedData m_currentEntity;

	public void Init(EntitySavedData _data )
	{
		m_currentEntity = _data;

		FrameEquipmentData chassisData = GameAssets.current.equipments[_data.frameID] as FrameEquipmentData;

		m_chassisDisplay.Init(chassisData);
		m_brain.Init(GameAssets.current.equipments[_data.brainID]);
		for(int i = 0; i < m_armSlot.Count; i++)
		{
			m_armSlot[i].Init(i < chassisData.armSlotAvailable ? null : GameAssets.current.equipments[_data.armsIds[i].value]);
		}
	}
}
