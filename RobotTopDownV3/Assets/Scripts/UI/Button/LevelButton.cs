using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class LevelButton : BaseButton
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;

	[ReadOnly, SerializeField] private LevelData m_level;

	public void Init( LevelData _level )
	{
		m_level = _level;
		m_name.text = _level.title;
	}

	protected override void OnClick ()
	{
		UIManager.Instance.ClosePanel<SoloHubPanel>();
		GameManager.Instance.SetupLevel(m_level);
		base.OnClick();
	}


}
