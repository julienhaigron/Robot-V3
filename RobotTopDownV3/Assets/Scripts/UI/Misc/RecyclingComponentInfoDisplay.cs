using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecyclingComponentInfoDisplay : MonoBehaviour
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private CanvasGroup m_canvasGroup;

	private Vector2 m_restPosition;
	private ulong m_price;

	private void Awake ()
	{
		m_restPosition = ((RectTransform)transform).anchoredPosition;
	}

	public void Init ( GameDatas.PlayerSave.DayData.RecyclingComponentData _component )
	{
		EntityEquipmentData data = _component.component.GetData<EntityEquipmentData>();
		m_icon.sprite = data.icon;
		m_nameTMP.text = data.GetLocalizedName();
		m_price = data.GetRecyclingPrice(_component.yieldBonusPercent).Item2;
		m_priceTMP.text = PriceText(m_price);
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}

	public Tween Reveal ( float _duration, float _slideDistance )
	{
		RectTransform rectTransform = (RectTransform)transform;
		m_canvasGroup.alpha = 0f;
		rectTransform.anchoredPosition = m_restPosition + Vector2.right * _slideDistance;

		return DOTween.Sequence()
			.Append(m_canvasGroup.DOFade(1f, _duration))
			.Join(rectTransform.DOAnchorPos(m_restPosition, _duration).SetEase(Ease.OutCubic))
			.Join(DOTween.To(() => 0f, _value => m_priceTMP.text = PriceText((ulong)_value), m_price, _duration * 1.6f));
	}

	private static string PriceText ( ulong _price )
	{
		return "+ " + _price;
	}
}
