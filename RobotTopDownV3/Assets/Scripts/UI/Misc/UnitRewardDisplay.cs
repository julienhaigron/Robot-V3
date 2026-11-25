using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitRewardDisplay : ARewardDisplay
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private Image m_mainIcon;
	[SerializeField] private Image m_subIcon;
	private UnitPreset m_unitPreset;
	public UnitPreset UnitPreset => m_unitPreset;

	public static void RefreshPreview ( UnitRewardDisplay[] _displays, MissionData.RewardSet _rewardSet )
	{
		if (_displays == null)
			return;

		for (int i = 0; i < _displays.Length; i++)
		{
			if (_displays[i] == null)
				continue;

			if (_rewardSet != null && _rewardSet.units.Count > i && _rewardSet.units[i] != null)
			{
				_displays[i].Show();
				_displays[i].Init(_rewardSet.units[i], null, true);
			}
			else
				_displays[i].Hide();
		}
	}

	public void Init ( UnitPreset _unit, System.Action _onSelected, bool _isLockedOnSelected = false )
	{
		m_unitPreset = _unit;
		m_mainIcon.sprite = _unit.icon;
		m_subIcon.sprite = GameAssets.current.ui.corporationsIcons[_unit.GetSavedData().GetDominentFaction(out float percentage)];
		SetDismantled(false);

		InitSelection(_onSelected, _isLockedOnSelected);
	}

	public void SetDismantled ( bool _isDismantled )
	{
		string name = m_unitPreset.GetLocalizedName();
		m_nameTMP.text = !_isDismantled ? name : name + "\n<size=70%><color=#" + ColorUtility.ToHtmlStringRGB(GestureFeedback.RefusalColor) + ">"
			+ LocalizationManager.Instance.Get(LocalizationKey.endlevel_unit_dismantled) + "</color></size>";
	}
}
