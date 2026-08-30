using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class OptionPopup : AUIPopup
{
	[System.Serializable]
	private class RebindEntry
	{
		public string actionName;
		public int bindingIndex;
		public TextMeshProUGUI bindingLabel;
		public BaseButton rebindButton;
	}

	[Header("General")]
	[SerializeField] private BaseButton m_closeBtn;

	[Header("Volume")]
	[SerializeField] private Slider m_volumeSlider;

	[Header("Language")]
	[SerializeField] private TMP_Dropdown m_languageDropdown;

	[Header("Controls")]
	[SerializeField] private RebindEntry[] m_rebindEntries;
	[SerializeField] private BaseButton m_resetControlsBtn;
	[SerializeField] private TextMeshProUGUI m_rebindPromptLabel;

	private List<SystemLanguage> m_availableLanguages;
	private InputActionRebindingExtensions.RebindingOperation m_activeRebind;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		m_volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
		m_languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

		if (m_resetControlsBtn != null)
			m_resetControlsBtn.onClick += OnClickResetControls;

		foreach (RebindEntry entry in m_rebindEntries)
			entry.rebindButton.onClick += () => StartRebind(entry);
	}

	private void OnDestroy ()
	{
		m_closeBtn.onClick -= OnClickClose;
		m_volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
		m_languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);

		if (m_resetControlsBtn != null)
			m_resetControlsBtn.onClick -= OnClickResetControls;

		m_activeRebind?.Dispose();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshVolume();
		RefreshLanguages();
		RefreshBindings();
	}

	private void OnClickClose ()
	{
		Close();
	}

	#region Volume

	private void RefreshVolume ()
	{
		m_volumeSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
	}

	private void OnVolumeChanged ( float _value )
	{
		SoundManager.Instance.SetMasterVolume(_value);
	}

	#endregion

	#region Language

	private void RefreshLanguages ()
	{
		m_availableLanguages = new List<SystemLanguage>(LocalizationManager.Instance.AvailableLanguages);

		m_languageDropdown.ClearOptions();
		m_languageDropdown.AddOptions(m_availableLanguages.ConvertAll(_language => _language.ToString()));

		int currentIndex = m_availableLanguages.IndexOf(LocalizationManager.Instance.CurrentLanguage);
		m_languageDropdown.SetValueWithoutNotify(Mathf.Max(currentIndex, 0));
	}

	private void OnLanguageChanged ( int _index )
	{
		if (_index < 0 || _index >= m_availableLanguages.Count)
			return;

		LocalizationManager.Instance.SetLanguage(m_availableLanguages[_index]);
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
			m_rebindPromptLabel.text = $"Press a key for {_entry.actionName}...";
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
