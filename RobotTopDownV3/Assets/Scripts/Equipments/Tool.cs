using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Linq;

public class Tool : MonoBehaviour
{
	protected ToolEquipmentData m_data;
	public ToolEquipmentData Data => m_data;
	protected Entity m_user;
	public Entity LinkedEntity => m_user;
	private string m_id;
	public string ID => m_id;


	protected void LogUseTool ( SpecialAction _specialAction )
	{
		List<string> targetNames = new();
		if (_specialAction.targetedEntityIDs != null)
		{
			foreach (int targetID in _specialAction.targetedEntityIDs)
			{
				Entity target = GameManager.Instance.GetEntityFromID(targetID);
				if (target != null && !targetNames.Contains(target.Data.name))
					targetNames.Add(target.Data.name);
			}
		}

		Entity firstTarget = targetNames.Count == 0 || _specialAction.targetedEntityIDs == null
			? null : GameManager.Instance.GetEntityFromID(_specialAction.targetedEntityIDs[0]);

		LocalizationManager localization = LocalizationManager.Instance;
		List<AEntityStatus> appliedStatuses = _specialAction.Data.GetAppliedStatuses(_specialAction, m_user, firstTarget);

		LogConsole.AddLog(_specialAction.TagLogMessage(string.Format(localization.Get(LocalizationKey.log_use_tool), m_user.Data.name
				, _specialAction.Data.GetLocalizedName()
				, targetNames.Count == 0 ? localization.Get(LocalizationKey.log_use_weapon_ground) : string.Join(", ", targetNames)))
			, LogConsole.LogEventType.UseTool
			, new LogConsole.LogDetails("usetool_" + LogConsole.Instance.LogsDetails.Keys.Count, _specialAction.Data.GetLocalizedName(), _specialAction.Data.GetDescription()));

		foreach (AEntityStatus status in appliedStatuses)
		{
			LogConsole.AddLog(string.Format(localization.Get(LocalizationKey.log_effect), status.GetLocalizedName())
				, LogConsole.LogEventType.UseTool
				, new LogConsole.LogDetails("effect_" + LogConsole.Instance.LogsDetails.Keys.Count, status.GetLocalizedName(), status.GetTooltip(status.duration)));
		}
	}


	public virtual void Init ( Entity _user, ToolEquipmentData _data, string _id, bool _isFirstSide )
	{
		m_user = _user;
		m_data = _data;
		m_id = _id;
	}

	public virtual void PerformAction ( SpecialAction _specialAction, Action _onPerformEnd )
	{
		LogUseTool(_specialAction);
		DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
	}
}
