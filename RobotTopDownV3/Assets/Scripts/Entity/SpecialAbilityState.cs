using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpecialAbilityState
{
	public readonly EntityActionData data;
	public readonly string componentID;
	public Action onChanged;

	private int m_points;
	private int m_lastEarnedTick = int.MinValue;
	private int m_windowStart = int.MinValue;
	private int m_windowEnd = int.MinValue;

	public SpecialAbilityState ( EntityActionData _data, string _componentID )
	{
		data = _data;
		componentID = _componentID;
	}

	public int Points => m_points;
	public int MaxPoints => data.abilityChargeMax * data.abilityRatio;
	public int Charges => m_points / data.abilityRatio;
	public bool IsFull => m_points >= MaxPoints;
	public int PointsTowardNextCharge => IsFull ? data.abilityRatio : m_points % data.abilityRatio;

	public int WindowLength => m_windowEnd - m_windowStart;

	public bool IsActiveAt ( int _absoluteTick )
	{
		return _absoluteTick >= m_windowStart && _absoluteTick < m_windowEnd;
	}

	public int GetRemainingActiveTicks ( int _absoluteTick )
	{
		return IsActiveAt(_absoluteTick) ? m_windowEnd - _absoluteTick : 0;
	}

	public bool EarnsFrom ( EntityActionData _action )
	{
		return _action != null && !_action.isModAction && _action.codeType != EntityActionData.ActionCodeType.Wait && _action.type == data.type;
	}

	public void Earn ( EntityActionData _action, int _absoluteTick )
	{
		if (_absoluteTick == m_lastEarnedTick || IsFull || !EarnsFrom(_action) || IsActiveAt(_absoluteTick))
			return;

		m_lastEarnedTick = _absoluteTick;
		m_points++;
		onChanged?.Invoke();
	}

	public void Activate ( int _charges, int _absoluteTick )
	{
		m_points = Mathf.Max(0, m_points - _charges * data.abilityRatio);
		m_windowStart = _absoluteTick;
		m_windowEnd = _absoluteTick + data.GetAbilityWindowLength(_charges);
		onChanged?.Invoke();
	}

#if UNITY_EDITOR
	public void DebugFill ()
	{
		m_points = MaxPoints;
		onChanged?.Invoke();
	}
#endif

	public int PredictChargesAt ( IEnumerable<TurnManager.RecordedAction> _queue, int _tick, Func<int, int> _toAbsoluteTick )
	{
		int points = m_points;
		foreach (TurnManager.RecordedAction recordedAction in _queue ?? Array.Empty<TurnManager.RecordedAction>())
		{
			AEntityAction action = recordedAction.action;
			if (action.timeAtStart >= _tick)
				break;
			if (!EarnsFrom(action.Data))
				continue;

			int end = Mathf.Min(action.TimeAtEnd, _tick);
			for (int tick = action.timeAtStart; tick < end; tick++)
			{
				if (!IsActiveAt(_toAbsoluteTick(tick)))
					points = Mathf.Min(points + 1, MaxPoints);
			}
		}

		return points / data.abilityRatio;
	}
}
