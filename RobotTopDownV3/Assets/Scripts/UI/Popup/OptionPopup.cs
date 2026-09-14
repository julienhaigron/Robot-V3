using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class OptionPopup : AUIPopup
{
	[System.Serializable]
	private class VolumeSlider
	{
		public SoundChannel channel;
		public Slider slider;
	}

	[System.Serializable]
	private class RebindEntry
	{
		public string actionName;
		public int bindingIndex;
		[LocalizedKey] public string labelKey;
		public TextMeshProUGUI bindingLabel;
		public BaseButton rebindButton;
	}

	[Header("General")]
	[SerializeField] private BaseButton m_closeBtn;

	[Header("Volume")]
	[SerializeField] private Slider m_masterVolumeSlider;
	[SerializeField] private VolumeSlider[] m_channelVolumeSliders;

	[Header("Language")]
	[SerializeField] private TMP_Dropdown m_languageDropdown;

	[Header("Graphics")]
	[SerializeField] private TMP_Dropdown m_displayModeDropdown;
	[SerializeField] private TMP_Dropdown m_windowSizeDropdown;

	[Header("Controls")]
	[SerializeField] private RebindEntry[] m_rebindEntries;
	[SerializeField] private BaseButton m_resetControlsBtn;
	[SerializeField] private TextMeshProUGUI m_rebindPromptLabel;

	private const float VOLUME_PREVIEW_INTERVAL = .08f;
	private const int MIN_WINDOW_WIDTH = 1024;

	private List<SystemLanguage> m_availableLanguages;
	private float m_lastVolumePreviewTime = float.NegativeInfinity;
	private InputActionRebindingExtensions.RebindingOperation m_activeRebind;
	private List<Vector2Int> m_windowSizes = new List<Vector2Int>();
	private int m_selectedWindowSizeIndex;
	private bool m_isFullscreen;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		m_masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

		foreach (VolumeSlider entry in m_channelVolumeSliders)
			entry.slider.onValueChanged.AddListener(_value => OnChannelVolumeChanged(entry.channel, _value));

		m_languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
		LocalizationManager.onLanguageChanged += RefreshLanguages;
		m_displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
		m_windowSizeDropdown.onValueChanged.AddListener(OnWindowSizeChanged);
		LocalizationManager.onLanguageChanged += RefreshGraphics;

		if (m_resetControlsBtn != null)
			m_resetControlsBtn.onClick += OnClickResetControls;

		foreach (RebindEntry entry in m_rebindEntries)
			entry.rebindButton.onClick += () => StartRebind(entry);
	}

	private void OnDestroy ()
	{
		m_closeBtn.onClick -= OnClickClose;
		m_masterVolumeSlider.onValueChanged.RemoveAllListeners();

		foreach (VolumeSlider entry in m_channelVolumeSliders)
			entry.slider.onValueChanged.RemoveAllListeners();

		m_languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
		LocalizationManager.onLanguageChanged -= RefreshLanguages;
		LocalizationManager.onLanguageChanged -= RefreshGraphics;
		m_displayModeDropdown.onValueChanged.RemoveListener(OnDisplayModeChanged);
		m_windowSizeDropdown.onValueChanged.RemoveListener(OnWindowSizeChanged);

		if (m_resetControlsBtn != null)
			m_resetControlsBtn.onClick -= OnClickResetControls;

		m_activeRebind?.Dispose();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshVolume();
		RefreshLanguages();
		RefreshGraphics();
		RefreshBindings();
	}

	protected override void OnHideStarted ()
	{
		base.OnHideStarted();

		SaveVolumes();
	}

	private void OnClickClose ()
	{
		Close();
	}

	#region Volume

	private void RefreshVolume ()
	{
		m_masterVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);

		foreach (VolumeSlider entry in m_channelVolumeSliders)
			entry.slider.SetValueWithoutNotify(SoundManager.Instance.GetChannelVolume(entry.channel));
	}

	private void OnMasterVolumeChanged ( float _value )
	{
		SoundManager.Instance.SetMasterVolume(_value, false);
		PlayVolumePreview(SoundChannel.UI);
	}

	private void OnChannelVolumeChanged ( SoundChannel _channel, float _value )
	{
		SoundManager.Instance.SetChannelVolume(_channel, _value, false);
		PlayVolumePreview(_channel);
	}

	private void SaveVolumes ()
	{
		if (SoundManager.Instance != null)
			SoundManager.Instance.SaveVolumes();
	}

	private void PlayVolumePreview ( SoundChannel _channel )
	{
		if (_channel == SoundChannel.Music || SoundManager.Instance.UISounds == null)
			return;

		if (Time.unscaledTime - m_lastVolumePreviewTime < VOLUME_PREVIEW_INTERVAL)
			return;

		m_lastVolumePreviewTime = Time.unscaledTime;
		SoundManager.Instance.Play(SoundManager.Instance.UISounds.sliderChanged, _channel);
	}

	#endregion

	#region Language

	private void RefreshLanguages ()
	{
		m_availableLanguages = new List<SystemLanguage>(LocalizationManager.Instance.AvailableLanguages);

		m_languageDropdown.ClearOptions();
		m_languageDropdown.AddOptions(m_availableLanguages.ConvertAll(GetLanguageLabel));

		int currentIndex = m_availableLanguages.IndexOf(LocalizationManager.Instance.CurrentLanguage);
		m_languageDropdown.SetValueWithoutNotify(Mathf.Max(currentIndex, 0));
		m_languageDropdown.RefreshShownValue();
	}

	private string GetLanguageLabel ( SystemLanguage _language )
	{
		string key = "language/" + _language.ToString().ToLowerInvariant();
		string label = LocalizationManager.Instance.Get(key);

		// Get() renvoie la key quand la traduction n'existe pas
		return label == key ? _language.ToString() : label;
	}

	private void OnLanguageChanged ( int _index )
	{
		if (_index < 0 || _index >= m_availableLanguages.Count)
			return;

		LocalizationManager.Instance.SetLanguage(m_availableLanguages[_index]);
	}

	#endregion

	#region Graphics

	private void RefreshGraphics ()
	{
		m_isFullscreen = Screen.fullScreen;

		BuildWindowSizes();

		m_displayModeDropdown.ClearOptions();
		m_displayModeDropdown.AddOptions(new List<string>
		{
			LocalizationManager.Instance.Get(LocalizationKey.option_display_fullscreen),
			LocalizationManager.Instance.Get(LocalizationKey.option_display_windowed)
		});
		m_displayModeDropdown.SetValueWithoutNotify(m_isFullscreen ? 0 : 1);
		m_displayModeDropdown.RefreshShownValue();

		m_windowSizeDropdown.ClearOptions();
		m_windowSizeDropdown.AddOptions(m_windowSizes.ConvertAll(GetWindowSizeLabel));
		m_windowSizeDropdown.SetValueWithoutNotify(m_selectedWindowSizeIndex);
		m_windowSizeDropdown.RefreshShownValue();

		RefreshWindowSizeInteractability();
	}

	private void BuildWindowSizes ()
	{
		m_windowSizes.Clear();

		int maxWidth = Display.main.systemWidth;
		int maxHeight = Display.main.systemHeight;

		foreach (Resolution resolution in Screen.resolutions)
		{
			Vector2Int size = new Vector2Int(resolution.width, resolution.height);

			if (size.x < MIN_WINDOW_WIDTH || size.x > maxWidth || size.y > maxHeight)
				continue;

			if (!m_windowSizes.Contains(size))
				m_windowSizes.Add(size);
		}

		Vector2Int nativeSize = new Vector2Int(maxWidth, maxHeight);
		if (nativeSize.x >= MIN_WINDOW_WIDTH && !m_windowSizes.Contains(nativeSize))
			m_windowSizes.Add(nativeSize);

		if (m_windowSizes.Count == 0)
			m_windowSizes.Add(nativeSize);

		m_windowSizes.Sort(( _a, _b ) => _a.x == _b.x ? _a.y.CompareTo(_b.y) : _a.x.CompareTo(_b.x));

		m_selectedWindowSizeIndex = m_windowSizes.IndexOf(new Vector2Int(Screen.width, Screen.height));
		if (m_selectedWindowSizeIndex < 0)
			m_selectedWindowSizeIndex = m_windowSizes.Count - 1;
	}

	private string GetWindowSizeLabel ( Vector2Int _size )
	{
		return _size.x + " x " + _size.y;
	}

	private void OnDisplayModeChanged ( int _index )
	{
		m_isFullscreen = _index == 0;

		if (m_isFullscreen)
			Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
		else
			ApplyWindowSize();

		RefreshWindowSizeInteractability();
	}

	private void OnWindowSizeChanged ( int _index )
	{
		if (_index < 0 || _index >= m_windowSizes.Count)
			return;

		m_selectedWindowSizeIndex = _index;

		if (!m_isFullscreen)
			ApplyWindowSize();
	}

	private void ApplyWindowSize ()
	{
		Vector2Int size = m_windowSizes[Mathf.Clamp(m_selectedWindowSizeIndex, 0, m_windowSizes.Count - 1)];
		Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
	}

	private void RefreshWindowSizeInteractability ()
	{
		m_windowSizeDropdown.interactable = !m_isFullscreen;
	}

	#endregion

	#region Controls

	private void RefreshBindings ()
	{
		foreach (RebindEntry entry in m_rebindEntries)
			UpdateBindingLabel(entry);
	}

	private void UpdateBindingLabel ( RebindEntry _entry )
	{
		InputAction action = PlayerController.Instance.InputActions.FindAction(_entry.actionName);
		if (action == null || _entry.bindingLabel == null)
			return;

		_entry.bindingLabel.text = action.GetBindingDisplayString(_entry.bindingIndex);
	}

	private void StartRebind ( RebindEntry _entry )
	{
		if (m_activeRebind != null)
			return;

		InputAction action = PlayerController.Instance.InputActions.FindAction(_entry.actionName);
		if (action == null)
			return;

		action.Disable();

		if (m_rebindPromptLabel != null)
		{
			m_rebindPromptLabel.gameObject.SetActive(true);
			m_rebindPromptLabel.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.option_rebind_prompt), GetActionLabel(_entry));
		}

		m_activeRebind = action.PerformInteractiveRebinding(_entry.bindingIndex)
			.WithControlsExcluding("Mouse/position")
			.WithControlsExcluding("Mouse/delta")
			.WithCancelingThrough("<Keyboard>/escape")
			.OnMatchWaitForAnother(0.1f)
			.OnComplete(_operation => OnRebindComplete(_entry, action, _operation))
			.OnCancel(_operation => OnRebindEnd(action, _operation))
			.Start();
	}

	private string GetActionLabel ( RebindEntry _entry )
	{
		if (string.IsNullOrEmpty(_entry.labelKey))
			return _entry.actionName;

		return LocalizationManager.Instance.Get(_entry.labelKey);
	}

	private void OnRebindComplete ( RebindEntry _entry, InputAction _action, InputActionRebindingExtensions.RebindingOperation _operation )
	{
		OnRebindEnd(_action, _operation);

		UpdateBindingLabel(_entry);
		PlayerController.Instance.SaveInputBindingOverrides();
	}

	private void OnRebindEnd ( InputAction _action, InputActionRebindingExtensions.RebindingOperation _operation )
	{
		_operation.Dispose();
		m_activeRebind = null;
		_action.Enable();

		if (m_rebindPromptLabel != null)
			m_rebindPromptLabel.gameObject.SetActive(false);
	}

	private void OnClickResetControls ()
	{
		PlayerController.Instance.ResetInputBindingOverrides();
		RefreshBindings();
	}

	#endregion
}
