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


	public virtual void Init ( Entity _user, ToolEquipmentData _data, bool _isFirstSide )
	{
		m_user = _user;
		m_data = _data;
		m_id = _data.name + _user.Equipment.Tools.Values.Count;
	}

	public virtual void PerformAction ( SpecialAction _specialAction, Action _onPerformEnd )
	{
		DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
	}
}
