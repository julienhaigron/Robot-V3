using System.Collections.Generic;
using UnityEngine;

public static class AISpecialEvaluator
{
	public struct SpecialChoice
	{
		public Tile targetTile;
		public List<Entity> affectedEnemies;
		public int shieldOrientation;
		public float score;
	}

	public struct Context
	{
		public Entity caster;
		public Tile casterTile;
		public HashSet<Entity> visibleEnemies;
		public MatchStrategy strategy;
		public Entity focus;
		public Tile orderDestination;
	}

	public const float c_minScore = 1f;
	public const float c_overAttackScore = 2f;

	private const float c_shieldTurnScore = 1f;
	private const float c_allyPenaltyFactor = 3f;
	private const float c_alreadyAppliedFactor = 0.2f;
	private const float c_pushValue = 0.6f;
	private const float c_collisionValue = 0.3f;
	private const float c_lethalValue = 5f;
	private const float c_strategyBonus = 1.5f;
	private const float c_crowdBonus = 1.2f;
	private const int c_packThreatRange = 2;
	private const float c_blastHitValue = 1.5f;
	private const float c_blastKillValue = 3f;
	private const float c_selfLossCost = 2.5f;
	private const float c_lowHealthSelfLossCost = 1f;
	private const float c_lowHealthRatio = 0.3f;
	private const float c_blockedLineValue = 0.6f;
	private const int c_smokeCoverRadius = 2;
	private const float c_mineValue = 2f;
	private const float c_buffValuePerUnit = 0.6f;

	public static bool IsUsableSpecial ( EntityActionData _data )
	{
		if (_data == null || _data.isModAction || _data.IsRotation || _data.GetMainActionType() != EntityActionData.MainActionType.Special)
			return false;

		if (_data.codeType == EntityActionData.ActionCodeType.TurnShield || IsSelfDestruct(_data))
			return true;

		if (_data.codeType == EntityActionData.ActionCodeType.InvokeItem)
			return (_data.invocatedItem is MineItemData mine && GetDamageSum(mine.damages) > 0)
				|| (_data.invocatedItem is ApplyEffectItemData emitter && emitter.type == ApplyEffectItemData.EffectType.StatBuff);

		return _data.codeType == EntityActionData.ActionCodeType.ApplyEffect
			&& _data.aoeType != EntityActionData.AOEType.Noone
			&& (_data.targetType == EntityActionData.TargetType.Tile || _data.targetType == EntityActionData.TargetType.Self)
			&& _data.passiveEffects != null && _data.passiveEffects.Length > 0;
	}

	public static bool TryGetBestUse ( Context _context, AEntityAction _action, out SpecialChoice _choice )
	{
		_choice = default;
		if (_context.casterTile == null)
			return false;

		EntityActionData data = _action.Data;
		if (data.codeType == EntityActionData.ActionCodeType.TurnShield)
			return TryGetShieldTurn(_context, _action, out _choice);

		if (IsSelfDestruct(data))
			return TryGetSelfDestruct(_context, out _choice);

		if (data.codeType == EntityActionData.ActionCodeType.InvokeItem)
			return TryGetItemPlacement(_context, _action, out _choice);

		if (data.targetType == EntityActionData.TargetType.Self)
		{
			float selfScore = EvaluateCenter(_context, _action, _context.casterTile, out List<Entity> selfAffected);
			_choice = new SpecialChoice { targetTile = _context.casterTile, affectedEnemies = selfAffected, score = selfScore };
			return selfAffected.Count > 0 && selfScore > 0f;
		}

		if (IsVisionBlocker(data))
			return TryGetSmokeCenter(_context, _action, out _choice);

		int aoeRange = Mathf.Max(0, data.GetAoEMaxRange(_action, _context.caster, null));
		HashSet<Tile> centers = new();
		foreach (Entity enemy in _context.visibleEnemies)
		{
			Tile enemyTile = enemy.Displacement.Coordinates.GetTile();
			if (enemyTile != null)
				centers.UnionWith(GridManager.Instance.GetTilesInVisionRange(enemyTile, 0, aoeRange, true, true, false));
		}

		bool hasChoice = false;
		foreach (Tile center in centers)
		{
			if (!IsInThrowRange(_context, _action, center))
				continue;

			float score = EvaluateCenter(_context, _action, center, out List<Entity> affected);
			if (affected.Count == 0 || (hasChoice && score <= _choice.score))
				continue;

			_choice = new SpecialChoice { targetTile = center, affectedEnemies = affected, score = score };
			hasChoice = true;
		}

		return hasChoice && _choice.score > 0f;
	}

