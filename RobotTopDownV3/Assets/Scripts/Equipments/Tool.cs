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

		List<string> effectNames = new();
		foreach (AEntityStatus status in _specialAction.Data.GetAppliedStatuses(_specialAction, m_user, firstTarget))
			if (!effectNames.Contains(status.name))
				effectNames.Add(status.name);

		string message = m_user.Data.name + " uses " + _specialAction.Data.name
			+ " on " + (targetNames.Count == 0 ? "the ground" : string.Join(", ", targetNames));

		if (effectNames.Count > 0)
			message += "\n-> Effects: " + string.Join(", ", effectNames);

		LogConsole.AddLog(message, LogConsole.LogEventType.UseTool
			, new LogConsole.LogDetails("usetool_" + LogConsole.Instance.LogsDetails.Keys.Count, _specialAction.Data.name, _specialAction.Data.GetDescription()));
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
