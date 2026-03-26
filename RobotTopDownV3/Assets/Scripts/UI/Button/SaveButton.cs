using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class SaveButton : BaseButton
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;

	[ReadOnly, SerializeField] private GameDatas.PlayerSave m_save;
	[ReadOnly, SerializeField] private int m_id;

	public void Init( GameDatas.PlayerSave _save, int _saveID)
	{
		m_save = _save;
		m_id = _saveID;

		if (_save == null)
		{
			SetInteractability(false);
			m_name.text = "Empty save slot";
		}
		else
		{
			SetInteractability(true);
			m_name.text = _save.saveName;
		}
	}

	protected override void OnClick ()
	{
		if (m_save == null)
			return;

		GameDatas.current.game.lastPlayerSaveSelectedID = m_id;
		GameManager.Instance.LoadSaveAndGoToHub(m_id);
		base.OnClick();
	}


}