	private static bool IsInThrowRange ( Context _context, AEntityAction _action, Tile _tile )
	{
		int distance = _context.casterTile.coordinates.DistanceTo(_tile.coordinates);
		return distance >= _action.Data.minDistance && distance <= _action.Data.GetMaxRange(_action, _context.caster, null)
			&& GridManager.Instance.IsInLineOfSight(_context.casterTile, _tile);
	}

	private static float EvaluateCenter ( Context _context, AEntityAction _action, Tile _center, out List<Entity> _affectedEnemies )
	{
		_affectedEnemies = new List<Entity>();
		float score = 0f;

		foreach (Tile tile in _context.caster.Equipment.GetTilesInAoERange(_action, _center))
		{
			Entity entity = tile.GetCurrentEntity();
			if (entity == null || entity.Equipment.IsDead || entity.IsImmortal)
				continue;

			bool isAlly = entity.IsAlliedTo(_context.caster.OwnerID);
			if (!isAlly && !_context.visibleEnemies.Contains(entity))
				continue;

			float value = 0f;
			foreach (AEntityPassiveEffect.PassiveEffectContainer container in _action.Data.passiveEffects)
			{
				if (container.targetType != EntityActionData.TargetType.Self)
					value += GetEffectValue(_context, container, entity, _center, isAlly);
			}

			if (isAlly && value >= c_lethalValue)
			{
				_affectedEnemies.Clear();
				return float.MinValue;
			}

			if (isAlly)
				score -= Mathf.Abs(value) * c_allyPenaltyFactor;
			else if (value > 0f)
			{
				score += value;
				_affectedEnemies.Add(entity);
			}
		}

		if (_context.strategy == MatchStrategy.Encerclement && _affectedEnemies.Count >= 2)
			score *= c_crowdBonus;

		return score;
	}

	private static float GetEffectValue ( Context _context, AEntityPassiveEffect.PassiveEffectContainer _container, Entity _target, Tile _center, bool _isAlly )
	{
		if (!GameAssets.current.game.entityEffects.TryGetValue(_container.enumID, out AEntityPassiveEffect effect))
			return 0f;

		switch (effect)
		{
			case ApplyStatusPassiveEffect statusEffect:
				float statusValue = GetStatusValue(statusEffect.statusApplied);
				if (_target.Status.Contains(statusEffect.statusApplied))
					statusValue *= c_alreadyAppliedFactor;
				return _isAlly ? statusValue : statusValue * GetStatusStrategyFactor(_context, statusEffect.statusApplied, _target);

			case PushOrPullPassiveEffect pushEffect:
				float pushValue = GetPushValue(pushEffect.PredictPush(_target, _center, _context.casterTile), _target);
				return _isAlly ? pushValue : pushValue * GetControlStrategyFactor(_context, _target);

			case InstantDeathPassiveEffect:
				return c_lethalValue;

			default:
				return 0f;
		}
	}

	private static float GetPushValue ( PushOrPullPassiveEffect.PushPrediction _push, Entity _target )
	{
		if (_push.fallsIntoVoid)
			return c_lethalValue;

		if (_push.didCollide)
			return c_pushValue + c_collisionValue;

		return _push.destination != _target.Displacement.Coordinates.GetTile() ? c_pushValue : 0f;
	}

	private static float GetStatusValue ( EntityStatusEnumID _status )
	{
		switch (_status)
		{
			case EntityStatusEnumID.Doom:
				return 3f;
			case EntityStatusEnumID.Stun:
				return 2f;
			case EntityStatusEnumID.Burn1:
			case EntityStatusEnumID.Rooted:
				return 1f;
			case EntityStatusEnumID.Marked:
				return 0.6f;
			default:
				return 0f;
		}
	}

	private static float GetStatusStrategyFactor ( Context _context, EntityStatusEnumID _status, Entity _target )
	{
		if (_status == EntityStatusEnumID.Burn1)
			return _context.strategy == MatchStrategy.Encerclement ? c_strategyBonus : 1f;

		if (_status == EntityStatusEnumID.Marked)
			return _context.strategy == MatchStrategy.Isolation && _target == _context.focus ? 2f : 1f;

		if (_status == EntityStatusEnumID.Stun || _status == EntityStatusEnumID.Rooted)
			return GetControlStrategyFactor(_context, _target);

		return 1f;
	}

