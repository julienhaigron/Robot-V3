using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecyclingComponentInfoDisplay : MonoBehaviour
{
    [SerializeField] private Image m_icon;
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private TextMeshProUGUI m_priceTMP;

    public void Init ( GameDatas.PlayerSave.DayData.RecyclingComponentData _component)
	{
        EntityEquipmentData data = _component.component.GetData<EntityEquipmentData>();
        m_icon.sprite = data.icon;
        m_nameTMP.text = data.displayName;
        m_priceTMP.text = "-" + data.GetRecyclingPrice();
        gameObject.SetActive(true);
    }

    public void Hide ()
	{
        gameObject.SetActive(false);
	}
}
