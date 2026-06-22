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

	[ReadOnly, SerializeField] private int m_id;

	public void Init(bool _hasSave, int _saveID)
	{
		m_id = _saveID;

		if (!_hasSave)
		{
			SetInteractability(false);
			m_name.text = "Empty save slot";
		}
		else
		{
			SetInteractability(true);
			m_name.text = GameDatas.current.playerSaves[_saveID].saveName;
		}
	}

	protected override void OnClick ()
	{
		GameDatas.current.game.lastPlayerSaveSelectedID = m_id;
		GameManager.Instance.LoadSaveAndGoToHub(m_id);
		base.OnClick();
	}


}