	private static float GetControlStrategyFactor ( Context _context, Entity _target )
	{
		switch (_context.strategy)
		{
			case MatchStrategy.Isolation:
				return _context.focus != null && _target != _context.focus ? c_strategyBonus : 1f;
			case MatchStrategy.Groupement:
				return IsWithinRangeOfAlly(_context.caster.OwnerID, _target.Displacement.Coordinates.GetTile(), c_packThreatRange) ? c_strategyBonus : 1f;
			default:
				return 1f;
		}
	}

	private static bool IsWithinRangeOfAlly ( int _ownerID, Tile _tile, int _range, Entity _ignored = null )
	{
		if (_tile == null)
			return false;

		foreach (Entity ally in GameManager.Instance.PlayersEntityAnchor[_ownerID].Entities)
		{
			if (ally == null || ally == _ignored || ally.Equipment.IsDead)
				continue;

			Tile allyTile = ally.Displacement.Coordinates.GetTile();
			if (allyTile != null && allyTile.coordinates.DistanceTo(_tile.coordinates) <= _range)
				return true;
		}

		return false;
	}

	#region Smoke

	private static bool IsVisionBlocker ( EntityActionData _data )
	{
		if (!_data.doesAffectTile || _data.passiveEffects == null)
			return false;

		foreach (AEntityPassiveEffect.PassiveEffectContainer container in _data.passiveEffects)
		{
			if (GameAssets.current.game.entityEffects.TryGetValue(container.enumID, out AEntityPassiveEffect effect)
				&& effect is ApplyStatusPassiveEffect statusEffect
				&& GameAssets.current.game.entityStatus.TryGetValue(statusEffect.statusApplied, out AEntityStatus status)
				&& status is SmokedStatus)
				return true;
		}

		return false;
	}

	private struct FireLine
	{
		public Entity enemy;
		public Tile enemyTile;
		public Tile allyTile;
		public bool canAllyShootBack;
	}

	private static bool TryGetSmokeCenter ( Context _context, AEntityAction _action, out SpecialChoice _choice )
	{
		_choice = default;
		List<FireLine> lines = GetThreateningFireLines(_context);
		if (lines.Count == 0)
			return false;

		HashSet<Tile> centers = new();
		foreach (FireLine line in lines)
			centers.UnionWith(GridManager.Instance.GetTilesInVisionRange(line.allyTile, 0, c_smokeCoverRadius, true, true, false));

		bool hasChoice = false;
		foreach (Tile center in centers)
		{
			if (!IsInThrowRange(_context, _action, center))
				continue;

			HashSet<Tile> cloud = new(_context.caster.Equipment.GetTilesInAoERange(_action, center));
			List<Entity> blinded = new();
			float score = 0f;
			foreach (FireLine line in lines)
			{
				if (GridManager.Instance.IsInLineOfSight(line.enemyTile, line.allyTile, cloud))
					continue;

				score += line.canAllyShootBack ? 0f : c_blockedLineValue;
				if (!blinded.Contains(line.enemy))
					blinded.Add(line.enemy);
			}

			if (blinded.Count == 0 || (hasChoice && score <= _choice.score))
				continue;

			_choice = new SpecialChoice { targetTile = center, affectedEnemies = blinded, score = score };
			hasChoice = true;
		}

		return hasChoice && _choice.score > 0f;
	}

	private static List<FireLine> GetThreateningFireLines ( Context _context )
	{
		List<FireLine> lines = new();
		List<KeyValuePair<Tile, int>> allies = new();
		foreach (Entity ally in GameManager.Instance.PlayersEntityAnchor[_context.caster.OwnerID].Entities)
		{
			Tile allyTile = ally == null || ally.Equipment.IsDead ? null : ally.Displacement.Coordinates.GetTile();
			if (allyTile != null)
				allies.Add(new(allyTile, ally.AI.TryGetMainWeaponRange(out _, out int allyRange) ? allyRange : 0));
		}

		foreach (Entity enemy in _context.visibleEnemies)
		{
			Tile enemyTile = enemy.Displacement.Coordinates.GetTile();
			if (enemyTile == null || !enemy.AI.TryGetMainWeaponRange(out _, out int enemyRange))
				continue;

			foreach (KeyValuePair<Tile, int> ally in allies)
			{
				int distance = enemyTile.coordinates.DistanceTo(ally.Key.coordinates);
				if (distance <= 1 || distance > enemyRange || !GridManager.Instance.IsInLineOfSight(enemyTile, ally.Key))
					continue;

				lines.Add(new FireLine
				{
					enemy = enemy,
					enemyTile = enemyTile,
					allyTile = ally.Key,
					canAllyShootBack = distance <= ally.Value
				});
			}
		}

		return lines;
	}

