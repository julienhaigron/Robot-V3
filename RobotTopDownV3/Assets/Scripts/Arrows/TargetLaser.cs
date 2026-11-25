using System.Collections.Generic;
using UnityEngine;

public class TargetLaser : PoolElement
{
	private const float c_fallbackHeight = 1.1f;
	private const float c_itemHeight = .6f;
	private static readonly List<TargetLaser> s_shown = new();
	private static readonly List<TargetLaser> s_shownAITargets = new();
	private static readonly int s_colorID = Shader.PropertyToID("_Color");
	private static readonly int s_opacityID = Shader.PropertyToID("_Opacity");
	private static readonly int s_dashID = Shader.PropertyToID("_DashLength");
	private static readonly int s_pulseID = Shader.PropertyToID("_PulseSpacing");

	private enum Style { Designated, Preview, AITarget }

	[SerializeField] private LineRenderer m_beam;
	[SerializeField] private Renderer m_dot;
	[SerializeField] private float m_beamWidth = .24f;
	[SerializeField] private float m_startOffset = .45f;
	[SerializeField] private float m_dotSize = .4f;

	[Header("Preview")]
	[SerializeField, Range(0f, 1f)] private float m_previewOpacity = .5f;
	[SerializeField] private float m_previewDash = .22f;
	[SerializeField] private float m_previewDotScale = .7f;

	[Header("AI target")]
	[SerializeField, ColorUsage(false, true)] private Color m_aiTargetColor = new(1.3f, .5f, .04f, 1f);
	[SerializeField, Range(0f, 1f)] private float m_aiTargetOpacity = .85f;
	[SerializeField] private float m_aiTargetDash = .3f;

	private MaterialPropertyBlock m_block;
	private int m_fromTileID;
	private Object m_target;

	public static void HideAll ()
	{
		Discard(s_shown);
	}

	public static void HideAITargets ()
	{
		Discard(s_shownAITargets);
	}

	private static void Discard ( List<TargetLaser> _lasers )
	{
		for (int i = _lasers.Count - 1; i >= 0; i--)
		{
			if (_lasers[i] != null)
				_lasers[i].Discard();
		}
		_lasers.Clear();
	}

	public static void ShowAITarget ( Entity _ally, Entity _target )
	{
		Tile from = _ally.Displacement.Coordinates.GetTile();
		Tile to = _target.Displacement.Coordinates.GetTile();
		if (from == null || to == null
			|| ObjectsPooling.GetElement(GameAssets.current.game.targetLaserPoolData, Vector3.zero, Quaternion.identity) is not TargetLaser laser)
			return;

		s_shownAITargets.Add(laser);
		laser.Draw(from.transform.position + Vector3.up * HeightOf(_ally), null, CenterOf(_target, to), Style.AITarget);
	}

	public static void Show ( AEntityAction _action, Entity _target, bool _isPreview )
	{
		if (_target == null || _target.Equipment.IsDead || !_target.IsShownToLocalPlayer)
			return;

		Tile to = _target.Displacement.Coordinates.GetTile();
		Show(_action, _target, to, CenterOf(_target, to), _isPreview);
	}

	public static void Show ( AEntityAction _action, Item _target, bool _isPreview )
	{
		if (_target == null || _target.Health <= 0 || !_target.IsShownToLocalPlayer || _target.CurrentPosition == null)
			return;

		Tile to = _target.CurrentPosition;
		Show(_action, _target, to, to.transform.position + Vector3.up * c_itemHeight, _isPreview);
	}

	private static void Show ( AEntityAction _action, Object _target, Tile _to, Vector3 _end, bool _isPreview )
	{
		Entity caster = _action == null ? null : _action.PerformingEntity;
		GridManager grid = GridManager.Instance;
		if (caster == null || _to == null || grid == null || grid.Tiles == null
			|| _action.supposedPositionAtActionStartID < 0 || _action.supposedPositionAtActionStartID >= grid.Tiles.Length)
			return;

		Tile from = grid.Tiles[_action.supposedPositionAtActionStartID];
		if (from == null || s_shown.Exists(shown => shown.m_fromTileID == _action.supposedPositionAtActionStartID && shown.m_target == _target)
			|| ObjectsPooling.GetElement(GameAssets.current.game.targetLaserPoolData, Vector3.zero, Quaternion.identity) is not TargetLaser laser)
			return;

		s_shown.Add(laser);
		laser.m_fromTileID = _action.supposedPositionAtActionStartID;
		laser.m_target = _target;
		Tile strikeTile = _action is AttackAction attack ? attack.GetPlannedStrikeTile(from, _to) : from;
		Tile shotOrigin = caster.Equipment.GetShotOrigin(_action, from, _to);
		laser.Draw(from.transform.position + Vector3.up * HeightOf(caster), shotOrigin != strikeTile ? shotOrigin : null, _end, _isPreview ? Style.Preview : Style.Designated);
	}

	private static float HeightOf ( Entity _entity )
	{
		return _entity.Skin != null && _entity.Skin.Center != null ? _entity.Skin.Center.position.y - _entity.transform.position.y : c_fallbackHeight;
	}

	private static Vector3 CenterOf ( Entity _entity, Tile _tile )
	{
		return _entity.Skin != null && _entity.Skin.Center != null ? _entity.Skin.Center.position : _tile.transform.position + Vector3.up * c_fallbackHeight;
	}

	private void Draw ( Vector3 _start, Tile _bendTile, Vector3 _end, Style _style )
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
		m_dot.transform.localScale = Vector3.one * (m_dotSize * (_style == Style.Designated ? 1f : m_previewDotScale));

		m_block ??= new MaterialPropertyBlock();
		m_block.Clear();
		if (_style == Style.Preview)
		{
			m_block.SetFloat(s_opacityID, m_previewOpacity);
			m_block.SetFloat(s_dashID, m_previewDash);
			m_block.SetFloat(s_pulseID, 0f);
		}
		else if (_style == Style.AITarget)
		{
			m_block.SetColor(s_colorID, m_aiTargetColor);
			m_block.SetFloat(s_opacityID, m_aiTargetOpacity);
			m_block.SetFloat(s_dashID, m_aiTargetDash);
			m_block.SetFloat(s_pulseID, 0f);
		}
		m_beam.SetPropertyBlock(m_block);
		m_dot.SetPropertyBlock(m_block);
	}

	protected override void OnDestroy ()
	{
		s_shown.Remove(this);
		s_shownAITargets.Remove(this);
		base.OnDestroy();
	}
}
