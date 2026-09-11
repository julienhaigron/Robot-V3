using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DisallowMultipleComponent]
public class UISoundTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
	[SerializeField] private bool m_useDefaultSfx = true;
	[SerializeField] private SfxId m_hoverSfxOverride = SfxId.None;
	[SerializeField] private SfxId m_clickSfxOverride = SfxId.None;

	[Header("Slider")]
	[SerializeField] private float m_sliderSfxInterval = .08f;

	private Selectable m_selectable;
	private Toggle m_toggle;
	private Slider m_slider;
	private TMP_Dropdown m_dropdown;

	private float m_lastSliderSfxTime = float.NegativeInfinity;

	private void Awake ()
	{
		m_selectable = GetComponent<Selectable>();
		m_toggle = GetComponent<Toggle>();
		m_slider = GetComponent<Slider>();
		m_dropdown = GetComponent<TMP_Dropdown>();

		if (m_toggle != null)
			m_toggle.onValueChanged.AddListener(OnToggleChanged);

		if (m_slider != null)
			m_slider.onValueChanged.AddListener(OnSliderChanged);

		if (m_dropdown != null)
			m_dropdown.onValueChanged.AddListener(OnDropdownChanged);
	}

	private void OnDestroy ()
	{
		if (m_toggle != null)
			m_toggle.onValueChanged.RemoveListener(OnToggleChanged);

		if (m_slider != null)
			m_slider.onValueChanged.RemoveListener(OnSliderChanged);

		if (m_dropdown != null)
			m_dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		if (!IsInteractable)
			return;

		Play(m_hoverSfxOverride, _config => _config.buttonHover);
	}

	public void OnPointerClick ( PointerEventData eventData )
	{
		if (m_toggle != null || m_slider != null || m_dropdown != null)
			return;

		if (!IsInteractable)
		{
			Play(SfxId.None, _config => _config.buttonBlocked);
			return;
		}

		Play(m_clickSfxOverride, _config => _config.buttonClick);
	}

	private void OnToggleChanged ( bool _isOn )
	{
		if (m_clickSfxOverride != SfxId.None)
		{
			SoundManager.PlayUISafe(m_clickSfxOverride);
			return;
		}

		Play(SfxId.None, _config => _isOn ? _config.toggleOn : _config.toggleOff);
	}

	private void OnSliderChanged ( float _value )
	{
		if (Time.unscaledTime - m_lastSliderSfxTime < m_sliderSfxInterval)
			return;

		m_lastSliderSfxTime = Time.unscaledTime;
		Play(m_clickSfxOverride, _config => _config.sliderChanged);
	}

	private void OnDropdownChanged ( int _index )
	{
		Play(m_clickSfxOverride, _config => _config.dropdownChanged);
	}

	private bool IsInteractable => m_selectable == null || m_selectable.IsInteractable();

	private void Play ( SfxId _override, System.Func<UISoundConfig, SfxId> _default )
	{
		SoundManager.PlayUISafe(_override, _default, m_useDefaultSfx);
	}
}
