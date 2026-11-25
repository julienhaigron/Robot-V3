using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlueprintDisplay : MonoBehaviour
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private Image m_currencyIcon;
	[SerializeField] private BaseButton m_processBtn;

	private EntityEquipmentData m_component;
	private System.Action<EntityEquipmentData> m_onProcess;
	private Color? m_basePriceColor;

	private void Awake ()
	{
		m_processBtn.onClick += OnClickProcess;
	}

	private void OnDestroy ()
	{
		m_processBtn.onClick -= OnClickProcess;
	}

	public void Show ( EntityEquipmentData _component, bool _canAfford, System.Action<EntityEquipmentData> _onProcess )
	{
		m_component = _component;
		m_onProcess = _onProcess;
		m_icon.sprite = _component.icon;
		m_nameTMP.text = _component.GetLocalizedName();
		System.Tuple<CurrencyType, ulong> price = _component.GetBlueprintProcessingPrice();
		m_priceTMP.text = price.Item2.ToString();
		m_currencyIcon.sprite = GameAssets.current.currencies[price.Item1].icon;
		m_basePriceColor ??= m_priceTMP.color;
		m_priceTMP.color = _canAfford ? m_basePriceColor.Value : GestureFeedback.RefusalColor;
		m_processBtn.SetInteractability(_canAfford);
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		m_component = null;
		m_onProcess = null;
		gameObject.SetActive(false);
	}

	private void OnClickProcess ()
	{
		if (m_component != null)
			m_onProcess?.Invoke(m_component);
	}
}
