using System;
using DG.Tweening;
using UnityEngine;

public class Shield : ToolEquipment
{
	public ShieldEquipmentData ShieldData => m_data as ShieldEquipmentData;

	public int Orientation { get; private set; }

	[SerializeField] private ShieldMarker m_markerPrefab;
	[SerializeField] private PoolData m_turnVFXPool;
	[SerializeField] private float m_turnVFXDistance = .72f;

	private int m_currentHp;
	private int m_remainingActiveTicks;
	private ShieldMarker m_marker;

	public int CurrentHp => m_currentHp;
	public int MaxHp => ShieldData != null ? ShieldData.hp : 0;
	public int CoveredSides => ShieldData != null ? Mathf.Clamp(ShieldData.coveredSides, 1, 3) : 1;
	public bool IsActivable => ShieldData != null && ShieldData.activeDuration > 0;
	public bool IsUp => m_currentHp > 0;
	public bool IsShownToLocalPlayer => m_user != null && m_user.IsShownToLocalPlayer;

	public override void Init ( Entity _user, ToolEquipmentData _data, string _id )
	{
		base.Init(_user, _data, _id);

		Orientation = _user.Displacement.CurrentOrientation;
		m_currentHp = IsActivable ? 0 : MaxHp;
		m_remainingActiveTicks = 0;
		TurnManager.onNewRoundStart -= OnTick;
		if (IsActivable)
			TurnManager.onNewRoundStart += OnTick;

		if (m_marker == null && m_markerPrefab != null)
			m_marker = Instantiate(m_markerPrefab);
		if (m_marker != null)
			m_marker.Bind(this);
	}

	public bool Covers ( int _incomingSide )
	{
		return IsUp && IsSideCovered(_incomingSide);
	}

	public bool IsSideCovered ( int _side )
	{
		if (_side < 0)
			return false;

		int offset = ((_side - Orientation) % 6 + 6) % 6;
		return offset == 0 || (offset == 1 && CoveredSides >= 2) || (offset == 5 && CoveredSides >= 3);
	}

	public void Activate ()
	{
		if (!IsActivable || m_user == null || m_user.Equipment.IsDead)
			return;

		Orientation = m_user.Displacement.CurrentOrientation;
		m_currentHp = MaxHp;
		m_remainingActiveTicks = ShieldData.activeDuration;
		if (m_marker != null)
			m_marker.Raise();
	}

	private void OnTick ()
	{
		if (m_remainingActiveTicks > 0)
			m_remainingActiveTicks--;
		else
			m_currentHp = 0;
	}

	public int Absorb ( int _damage )
	{
		if (m_currentHp <= 0 || _damage <= 0)
			return 0;

		int absorbed = Mathf.Min(_damage, m_currentHp);
		m_currentHp -= absorbed;
		if (m_marker != null)
			m_marker.RefreshHealth();
		return absorbed;
	}

	public void SetAimed ( bool _isAimed )
	{
		if (m_marker != null)
			m_marker.SetAimed(_isAimed);
	}

	public override void PerformAction ( SpecialAction _specialAction, Action _onPerformEnd )
	{
		if (_specialAction is TurnShieldAction turnAction)
		{
			Orientation = turnAction.targetedOrientation;
			if (m_marker != null)
				m_marker.SlideTo(Orientation, PlayTurnFlash);
			else
				PlayTurnFlash();
		}

		DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
	}

	private void PlayTurnFlash ()
	{
		if (m_turnVFXPool == null || m_user == null || !IsShownToLocalPlayer)
			return;

		Vector3 facing = RotateEntityAction.GetFacingRotation(Orientation) * Vector3.forward;
		Weapon.PlayOrientedVFX(m_turnVFXPool, GroundStrip.OnGround(m_user.Displacement.FeetPosition) + facing * m_turnVFXDistance, facing, ShieldData != null ? ShieldData.name : name);
	}

	private void OnDestroy ()
	{
		TurnManager.onNewRoundStart -= OnTick;
		if (m_marker != null)
			Destroy(m_marker.gameObject);
	}
}
