using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EndLevelEntityDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private DamagedSlotDisplay[] m_componentDisplays;

	[System.Serializable]
	public class SubDamagedSlotContainer
	{
		public List<DamagedSlotDisplay> slots = new();
	}

	public void Init ( EntitySavedData _data, List<GameDatas.PlayerSave.Component> _damagedComponent )
	{
		m_nameTMP.text = _data.name;

		for(int i = 0; i < m_componentDisplays.Length; i++)
		{
			if (_damagedComponent.Count <= i)
				m_componentDisplays[i].Hide();
			else
				m_componentDisplays[i].Init(_damagedComponent[i].GetData<EntityEquipmentData>().icon, true, false);
		}

	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}

	private static void InitComponentSlot ( DamagedSlotDisplay _slot, GameDatas.PlayerSave.Component _component, bool hasDamagedSub )
	{
		EntityEquipmentData componentData = _component == null ? null : _component.GetData<EntityEquipmentData>();

		if (componentData == null)
			_slot.Hide();
		else
			_slot.Init(componentData.icon, _component.isDamaged, hasDamagedSub);
	}
}
