using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FacingMarker : GroundDisplay
{
	private static readonly HashSet<FacingMarker> s_bound = new();

	[Header("Shape")]
	[SerializeField] private float m_radius = 1.06f;
	[SerializeField] private float m_headTip = 1.36f;
	[SerializeField] private float m_headScale = .6f;
	[SerializeField] private float m_selectedHeadScale = .68f;
	[SerializeField] private float m_visorHalfSpan = 17f;
	[SerializeField] private float m_sectorHalfSpan = 24f;
	[SerializeField] private float m_sectorHalfWidth = .045f;
	[SerializeField] private float m_aimedSectorHalfWidth = .065f;
	[SerializeField] private float m_ringHalfWidth = .018f;

	[Header("Colours")]
	[SerializeField] private Color m_allyColor = new(.35f, .85f, 1f, 1f);
	[SerializeField] private Color m_enemyColor = new(1f, .35f, .3f, 1f);
	[SerializeField] private Color m_flankColor = new(.94f, .64f, .23f, 1f);
	[SerializeField, Range(0f, 1f)] private float m_idleAlpha = 1f;
	[SerializeField, Range(0f, 1f)] private float m_visorIdleAlpha = .85f;
	[SerializeField, Range(0f, 1f)] private float m_ringAlpha = .5f;
	[SerializeField, Range(0f, 1f)] private float m_lowestFlankAlpha = .3f;
	[SerializeField] private float m_aimPulseSpeed = 7f;

	[Header("Flank labels")]
	[SerializeField] private TextMeshPro[] m_labels;
	[SerializeField] private float m_labelRadius = 1.5f;
	[SerializeField] private float m_aimedLabelScale = 1.3f;

	private Entity m_entity;
	private Renderer[] m_labelRenderers;
	private bool m_isShown = true;
	private bool m_areLabelsShown = true;
	private bool m_isSelected;
	private bool m_isHovered;
	private int m_aimedSide = -1;
	private Color m_teamColor;
	private int m_drawnKey = -1;

	private bool IsCrownShown => m_isHovered || m_aimedSide >= 0;

	protected override void Awake ()
	{
		base.Awake();
		m_labelRenderers = new Renderer[m_labels.Length];
		for (int i = 0; i < m_labels.Length; i++)
			m_labelRenderers[i] = m_labels[i].GetComponent<Renderer>();

		PlayerController.onEntityHovered += OnEntityHovered;
	}

	protected override void OnDestroy ()
	{
		PlayerController.onEntityHovered -= OnEntityHovered;
		Unbind();
		base.OnDestroy();
	}

	public static void ClearAim ()
	{
		foreach (FacingMarker marker in s_bound)
			marker.m_aimedSide = -1;
	}

	public static void Aim ( Entity _target, int _incomingSide )
	{
		if (_target == null || _incomingSide < 0)
			return;

		foreach (FacingMarker marker in s_bound)
		{
			if (marker.m_entity == _target)
				marker.m_aimedSide = (_incomingSide - _target.Displacement.CurrentOrientation + 6) % 6;
		}
	}

	public void Bind ( Entity _entity )
	{
		Unbind();
		m_entity = _entity;
		m_entity.onSelect += OnSelected;
		m_entity.onDeselect += OnDeselected;
		s_bound.Add(this);
		m_isSelected = false;
		m_isHovered = false;
		m_aimedSide = -1;
		m_teamColor = Color.clear;
		m_drawnKey = -1;
	}

	private void Unbind ()
	{
		s_bound.Remove(this);
		if (m_entity == null)
			return;

		m_entity.onSelect -= OnSelected;
		m_entity.onDeselect -= OnDeselected;
		m_entity = null;
	}

	private void OnSelected ()
	{
		m_isSelected = true;
	}

	private void OnDeselected ()
	{
		m_isSelected = false;
	}

	private void OnEntityHovered ( int? _entityID )
	{
		m_isHovered = m_entity != null && _entityID == m_entity.ID;
	}

	private void LateUpdate ()
	{
		bool isVisible = m_entity != null && !m_entity.Equipment.IsDead && IsOnGrid(m_entity) && m_entity.IsShownToLocalPlayer;
		SetShown(isVisible);
		if (!isVisible)
			return;

		transform.SetPositionAndRotation(m_entity.Displacement.FeetPosition, Quaternion.Euler(0f, m_entity.SkinParent.transform.eulerAngles.y, 0f));

		Color team = m_entity.IsAlliedTo(GameManager.Instance.PlayerID) ? m_allyColor : m_enemyColor;
		if (team != m_teamColor)
		{
			m_teamColor = team;
			SetStyle(team, false);
			m_drawnKey = -1;
		}

		int key = (m_isSelected ? 1 : 0) | (IsCrownShown ? 2 : 0) | ((m_aimedSide + 1) << 2);
		if (key != m_drawnKey)
		{
			m_drawnKey = key;
			Redraw(true);
		}
		else if (m_aimedSide >= 0)
			Redraw(false);

		if (m_areLabelsShown)
		{
			Quaternion flat = Quaternion.Euler(90f, BillboardManager.TargetRot.eulerAngles.y, 0f);
			foreach (TextMeshPro label in m_labels)
				label.transform.rotation = flat;
		}
	}

	private void Redraw ( bool _isLayoutChanged )
	{
		Vector3 centre = transform.position;
		float yaw = transform.eulerAngles.y;
		bool isCrown = IsCrownShown;

		Strip.Clear();
		if (m_isSelected)
			Strip.AddArc(centre, m_radius, yaw, yaw + 360f, m_ringHalfWidth, WithAlpha(m_teamColor, m_ringAlpha));

		if (isCrown)
			AddCrown(centre, yaw, _isLayoutChanged);
		else
			Strip.AddArc(centre, m_radius, yaw - m_visorHalfSpan, yaw + m_visorHalfSpan, StripHalfWidth, WithAlpha(m_teamColor, m_isSelected ? 1f : m_visorIdleAlpha));
		Strip.Apply();

		if (!_isLayoutChanged)
			return;

		SetLabelsShown(isCrown);
		Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
		Head.transform.localScale = Vector3.one * (m_isSelected ? m_selectedHeadScale : m_headScale);
		PlaceFlat(Head, centre + forward * m_headTip, forward, WithAlpha(m_teamColor, m_isSelected || isCrown ? 1f : m_idleAlpha));
	}

	private void AddCrown ( Vector3 _centre, float _yaw, bool _isLayoutChanged )
	{
		SerializableDictionary<Tile.TileDirectionType, float> bonuses = GameConfig.current.game.entityFlankRatio;
		float maxBonus = 0f;
		foreach (float bonus in bonuses.Values)
			maxBonus = Mathf.Max(maxBonus, bonus);

		float pulse = .5f + .5f * Mathf.Sin(Time.time * m_aimPulseSpeed);
		for (int side = 0; side < 6; side++)
		{
			float bonus = bonuses[GridManager.GetSideFromDelta(side)];
			bool isAimed = side == m_aimedSide;
			Color color = side == 0 ? m_teamColor : WithAlpha(m_flankColor, Mathf.Lerp(m_lowestFlankAlpha, 1f, maxBonus > 0f ? bonus / maxBonus : 0f));
			if (isAimed)
			{
				color.a = 1f;
				color = Color.Lerp(color, Color.white, .35f * pulse);
			}

			float sideYaw = _yaw + side * 60f;
			Strip.AddArc(_centre, m_radius, sideYaw - m_sectorHalfSpan, sideYaw + m_sectorHalfSpan, isAimed ? m_aimedSectorHalfWidth : m_sectorHalfWidth, color);

			if (side == 0 || !_isLayoutChanged)
				continue;

			TextMeshPro label = m_labels[side - 1];
			label.transform.position = OnGround(_centre + Quaternion.Euler(0f, sideYaw, 0f) * Vector3.forward * m_labelRadius);
			label.transform.localScale = Vector3.one * (isAimed ? m_aimedLabelScale : 1f);
			label.color = m_flankColor;
			label.SetText("+" + bonus.ToPercentText());
		}
	}

	private void SetShown ( bool _isShown )
	{
		if (_isShown == m_isShown)
			return;

		m_isShown = _isShown;
		StripRenderer.enabled = _isShown;
		Head.enabled = _isShown;
		m_drawnKey = -1;
		if (!_isShown)
			SetLabelsShown(false);
	}

	private void SetLabelsShown ( bool _isShown )
	{
		if (_isShown == m_areLabelsShown)
			return;

		m_areLabelsShown = _isShown;
		foreach (Renderer labelRenderer in m_labelRenderers)
			labelRenderer.enabled = _isShown;
	}

	private static Color WithAlpha ( Color _color, float _alpha )
	{
		_color.a *= _alpha;
		return _color;
	}
}
