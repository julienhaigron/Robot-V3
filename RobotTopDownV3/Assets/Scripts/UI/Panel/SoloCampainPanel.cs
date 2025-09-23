using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SoloCampainPanel : AUIPanel
{
	[SerializeField] private BaseButton m_changeSquadBtn;
    [SerializeField] private LevelButton m_levelBtnPrefab;
    [SerializeField] private Transform m_levelBtnsParent;

    [ReadOnly, SerializeField] private List<LevelButton> levelBtns = new();

	private void Awake ()
	{
		m_changeSquadBtn.onClick += OnClickChangeSquadBtn;
	}

	private void OnClickChangeSquadBtn ()
	{
		UIManager.Instance.OpenPanel<SquadConfigPanel>();
	}

#if UNITY_EDITOR
	[Button]
	private void SetupLevelBtns()
	{
		foreach(LevelButton btn in levelBtns)
		{
			DestroyImmediate(btn.gameObject);
		}
		levelBtns.Clear();


		foreach (LevelData level in GameAssets.current.game.levels)
		{
			LevelButton newBtn = UnityEditor.PrefabUtility.InstantiatePrefab(m_levelBtnPrefab, m_levelBtnsParent) as LevelButton;
			newBtn.Init(level);
			newBtn.gameObject.name = level.name;
			levelBtns.Add(newBtn);
		}
	}
#endif
}
