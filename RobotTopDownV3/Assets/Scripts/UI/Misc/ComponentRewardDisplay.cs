using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ComponentRewardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private TextMeshProUGUI m_priceTMP;
    [SerializeField] private Image m_mainIcon;
    [SerializeField] private Image m_subIcon;

	public void Init(EntityEquipmentData _component)
	{
        m_nameTMP.text = _component.displayName;
        m_mainIcon.sprite = _component.icon;
        System.Tuple<CurrencyType, ulong> price = _component.GetPrice();
        m_priceTMP.text = price.Item2.ToString();
        //m_subIcon.sprite = 
    }

    public void Show ()
    {
        gameObject.SetActive(true);
    }

    public void Hide ()
    {
        gameObject.SetActive(false);
    }


}
