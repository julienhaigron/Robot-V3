using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Linq;
using Sirenix.OdinInspector;

public class Shield : Tool
{
	public ShieldEquipmentData ShieldData => m_data as ShieldEquipmentData;

	public int orientation = 0;

	private int m_currentHp;

	public override void Init ( Entity _user, ToolEquipmentData _data, bool _isFirstSide )
	{
		base.Init(_user, _data, _isFirstSide);

		orientation = _isFirstSide ? 5 : 2;
		m_currentHp = ShieldData.hp;
	}

	public int RemoveDamage(int _damageAmount )
	{
		if (m_currentHp <= 0)
			return 0;

		int amountRemoved = Mathf.Min(_damageAmount, m_currentHp);
		m_currentHp -= amountRemoved;
		return amountRemoved;
	}

	public override void PerformAction ( SpecialAction _specialAction, Action _onPerformEnd )
	{

		if(_specialAction is TurnShieldAction turnAction)
		{
			orientation = turnAction.targetedOrientation;
		}

		DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
	}
}
