using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ForgePanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private Transform m_unitsParent;

	[SerializeField] private FrameDisplay m_frameDisplayPrefab;

	private List<FrameDisplay> frameDisplays = new();

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SquadConfigPanel>();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshFrameList();
	}

	private void RefreshFrameList ()
	{
		for (int i = 0; i < GameAssets.current.game.frames.Count; i++)
		{
			if (frameDisplays.Count < i)
				frameDisplays.Add(Instantiate(m_frameDisplayPrefab, m_unitsParent));

			frameDisplays[i].Init(GameAssets.current.game.frames[i], () => CreateUnit(GameAssets.current.game.frames[i]));
		}
	}

	private void CreateUnit (FrameEquipmentData _frame)
	{
		GameDatas.current.currentPlayerSave.AddEquipmentToInventory(_frame);

		EntitySavedData newSavedUnit = new EntitySavedData() { armsIds = null, brainID = null, frameID = _frame.name, name = "New Unit" };
		GameDatas.current.currentPlayerSave.units.Add(newSavedUnit);
	}
}
