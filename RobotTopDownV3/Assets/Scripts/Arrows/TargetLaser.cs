using System.Collections.Generic;
using UnityEngine;

public class TargetLaser : PoolElement
{
	private const float c_fallbackHeight = 1.1f;
	private static readonly List<TargetLaser> s_shown = new();
	private static readonly int s_opacityID = Shader.PropertyToID("_Opacity");
	private static readonly int s_dashID = Shader.PropertyToID("_DashLength");
	private static readonly int s_pulseID = Shader.PropertyToID("_PulseSpacing");

	[SerializeField] private LineRenderer m_beam;
	[SerializeField] private Renderer m_dot;
	[SerializeField] private float m_beamWidth = .24f;
	[SerializeField] private float m_startOffset = .45f;
	[SerializeField] private float m_dotSize = .4f;

	[Header("Preview")]
	[SerializeField, Range(0f, 1f)] private float m_previewOpacity = .5f;
	[SerializeField] private float m_previewDash = .22f;
	[SerializeField] private float m_previewDotScale = .7f;

	private MaterialPropertyBlock m_block;
	private int m_fromTileID;
	private int m_targetID;

	public static void HideAll ()
	{
		for (int i = s_shown.Count - 1; i >= 0; i--)
		{
			if (s_shown[i] != null)
				s_shown[i].Discard();
		}
		s_shown.Clear();
	}

	public static void Show ( AEntityAction _action, Entity _target, bool _isPreview )
	{
		Entity caster = _action == null ? null : _action.PerformingEntity;
		GridManager grid = GridManager.Instance;
		if (caster == null || _target == null || _target.Equipment.IsDead || !_target.IsShownToLocalPlayer
			|| grid == null || grid.Tiles == null || _action.supposedPositionAtActionStartID < 0 || _action.supposedPositionAtActionStartID >= grid.Tiles.Length)
			return;

		Tile from = grid.Tiles[_action.supposedPositionAtActionStartID];
		Tile to = _target.Displacement.Coordinates.GetTile();
		if (from == null || to == null || s_shown.Exists(shown => shown.m_fromTileID == _action.supposedPositionAtActionStartID && shown.m_targetID == _target.ID)
			|| ObjectsPooling.GetElement(GameAssets.current.game.targetLaserPoolData, Vector3.zero, Quaternion.identity) is not TargetLaser laser)
			return;

		s_shown.Add(laser);
		laser.m_fromTileID = _action.supposedPositionAtActionStartID;
		laser.m_targetID = _target.ID;
		Tile strikeTile = _action is AttackAction attack ? attack.GetPlannedStrikeTile(from, to) : from;
		Tile shotOrigin = caster.Equipment.GetShotOrigin(_action, from, to);
		laser.Draw(from.transform.position + Vector3.up * HeightOf(caster), shotOrigin != strikeTile ? shotOrigin : null, CenterOf(_target, to), _isPreview);
	}

	private static float HeightOf ( Entity _entity )
	{
		return _entity.Skin != null && _entity.Skin.Center != null ? _entity.Skin.Center.position.y - _entity.transform.position.y : c_fallbackHeight;
	}

	private static Vector3 CenterOf ( Entity _entity, Tile _tile )
	{
		return _entity.Skin != null && _entity.Skin.Center != null ? _entity.Skin.Center.position : _tile.transform.position + Vector3.up * c_fallbackHeight;
	}

	private void Draw ( Vector3 _start, Tile _bendTile, Vector3 _end, bool _isPreview )
	{
		Vector3 firstAim = _end;
		if (_bendTile != null)
		{
			firstAim = _bendTile.transform.position;
			firstAim.y = (_start.y + _end.y) * .5f;
		}

		Vector3 direction = firstAim - _start;
		direction.y = 0f;
		if (direction.sqrMagnitude > .0001f)
			_start += direction.normalized * m_startOffset;

		m_beam.positionCount = _bendTile != null ? 3 : 2;
		m_beam.SetPosition(0, _start);
		if (_bendTile != null)
			m_beam.SetPosition(1, firstAim);
		m_beam.SetPosition(m_beam.positionCount - 1, _end);
		m_beam.widthMultiplier = m_beamWidth;

		m_dot.transform.position = _end;
		m_dot.transform.localScale = Vector3.one * (m_dotSize * (_isPreview ? m_previewDotScale : 1f));

		m_block ??= new MaterialPropertyBlock();
		m_block.Clear();
		if (_isPreview)
		{
			m_block.SetFloat(s_opacityID, m_previewOpacity);
			m_block.SetFloat(s_dashID, m_previewDash);
			m_block.SetFloat(s_pulseID, 0f);
		}
		m_beam.SetPropertyBlock(m_block);
		m_dot.SetPropertyBlock(m_block);
	}

	protected override void OnDestroy ()
	{
		s_shown.Remove(this);
		base.OnDestroy();
	}
}
