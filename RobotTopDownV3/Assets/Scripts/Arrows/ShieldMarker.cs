using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShieldMarker : GroundDisplay
{
	private static readonly HashSet<ShieldMarker> s_bound = new();

	[Header("Shape")]
	[SerializeField] private float m_radius = .94f;
	[SerializeField] private float m_innerGap = 11f;
	[SerializeField] private float m_halfSpan = 29f;
	[SerializeField, Range(0f, 1f)] private float m_emptyAlpha = .28f;
	[SerializeField] private TextMeshPro m_number;
	[SerializeField] private Color m_numberColor = new(.04f, .05f, .09f, 1f);
	[SerializeField] private Color m_allyColor = new(.35f, .85f, 1f, 1f);
	[SerializeField] private Color m_enemyColor = new(1f, .35f, .3f, 1f);

	[Header("Feedback")]
	[SerializeField] private SpriteRenderer[] m_shards;
	[SerializeField] private float m_slideDuration = .35f;
	[SerializeField] private float m_drainDuration = .35f;
	[SerializeField] private float m_flashDuration = .3f;
	[SerializeField] private float m_breakDuration = .6f;
	[SerializeField] private float m_aimPulseSpeed = 7f;

	private Shield m_shield;
	private Renderer m_numberRenderer;
	private Color m_teamColor;
	private bool m_isShown = true;
	private bool m_areShardsFlying = true;
	private bool m_isAimed;
	private float m_yaw;
	private float m_slideFromYaw;
	private float m_slideToYaw;
	private float m_slideStart = -1f;
	private Action m_onSlideEnd;
	private float m_fill = 1f;
	private float m_drainFrom = 1f;
	private float m_drainStart = -1f;
	private float m_flashStart = -1f;
	private float m_breakStart = -1f;
	private float m_drawnFill = -1f;
	private Color m_drawnColor;
	private int m_label = int.MinValue;

	private float HealthShare => m_shield != null && m_shield.MaxHp > 0 ? m_shield.CurrentHp / (float)m_shield.MaxHp : 0f;

	protected override void Awake ()
	{
		base.Awake();
		m_numberRenderer = m_number.GetComponent<Renderer>();
	}

	protected override void OnDestroy ()
	{
		s_bound.Remove(this);
		base.OnDestroy();
	}

	public static void ClearAim ()
	{
		foreach (ShieldMarker marker in s_bound)
			marker.m_isAimed = false;
	}

	public static void ShowPlanned ( Shield _shield, Tile _pivot, int _fromOrientation, int _toOrientation, int _tick, bool _isPreview )
	{
		if (_shield == null || _pivot == null || _toOrientation < 0
			|| ObjectsPooling.GetElement(GameAssets.current.game.shieldMarkerPoolData, _pivot.transform.position, RotateEntityAction.GetFacingRotation(_toOrientation)) is not ShieldMarker ghost)
			return;

		int entityID = _shield.LinkedEntity.ID;
		Color color = ghost.TeamColorOf(_shield);
		ghost.ShowGhost(color, _tick);
		PlayerController.Instance.AddOrderDisplay(ghost, entityID);
		if (_fromOrientation >= 0 && _fromOrientation != _toOrientation)
			RotationActionDisplay.ShowShieldTurn(entityID, _pivot, _fromOrientation, _toOrientation, color, _isPreview);
	}

	public void Bind ( Shield _shield )
	{
		m_shield = _shield;
		s_bound.Add(this);
		m_yaw = RotateEntityAction.GetFacingYaw(_shield.orientation);
		m_slideStart = m_drainStart = m_flashStart = m_breakStart = -1f;
		m_onSlideEnd = null;
		m_fill = m_drainFrom = HealthShare;
		m_drawnFill = -1f;
		m_isAimed = false;
		m_teamColor = Color.clear;
		AnimateShards(1f);
	}

	public void SetAimed ( bool _isAimed )
	{
		m_isAimed = _isAimed;
	}

	public void SlideTo ( int _orientation, Action _onArrived )
	{
		Action pending = m_onSlideEnd;
		m_onSlideEnd = null;
		pending?.Invoke();

		m_slideFromYaw = m_yaw;
		m_slideToYaw = RotateEntityAction.GetFacingYaw(_orientation);
		if (!m_isShown)
		{
			m_yaw = m_slideToYaw;
			m_slideStart = -1f;
			_onArrived?.Invoke();
			return;
		}

		m_slideStart = Time.time;
		m_onSlideEnd = _onArrived;
	}

	public void RefreshHealth ()
	{
		m_drainFrom = m_fill;
		m_drainStart = Time.time;
		m_flashStart = Time.time;
		if (m_shield.CurrentHp <= 0)
			m_breakStart = Time.time;
	}

	private Color TeamColorOf ( Shield _shield )
	{
		return _shield.LinkedEntity.IsAlliedTo(GameManager.Instance.PlayerID) ? m_allyColor : m_enemyColor;
	}

	private void ShowGhost ( Color _color, int _tick )
	{
		m_shield = null;
		m_teamColor = _color;
		SetStyle(_color, true);
		SetShown(true);
		AnimateShards(1f);
		SetLabel(_tick);
		Redraw(_color, 1f);
		m_number.transform.rotation = BillboardManager.TargetRot;
	}

	private void LateUpdate ()
	{
		if (m_shield == null)
			return;

		Entity owner = m_shield.LinkedEntity;
		bool isIntact = m_shield.CurrentHp > 0;
		bool isVisible = owner != null && !owner.Equipment.IsDead && IsOnGrid(owner) && m_shield.IsShownToLocalPlayer;
		float breakProgress = m_breakStart < 0f ? 1f : (Time.time - m_breakStart) / m_breakDuration;
		SetShown(isVisible && isIntact);
		AnimateShards(isVisible && !isIntact ? breakProgress : 1f);
		if (!isVisible || (!isIntact && breakProgress >= 1f))
			return;

		transform.SetPositionAndRotation(owner.Displacement.FeetPosition, Quaternion.Euler(0f, UpdateYaw(), 0f));
		if (!isIntact)
			return;

		Color team = TeamColorOf(m_shield);
		if (team != m_teamColor)
		{
			m_teamColor = team;
			SetStyle(team, false);
			m_drawnFill = -1f;
		}

		Redraw(Color.Lerp(team, Color.white, Glow()), UpdateFill());
		SetLabel(m_shield.CurrentHp);
		m_number.transform.rotation = BillboardManager.TargetRot;
	}

	private static bool IsOnGrid ( Entity _owner )
	{
		GridManager grid = GridManager.Instance;
		int tileID = _owner.Displacement.Coordinates.ID;
		return grid != null && grid.Tiles != null && tileID >= 0 && tileID < grid.Tiles.Length;
	}

	private float UpdateYaw ()
	{
		if (m_slideStart < 0f)
			return m_yaw;

		float progress = (Time.time - m_slideStart) / m_slideDuration;
		if (progress < 1f)
			return m_yaw = Mathf.LerpAngle(m_slideFromYaw, m_slideToYaw, Mathf.SmoothStep(0f, 1f, progress));

		m_yaw = m_slideToYaw;
		m_slideStart = -1f;
		m_flashStart = Time.time;
		Action arrived = m_onSlideEnd;
		m_onSlideEnd = null;
		arrived?.Invoke();
		return m_yaw;
	}

	private float UpdateFill ()
	{
		float target = HealthShare;
		if (m_drainStart < 0f)
			return m_fill = target;

		float progress = (Time.time - m_drainStart) / m_drainDuration;
		if (progress >= 1f)
			m_drainStart = -1f;
		return m_fill = Mathf.Lerp(m_drainFrom, target, Mathf.SmoothStep(0f, 1f, Mathf.Min(progress, 1f)));
	}

	private float Glow ()
	{
		float flash = m_flashStart < 0f ? 0f : 1f - (Time.time - m_flashStart) / m_flashDuration;
		if (flash <= 0f)
			m_flashStart = -1f;

		float aim = m_isAimed ? .25f + .25f * Mathf.Sin(Time.time * m_aimPulseSpeed) : 0f;
		return Mathf.Max(flash * .65f, aim);
	}

	private void Redraw ( Color _color, float _fill )
	{
		Color empty = _color;
		empty.a *= m_emptyAlpha;
		Head.color = Faded(_fill > 0f ? _color : empty);
		m_number.color = Faded(m_numberColor);
		if (_color == m_drawnColor && Mathf.Approximately(_fill, m_drawnFill))
			return;

		m_drawnColor = _color;
		m_drawnFill = _fill;
		Vector3 centre = transform.position;
		float facing = transform.eulerAngles.y;
		float reach = (m_halfSpan - m_innerGap) * Mathf.Clamp01(_fill);
		Strip.Clear();
		for (int side = -1; side <= 1; side += 2)
		{
			float inner = facing + side * m_innerGap;
			float split = inner + side * reach;
			float outer = facing + side * m_halfSpan;
			if (reach > .01f)
				Strip.AddArc(centre, m_radius, inner, split, StripHalfWidth, _color);
			if (Mathf.Abs(outer - split) > .01f)
				Strip.AddArc(centre, m_radius, split, outer, StripHalfWidth, empty);
		}
		Strip.Apply();
	}

	private void SetShown ( bool _isShown )
	{
		if (_isShown == m_isShown)
			return;

		m_isShown = _isShown;
		StripRenderer.enabled = _isShown;
		Head.enabled = _isShown;
		m_numberRenderer.enabled = _isShown;
	}

	private void SetLabel ( int _value )
	{
		if (_value == m_label)
			return;

		m_label = _value;
		m_number.SetText("{0}", _value);
	}

	private void AnimateShards ( float _progress )
	{
		bool isFlying = _progress < 1f;
		if (!isFlying && !m_areShardsFlying)
			return;

		m_areShardsFlying = isFlying;
		for (int i = 0; i < m_shards.Length; i++)
		{
			SpriteRenderer shard = m_shards[i];
			shard.enabled = isFlying;
			if (!isFlying)
				continue;

			float spread = m_shards.Length > 1 ? (i / (float)(m_shards.Length - 1) - .5f) * 2f * m_halfSpan : 0f;
			float spin = (i % 2 == 0 ? 540f : -540f) * _progress;
			Vector3 direction = Quaternion.Euler(0f, spread, 0f) * Vector3.forward;
			shard.transform.localPosition = direction * (m_radius + _progress * (.45f + .15f * (i % 3))) + Vector3.up * GroundStrip.c_groundHeight;
			shard.transform.localRotation = Quaternion.Euler(90f, spread + spin, 0f);
			Color color = m_teamColor;
			color.a *= 1f - _progress;
			shard.color = color;
		}
	}
}
