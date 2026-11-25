using UnityEngine;
using TMPro;

public class InventoryCapacityLabel : MonoBehaviour
{
	[SerializeField] private TMP_Text m_text;

	private int m_shownCount = -1;
	private int m_shownCapacity = -1;
	private Color? m_baseColor;

	private void OnEnable ()
	{
		m_shownCount = -1;
	}

	private void LateUpdate ()
	{
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		int count = save.equipmentInventory.Count;
		int capacity = save.InventoryCapacity;
		if (count == m_shownCount && capacity == m_shownCapacity)
			return;

		m_shownCount = count;
		m_shownCapacity = capacity;
		m_baseColor ??= m_text.color;
		m_text.text = count + "/" + capacity;
		m_text.color = count < capacity ? m_baseColor.Value : GestureFeedback.RefusalColor;
	}
}
