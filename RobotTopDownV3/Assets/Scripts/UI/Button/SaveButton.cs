using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class SaveButton : BaseButton
{
	public static System.Action onSaveListChanged;

	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;

	[Title("Optional")]
	[SerializeField] private TextMeshProUGUI m_lastSaveTimeTMP;
	[SerializeField] private TMP_InputField m_nameInput;
	[SerializeField] private BaseButton m_deleteBtn;

	[ReadOnly, SerializeField] private int m_id;

	private bool m_hasSave;
	private bool m_isDeleteArmed;

	private void Awake ()
	{
		if (m_deleteBtn != null)
			m_deleteBtn.onClick += OnClickDelete;

		if (m_nameInput != null)
			m_nameInput.onEndEdit.AddListener(OnEndNameEdit);
	}

	private void OnDestroy ()
	{
		if (m_deleteBtn != null)
			m_deleteBtn.onClick -= OnClickDelete;

		if (m_nameInput != null)
			m_nameInput.onEndEdit.RemoveListener(OnEndNameEdit);
	}

	public void Init ( bool _hasSave, int _saveID )
	{
		m_id = _saveID;
		m_hasSave = _hasSave && GameDatas.current.IsValidSaveID(_saveID);
		SetDeleteArmed(false);

		SetInteractability(m_hasSave);

		if (!m_hasSave)
		{
			m_name.text = LocalizationManager.Instance.Get(LocalizationKey.save_empty_slot);

			if (m_lastSaveTimeTMP != null)
				m_lastSaveTimeTMP.text = "";
		}
		else
		{
			GameDatas.PlayerSave save = GameDatas.current.playerSaves[_saveID];
			m_name.text = save.saveName;

			if (m_lastSaveTimeTMP != null)
				m_lastSaveTimeTMP.text = save.GetLastSaveTimeText();

			if (m_nameInput != null)
				m_nameInput.SetTextWithoutNotify(save.saveName);
		}

		if (m_nameInput != null)
			m_nameInput.gameObject.SetActive(m_hasSave);

		if (m_deleteBtn != null)
			m_deleteBtn.gameObject.SetActive(m_hasSave);
	}

	protected override void OnClick ()
	{
		if (!m_hasSave)
			return;

		GameDatas.current.game.lastPlayerSaveSelectedID = m_id;
		GameManager.Instance.LoadSaveAndGoToHub(m_id);
		base.OnClick();
	}

	private void OnEndNameEdit ( string _newName )
	{
		if (!m_hasSave)
			return;

		if (!GameDatas.current.RenameSave(m_id, _newName))
		{
			m_nameInput.SetTextWithoutNotify(GameDatas.current.playerSaves[m_id].saveName);
			return;
		}

		m_name.text = GameDatas.current.playerSaves[m_id].saveName;
		GameDatas.current.Save();
	}

	private void OnClickDelete ()
	{
		if (!m_hasSave)
			return;

		if (!m_isDeleteArmed)
		{
			SetDeleteArmed(true);
			return;
		}

		SetDeleteArmed(false);
		if (!GameDatas.current.DeleteSave(m_id))
			return;

		GameDatas.current.Save();
		onSaveListChanged?.Invoke();
	}

	private void SetDeleteArmed ( bool _isArmed )
	{
		m_isDeleteArmed = _isArmed;

		if (m_deleteBtn != null)
			m_deleteBtn.SetLabel(LocalizationManager.Instance.Get(_isArmed ? LocalizationKey.save_delete_confirm : LocalizationKey.save_delete));
	}
}
