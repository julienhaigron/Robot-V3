using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DayEventChip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private GameObject m_content;
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private Color m_accent = new(.91f, .7f, .23f, 1f);

	private void Awake ()
	{
		LocalizationManager.onLanguageChanged += Refresh;
	}

	private void OnDestroy ()
	{
		LocalizationManager.onLanguageChanged -= Refresh;
	}

	public void Refresh ()
	{
		DayEventState dayEvent = GameDatas.current.currentPlayerSave.dayEvent;
		m_content.SetActive(dayEvent.IsActive);
		m_nameTMP.text = dayEvent.GetName();
	}

	public void OnPointerEnter ( PointerEventData _eventData )
	{
		DayEventState dayEvent = GameDatas.current.currentPlayerSave.dayEvent;
		DayEventData data = dayEvent.Data;
		if (data != null)
			ToolTipManager.Instance.Show(data.GetLocalizedName(), data.GetLocalizedLore() + "\n\n" + string.Join("\n", dayEvent.GetEffectLines()), gameObject, null, m_accent);
	}

	public void OnPointerExit ( PointerEventData _eventData )
	{
		ToolTipManager.Instance.Hide();
	}
}