	#endregion

	#region Items

	private static bool TryGetItemPlacement ( Context _context, AEntityAction _action, out SpecialChoice _choice )
	{
		_choice = default;
		AItemData item = _action.Data.invocatedItem;
		if (item == null || string.IsNullOrEmpty(_action.linkedEquipmentId)
			|| !_context.caster.Equipment.Tools.TryGetValue(_action.linkedEquipmentId, out ToolEquipment tool)
			|| !item.InvokeItemPredicate(tool, _action.Data))
			return false;

		int minRange = _action.Data.minDistance;
		int maxRange = _action.Data.GetMaxRange(_action, _context.caster, null);
		List<Tile> candidates = new();
		foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(_context.casterTile, minRange, maxRange, false, true, true))
		{
			if (!tile.IsObstacle(true) && tile.GetItem(true) == null && tile.GetPlannedItemAt(_action.timeAtStart) == null
				&& tile.GetCurrentEntity() == null && item.CanBePlacedOn(tile))
				candidates.Add(tile);
		}

		if (candidates.Count == 0)
			return false;

		if (item is MineItemData mine)
			return TryGetMinePlacement(_context, mine, candidates, out _choice);

		if (item is ApplyEffectItemData emitter)
			return TryGetBuffPlacement(_context, emitter, candidates, out _choice);

		return false;
	}

	private static bool TryGetMinePlacement ( Context _context, MineItemData _mine, List<Tile> _candidates, out SpecialChoice _choice )
	{
		_choice = default;
		if (_context.orderDestination == null)
			return false;

		int damage = GetDamageSum(_mine.damages);
		List<KeyValuePair<Entity, HashSet<Tile>>> approaches = new();
		foreach (Entity enemy in _context.visibleEnemies)
		{
			List<Tile> path = GridManager.Instance.GetPath(enemy.Displacement.Coordinates.GetTile(), _context.casterTile, true, _movingEntity: enemy, _canTraverseAllies: true);
			if (path != null && path.Count > 2)
				approaches.Add(new(enemy, new HashSet<Tile>(path)));
		}

		bool hasChoice = false;
		foreach (Tile tile in _candidates)
		{
			if (tile.coordinates.DistanceTo(_context.orderDestination.coordinates) <= _mine.range
				|| IsWithinRangeOfAlly(_context.caster.OwnerID, tile, _mine.range, _context.caster))
				continue;

			List<Entity> affected = new();
			float score = 0f;
			foreach (KeyValuePair<Entity, HashSet<Tile>> approach in approaches)
			{
				if (!approach.Value.Contains(tile))
					continue;

				score += Mathf.Min(1f, damage / (float)Mathf.Max(1, approach.Key.Equipment.CurrentHealth)) * c_mineValue;
				affected.Add(approach.Key);
			}

			if (affected.Count == 0 || (hasChoice && score <= _choice.score))
				continue;

			_choice = new SpecialChoice { targetTile = tile, affectedEnemies = affected, score = score };
			hasChoice = true;
		}

		return hasChoice;
	}

	private static bool TryGetBuffPlacement ( Context _context, ApplyEffectItemData _emitter, List<Tile> _candidates, out SpecialChoice _choice )
	{
		_choice = default;
		HashSet<Tile> walkway = new();
		if (_context.orderDestination != null && _context.orderDestination != _context.casterTile)
		{
			List<Tile> path = GridManager.Instance.GetPath(_context.casterTile, _context.orderDestination, true, _movingEntity: _context.caster, _canTraverseAllies: true);
			if (path != null)
				walkway.UnionWith(path);
		}

		bool hasChoice = false;
		foreach (Tile tile in _candidates)
		{
			if (walkway.Contains(tile))
				continue;

			int balance = 0;
			List<Entity> enemiesInRange = new();
			foreach (Entity entity in GridManager.Instance.GetEntitiesInRange(tile, _emitter.range))
			{
				if (entity.IsImmortal)
					continue;

				if (entity.IsAlliedTo(_context.caster.OwnerID))
					balance++;
				else if (_context.visibleEnemies.Contains(entity))
				{
					balance--;
					enemiesInRange.Add(entity);
				}
			}

			float score = balance * c_buffValuePerUnit;
			if (hasChoice && score <= _choice.score)
				continue;

			_choice = new SpecialChoice { targetTile = tile, affectedEnemies = enemiesInRange, score = score };
			hasChoice = true;
		}

		return hasChoice && _choice.score > 0f;
	}

	private static int GetDamageSum ( Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		int sum = 0;
		if (_damages != null)
		{
			foreach (int amount in _damages.Values)
				sum += amount;
		}

		return sum;
	}

	#endregion

	#region Self

	private static bool IsSelfDestruct ( EntityActionData _data )
	{
		if (_data.targetType != EntityActionData.TargetType.Self || _data.passiveEffects == null)
			return false;

		foreach (AEntityPassiveEffect.PassiveEffectContainer container in _data.passiveEffects)
		{
			if (container.targetType == EntityActionData.TargetType.Self
				&& GameAssets.current.game.entityEffects.TryGetValue(container.enumID, out AEntityPassiveEffect effect)
				&& effect is InstantDeathPassiveEffect)
				return true;
		}

		return false;
	}

	private static bool TryGetSelfDestruct ( Context _context, out SpecialChoice _choice )
	{
		_choice = default;
		Entity caster = _context.caster;
		List<Entity> affected = new();
		float score = 0f;

		foreach (AEntityPassiveEffect.PassiveEffectContainer container in caster.AllPassiveEffects)
		{
			if (!GameAssets.current.game.entityEffects.TryGetValue(container.enumID, out AEntityPassiveEffect effect) || effect is not DamageOnDeathPassiveEffect blast)
				continue;

			int damage = GetDamageSum(blast.damages);
			foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(_context.casterTile, 1, blast.explosionRange, false, true, true))
			{
				Entity entity = tile.GetCurrentEntity();
				if (entity == null || entity == caster || entity.Equipment.IsDead || entity.IsImmortal)
					continue;

				bool isAlly = entity.IsAlliedTo(caster.OwnerID);
				if (!isAlly && !_context.visibleEnemies.Contains(entity))
					continue;

				float value = damage >= entity.Equipment.CurrentHealth ? c_blastKillValue : c_blastHitValue;
				if (isAlly)
				{
					score -= value * c_allyPenaltyFactor;
					continue;
				}

				score += value;
				if (!affected.Contains(entity))
					affected.Add(entity);
			}
		}

		if (affected.Count == 0)
			return false;

		bool isLowHealth = caster.Equipment.CurrentHealth < caster.Equipment.MaxHealth * c_lowHealthRatio;
		score -= isLowHealth ? c_lowHealthSelfLossCost : c_selfLossCost;
		_choice = new SpecialChoice { targetTile = _context.casterTile, affectedEnemies = affected, score = score };
		return score > 0f;
	}

	private static bool TryGetShieldTurn ( Context _context, AEntityAction _action, out SpecialChoice _choice )
	{
		_choice = default;
		if (string.IsNullOrEmpty(_action.linkedEquipmentId)
			|| !_context.caster.Equipment.Tools.TryGetValue(_action.linkedEquipmentId, out ToolEquipment tool)
			|| tool is not Shield shield || shield.CurrentHp <= 0)
			return false;

		Entity threat = null;
		int threatDistance = int.MaxValue;
		foreach (Entity enemy in _context.visibleEnemies)
		{
			Tile enemyTile = enemy.Displacement.Coordinates.GetTile();
			if (enemyTile == null || enemyTile == _context.casterTile)
				continue;

			int distance = _context.casterTile.coordinates.DistanceTo(enemyTile.coordinates);
			if (distance >= threatDistance)
				continue;

			threat = enemy;
			threatDistance = distance;
		}

		if (threat == null)
			return false;

		int orientation = GridManager.Instance.GetIncomingSide(threat.Displacement.Coordinates.GetTile(), _context.casterTile);
		Tile neighbor = _context.casterTile.Neighbors[orientation];
		if (orientation == shield.Orientation || neighbor == null)
			return false;

		_choice = new SpecialChoice
		{
			targetTile = neighbor,
			affectedEnemies = new List<Entity> { threat },
			shieldOrientation = orientation,
			score = c_shieldTurnScore
		};
		return true;
	}

	#endregion
}
