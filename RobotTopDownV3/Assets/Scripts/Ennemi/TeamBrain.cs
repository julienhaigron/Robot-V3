using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TeamBrain
{
	public class Order
	{
		public Entity target;
		public Entity fireTarget;
		public Tile destination;
		public Tile leashCenter;
		public int leashRadius = -1;
	}

	private const int c_packRadius = 2;
	private const int c_retargetMargin = 2;
	private const int c_minRingRadius = 2;
	private const int c_maxRingRadius = 7;
	private const int c_maxExposure = 8;
	private const int c_aloneIsolation = 99;
	private const int c_unreachable = 100000;

	private class ObjectiveContext
	{
		public List<HashSet<Tile>> holdSites = new();
		public HashSet<Entity> priorityTargets = new();
		public Entity protectedUnit;
		public Tile goal;
	}

	private const int c_singleSiteHolders = 2;
	private const int c_minUnitsForTwoHolders = 4;

	private readonly int m_ownerID;
	private readonly HashSet<Tile> m_takenTiles = new();
	private readonly Dictionary<Tile, int[]> m_walkDistancesCache = new();
	private ObjectiveContext m_objective = new();
	private readonly Dictionary<Tile, int> m_lastSeenRound = new();
	private int m_plannedRounds;
	private Entity m_focusTarget;
	public Entity FocusTarget => m_focusTarget != null && !m_focusTarget.Equipment.IsDead ? m_focusTarget : null;
	private Tile m_lastContactTile;
	private Tile m_lastRingCenter;
	private int m_lastRingInnerRadius;
	private int m_ringTightening;

	public TeamBrain ( int _ownerID )
	{
		m_ownerID = _ownerID;
	}

	public Dictionary<Entity, Order> PlanRound ( MatchStrategy _strategy, List<Entity> _units )
	{
		Dictionary<Entity, Order> orders = new();
		m_takenTiles.Clear();
		m_walkDistancesCache.Clear();
		if (_units.Count == 0)
			return orders;

		RecordTeamVision();
		Dictionary<Entity, Tile> knownEnemies = GetKnownEnemies();
		if (knownEnemies.Count > 0)
			m_lastContactTile = GetMedoid(knownEnemies.Values.ToList());

		m_objective = BuildObjective(knownEnemies, _units);
		Entity protectedUnit = m_objective.protectedUnit;
		List<Entity> fighters = _units.Where(unit => unit != protectedUnit && unit.AI.CanEngageATarget()).ToList();
		List<Entity> escorts = _units.Where(unit => unit != protectedUnit && !unit.AI.CanEngageATarget()).ToList();
		AssignHolders(fighters, knownEnemies, orders);

		switch (_strategy)
		{
			case MatchStrategy.Rush:
				PlanRush(fighters, knownEnemies, orders);
				break;
			case MatchStrategy.Groupement:
				PlanPack(fighters, knownEnemies, orders);
				break;
			case MatchStrategy.Isolation:
				PlanIsolation(fighters, knownEnemies, orders);
				break;
			case MatchStrategy.Encerclement:
				PlanEncirclement(fighters, knownEnemies, orders);
				break;
		}

		List<Entity> leaders = orders.Keys.ToList();
		PlanEscorts(escorts, leaders.Count > 0 ? leaders : escorts, orders);
		if (protectedUnit != null)
			PlanProtectedUnit(protectedUnit, knownEnemies, orders);

		LogConsole.AddLog("Team " + m_ownerID + " " + _strategy + ": " + knownEnemies.Count + " known enemies"
			+ (m_focusTarget != null && knownEnemies.ContainsKey(m_focusTarget) ? ", focus " + m_focusTarget.Data.name : string.Empty)
			+ (m_objective.holdSites.Count > 0 ? ", " + m_objective.holdSites.Count + " site(s) to hold" : string.Empty)
			+ (protectedUnit != null ? ", protects " + protectedUnit.Data.name : string.Empty)
			, LogConsole.LogEventType.DebugSys);

		return orders;
	}

	#region Strategies

	private void PlanRush ( List<Entity> _units, Dictionary<Entity, Tile> _known, Dictionary<Entity, Order> _orders )
	{
		Tile searchArea = _known.Count == 0 ? GetSearchArea(_units) : null;
		foreach (Entity unit in _units)
		{
			int[] walkDistances = GetWalkDistances(GetTileOf(unit));
			if (_known.Count == 0)
			{
				_orders[unit] = new Order { destination = TakeFreeTileNear(searchArea, unit, walkDistances, c_packRadius + 1) };
				continue;
			}

			Entity target = PickPriority(_known, GetTileOf(unit)) ?? PickClosest(_known, walkDistances, unit.AI.GetCommittedTarget());
			_orders[unit] = GetAttackOrder(unit, target, _known[target], walkDistances, null);
		}
	}

	private void PlanPack ( List<Entity> _units, Dictionary<Entity, Tile> _known, Dictionary<Entity, Order> _orders )
	{
		if (_units.Count == 0)
			return;

		Tile packTile = GetMedoid(_units.Select(GetTileOf).ToList());
		Entity packUnit = _units.First(unit => GetTileOf(unit) == packTile);
		int[] packWalkDistances = GetWalkDistances(packTile);

		Entity target = null;
		if (_known.Count > 0)
		{
			target = PickPriority(_known, packTile) ?? PickClosest(_known, packWalkDistances, m_focusTarget);
			m_focusTarget = target;
		}

		bool isObjectiveGoal = m_objective.goal != null;
		Tile goal = isObjectiveGoal ? m_objective.goal : target != null ? _known[target] : GetSearchArea(_units);
		int stopShort = !isObjectiveGoal && target != null ? GetEngageDistance(_units) : 0;
		Tile anchor = GetPackAnchor(packTile, packUnit, goal, stopShort, GetRoundReach(_units));
		Entity visibleTarget = target != null && IsVisible(target) ? target : null;

		foreach (Entity unit in _units.OrderBy(unit => GetDistance(GetTileOf(unit), anchor)))
		{
			int[] walkDistances = GetWalkDistances(GetTileOf(unit));
			Tile slot = TakePackSlot(unit, anchor, visibleTarget, walkDistances, out bool canFireOnTarget);
			_orders[unit] = new Order
			{
				target = target,
				fireTarget = canFireOnTarget ? visibleTarget : null,
				destination = slot,
				leashCenter = anchor,
				leashRadius = c_packRadius + 1
			};
		}
	}

	private void PlanIsolation ( List<Entity> _units, Dictionary<Entity, Tile> _known, Dictionary<Entity, Order> _orders )
	{
		if (_known.Count == 0)
		{
			PlanPack(_units, _known, _orders);
			return;
		}

		Entity target = PickPriority(_known, GetMedoid(_units.Select(GetTileOf).ToList())) ?? PickMostIsolated(_known);
		m_focusTarget = target;
		List<Tile> otherEnemyTiles = _known.Where(pair => pair.Key != target).Select(pair => pair.Value).ToList();

		foreach (Entity unit in _units)
		{
			int[] walkDistances = GetWalkDistances(GetTileOf(unit));
			_orders[unit] = GetAttackOrder(unit, target, _known[target], walkDistances, tile => GetExposure(tile, otherEnemyTiles));
		}
	}

	private void PlanEncirclement ( List<Entity> _units, Dictionary<Entity, Tile> _known, Dictionary<Entity, Order> _orders )
	{
		if (_units.Count == 0)
			return;

		Tile center = _known.Count > 0 ? GetMedoid(_known.Values.ToList()) : GetSearchArea(_units);
		if (center == null)
		{
			PlanPack(_units, _known, _orders);
			return;
		}

		bool isEnemyContained = _known.Count > 0 && m_lastRingCenter != null
			&& _known.Values.All(tile => GetDistance(tile, m_lastRingCenter) < m_lastRingInnerRadius);
		m_ringTightening = isEnemyContained ? Mathf.Min(m_ringTightening + 1, c_maxRingRadius) : 0;

		Dictionary<Entity, int> radii = _units.ToDictionary(unit => unit, GetRingRadius);
		m_lastRingCenter = center;
		m_lastRingInnerRadius = radii.Values.Min();

		Tile facing = m_objective.goal != null && m_objective.goal != center ? m_objective.goal : GetMedoid(_units.Select(GetTileOf).ToList());
		float baseAngle = GetAngle(center, facing);
		List<float> freeSlots = Enumerable.Range(0, _units.Count).Select(slot => baseAngle + slot * 2f * Mathf.PI / _units.Count).ToList();

		foreach (Entity unit in _units.OrderBy(unit => GetDistance(GetTileOf(unit), center)))
		{
			int radius = radii[unit];
			int[] walkDistances = GetWalkDistances(GetTileOf(unit));
			Tile post = null;
			int postSlot = -1;
			float bestScore = float.MaxValue;

			foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(center, Mathf.Max(0, radius - 1), radius + 1, true, true, false))
			{
				int walk = walkDistances[tile.coordinates.ID];
				if (walk < 0 || m_takenTiles.Contains(tile) || !IsFreeFor(tile, unit))
					continue;

				float tileScore = Mathf.Abs(GetDistance(tile, center) - radius) * 10f + walk * 0.5f
					+ (GridManager.Instance.IsInLineOfSight(tile, center) ? 0f : 30f);
				float tileAngle = GetAngle(center, tile);
				for (int slot = 0; slot < freeSlots.Count; slot++)
				{
					float score = GetAngleGap(tileAngle, freeSlots[slot]) * 100f + tileScore;
					if (score >= bestScore)
						continue;

					post = tile;
					postSlot = slot;
					bestScore = score;
				}
			}

			if (post != null)
			{
				freeSlots.RemoveAt(postSlot);
				TakeTile(post);
			}
			else
				post = TakeFreeTileNear(center, unit, walkDistances, radius + 1);

			Entity fireTarget = post == null ? null : PickVisibleTargetFrom(unit, post, _known);
			_orders[unit] = new Order { target = fireTarget, fireTarget = fireTarget, destination = post, leashCenter = post, leashRadius = 1 };
		}
	}

	private void PlanEscorts ( List<Entity> _escorts, List<Entity> _leaders, Dictionary<Entity, Order> _orders )
	{
		if (_escorts.Count == 0 || _leaders.Count == 0)
			return;

		Tile rallyPoint = GetMedoid(_leaders.Select(leader => _orders.TryGetValue(leader, out Order order) && order.destination != null
			? order.destination
			: GetTileOf(leader)).ToList());

		foreach (Entity escort in _escorts)
		{
			int[] walkDistances = GetWalkDistances(GetTileOf(escort));
			_orders[escort] = new Order
			{
				destination = TakeFreeTileNear(rallyPoint, escort, walkDistances, c_packRadius),
				leashCenter = rallyPoint,
				leashRadius = c_packRadius + 1
			};
		}
	}

	#endregion

	#region Objectives

	private ObjectiveContext BuildObjective ( Dictionary<Entity, Tile> _known, List<Entity> _units )
	{
		ObjectiveContext context = new();
		MissionData mission = GameManager.Instance.CurrentMission;
		if (mission == null)
			return context;

		EntityAnchor[] anchors = GameManager.Instance.PlayersEntityAnchor;
		switch (mission.type)
		{
			case MissionData.MissionType.DefenseDeZone:
				HashSet<Tile> zone = anchors[0].Zones;
				if (zone != null && zone.Count > 0)
					context.holdSites.Add(zone);
				break;

			case MissionData.MissionType.ControleDePoint:
				context.holdSites.AddRange(GridManager.Instance.ControlPoints.Where(point => point.Count > 0));
				break;

			case MissionData.MissionType.Assassinat:
				Entity ownKing = anchors[m_ownerID].King;
				if (ownKing != null && !ownKing.Equipment.IsDead && _units.Contains(ownKing))
					context.protectedUnit = ownKing;

				for (int ownerID = 0; ownerID < anchors.Length; ownerID++)
				{
					Entity king = anchors[ownerID].King;
					if (ownerID != m_ownerID && king != null && _known.ContainsKey(king))
						context.priorityTargets.Add(king);
				}
				break;

			case MissionData.MissionType.Construction:
				AddMatchRoleTargets(context, _known, Entity.MatchRoleType.Technician);
				Entity technician = anchors[m_ownerID].Entities.FirstOrDefault(entity => entity != null && !entity.Equipment.IsDead
					&& entity.MatchRole == Entity.MatchRoleType.Technician);
				context.goal = technician != null ? GetTileOf(technician) : null;
				break;

			case MissionData.MissionType.Sabotage:
				AddMatchRoleTargets(context, _known, Entity.MatchRoleType.Demolisher);
				context.goal = GetClosestEnemySabotagePoint(GetMedoid(_units.Select(GetTileOf).ToList()));
				break;
		}

		if (context.holdSites.Count > 0)
			context.goal = GetMedoid(context.holdSites.SelectMany(site => site).ToList());

		return context;
	}

	private static void AddMatchRoleTargets ( ObjectiveContext _context, Dictionary<Entity, Tile> _known, Entity.MatchRoleType _role )
	{
		foreach (Entity enemy in _known.Keys)
		{
			if (enemy.MatchRole == _role)
				_context.priorityTargets.Add(enemy);
		}
	}

	public Tile GetClosestEnemySabotagePoint ( Tile _from )
	{
		if (_from == null)
			return null;

		Tile closest = null;
		int closestDistance = int.MaxValue;
		EntityAnchor[] anchors = GameManager.Instance.PlayersEntityAnchor;
		for (int ownerID = 0; ownerID < anchors.Length; ownerID++)
		{
			if (ownerID == m_ownerID)
				continue;

			foreach (Tile point in anchors[ownerID].Structures)
			{
				if (!SabotagePoints.TryGetLivingPoint(point, out _))
					continue;

				int distance = GetDistance(_from, point);
				if (distance >= closestDistance)
					continue;

				closest = point;
				closestDistance = distance;
			}
		}

		return closest;
	}

	private void AssignHolders ( List<Entity> _fighters, Dictionary<Entity, Tile> _known, Dictionary<Entity, Order> _orders )
	{
		if (m_objective.holdSites.Count == 0 || _fighters.Count == 0)
			return;

		int holdersPerSite = m_objective.holdSites.Count == 1 && _fighters.Count >= c_minUnitsForTwoHolders ? c_singleSiteHolders : 1;
		Dictionary<Entity, int[]> walkDistances = _fighters.ToDictionary(fighter => fighter, fighter => GetWalkDistances(GetTileOf(fighter)));
		Tile teamTile = GetMedoid(_fighters.Select(GetTileOf).ToList());

		foreach (HashSet<Tile> site in m_objective.holdSites
			.OrderBy(GetSiteUrgency)
			.ThenBy(site => site.Min(tile => GetDistance(tile, teamTile))).ToList())
		{
			Tile siteCenter = GetMedoid(site.ToList());
			int siteRadius = site.Max(tile => GetDistance(tile, siteCenter));
			for (int i = 0; i < holdersPerSite && _fighters.Count > 0; i++)
			{
				Entity holder = null;
				Tile post = null;
				int bestWalk = int.MaxValue;
				foreach (Entity fighter in _fighters)
				{
					foreach (Tile tile in site)
					{
						int walk = walkDistances[fighter][tile.coordinates.ID];
						if (walk < 0 || walk >= bestWalk || m_takenTiles.Contains(tile) || !IsFreeFor(tile, fighter))
							continue;

						holder = fighter;
						post = tile;
						bestWalk = walk;
					}
				}

				if (holder == null)
					break;

				TakeTile(post);
				_fighters.Remove(holder);
				Entity fireTarget = PickVisibleTargetFrom(holder, post, _known);
				_orders[holder] = new Order
				{
					target = fireTarget,
					fireTarget = fireTarget,
					destination = post,
					leashCenter = siteCenter,
					leashRadius = siteRadius + 1
				};
			}
		}
	}

	private int GetSiteUrgency ( HashSet<Tile> _site )
	{
		int controller = TurnManager.Instance.GetControllingPlayer(_site);
		return controller == -1 ? 1 : controller == m_ownerID ? 2 : 0;
	}

	private void PlanProtectedUnit ( Entity _unit, Dictionary<Entity, Tile> _known, Dictionary<Entity, Order> _orders )
	{
		List<Tile> teamDestinations = _orders.Values.Where(order => order.destination != null).Select(order => order.destination).ToList();
		Tile rallyPoint = teamDestinations.Count > 0 ? GetMedoid(teamDestinations) : GetTileOf(_unit);
		int[] walkDistances = GetWalkDistances(GetTileOf(_unit));

		Tile safest = null;
		int bestScore = int.MinValue;
		foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(rallyPoint, 0, c_packRadius, true, true, false))
		{
			int walk = walkDistances[tile.coordinates.ID];
			if (walk < 0 || m_takenTiles.Contains(tile) || !IsFreeFor(tile, _unit))
				continue;

			int safety = _known.Count == 0 ? 0 : _known.Values.Min(enemyTile => GetDistance(tile, enemyTile));
			int score = safety * 100 - walk;
			if (score <= bestScore)
				continue;

			safest = tile;
			bestScore = score;
		}

		if (safest != null)
			TakeTile(safest);
		else
			safest = TakeFreeTileNear(rallyPoint, _unit, walkDistances, c_packRadius + 1);

		_orders[_unit] = new Order { destination = safest, leashCenter = rallyPoint, leashRadius = c_packRadius + 1 };
	}

	private Entity PickPriority ( Dictionary<Entity, Tile> _known, Tile _from )
	{
		Entity best = null;
		int bestDistance = int.MaxValue;
		foreach (Entity target in m_objective.priorityTargets)
		{
			if (_from == null || !_known.TryGetValue(target, out Tile tile))
				continue;

			int distance = GetDistance(_from, tile);
			if (distance >= bestDistance)
				continue;

			best = target;
			bestDistance = distance;
		}

		return best;
	}

	#endregion

	#region Targets

	private Dictionary<Entity, Tile> GetKnownEnemies ()
	{
		Dictionary<Entity, Tile> known = new();
		if (!GridManager.Instance.EntitiesVisions.TryGetValue(m_ownerID, out GridManager.PlayerVisionRangeInfo vision))
			return known;

		foreach (KeyValuePair<Entity, Tile> pair in vision.lastKnownEnemyPositions)
		{
			if (pair.Key == null || pair.Value == null || pair.Key.Equipment.IsDead || pair.Key.IsImmortal || pair.Key.IsAlliedTo(m_ownerID))
				continue;

			known[pair.Key] = pair.Value;
		}

		return known;
	}

	private Entity PickClosest ( Dictionary<Entity, Tile> _known, int[] _walkDistances, Entity _current )
	{
		Entity closest = null;
		int closestDistance = int.MaxValue;
		foreach (KeyValuePair<Entity, Tile> pair in _known)
		{
			int distance = GetWalkDistance(_walkDistances, pair.Value);
			if (distance > closestDistance || (distance == closestDistance && closest != null && pair.Key.ID > closest.ID))
				continue;

			closest = pair.Key;
			closestDistance = distance;
		}

		if (_current != null && _current != closest && _known.TryGetValue(_current, out Tile currentTile)
			&& GetWalkDistance(_walkDistances, currentTile) <= closestDistance + c_retargetMargin)
			return _current;

		return closest;
	}

	private Entity PickMostIsolated ( Dictionary<Entity, Tile> _known )
	{
		Entity best = null;
		int bestIsolation = -1;
		foreach (Entity enemy in _known.Keys)
		{
			int isolation = GetIsolation(enemy, _known);
			if (isolation < bestIsolation)
				continue;

			if (isolation == bestIsolation && best != null
				&& (enemy.Equipment.CurrentHealth > best.Equipment.CurrentHealth
					|| (enemy.Equipment.CurrentHealth == best.Equipment.CurrentHealth && enemy.ID > best.ID)))
				continue;

			best = enemy;
			bestIsolation = isolation;
		}

		if (m_focusTarget != null && m_focusTarget != best && _known.ContainsKey(m_focusTarget)
			&& GetIsolation(m_focusTarget, _known) >= bestIsolation - c_retargetMargin)
			return m_focusTarget;

		return best;
	}

	private Entity PickVisibleTargetFrom ( Entity _unit, Tile _post, Dictionary<Entity, Tile> _known )
	{
		Entity best = null;
		int bestDistance = int.MaxValue;
		foreach (KeyValuePair<Entity, Tile> pair in _known)
		{
			int distance = GetDistance(_post, pair.Value) - (m_objective.priorityTargets.Contains(pair.Key) ? c_unreachable : 0);
			if (distance >= bestDistance || !IsVisible(pair.Key) || !_unit.AI.CanFireFrom(_post, pair.Key))
				continue;

			best = pair.Key;
			bestDistance = distance;
		}

		return best;
	}

	private Order GetAttackOrder ( Entity _unit, Entity _target, Tile _knownTile, int[] _walkDistances, System.Func<Tile, int> _exposureOf )
	{
		Order order = new() { target = _target };
		if (!IsVisible(_target))
		{
			order.destination = TakeFreeTileNear(_knownTile, _unit, _walkDistances, c_packRadius);
			return order;
		}

		Tile targetTile = GetTileOf(_target);
		order.destination = _unit.AI.GetBestFiringTile(_target, ( candidate, walkDistance ) => m_takenTiles.Contains(candidate)
			? int.MaxValue
			: walkDistance * 1000 + GetDistance(candidate, targetTile) - (_exposureOf == null ? 0 : _exposureOf(candidate) * 2000));

		if (order.destination == null)
		{
			order.destination = TakeFreeTileNear(targetTile, _unit, _walkDistances, 1);
			return order;
		}

		order.fireTarget = _target;
		TakeTile(order.destination);
		return order;
	}

	private static int GetIsolation ( Entity _enemy, Dictionary<Entity, Tile> _known )
	{
		Tile tile = _known[_enemy];
		int closest = c_aloneIsolation;
		foreach (KeyValuePair<Entity, Tile> pair in _known)
		{
			if (pair.Key != _enemy)
				closest = Mathf.Min(closest, GetDistance(tile, pair.Value));
		}

		return closest;
	}

	private static int GetExposure ( Tile _tile, List<Tile> _enemyTiles )
	{
		int closest = c_maxExposure;
		foreach (Tile enemyTile in _enemyTiles)
			closest = Mathf.Min(closest, GetDistance(_tile, enemyTile));

		return closest;
	}

	#endregion

	#region Places

	private void RecordTeamVision ()
	{
		m_plannedRounds++;
		if (!GridManager.Instance.EntitiesVisions.TryGetValue(m_ownerID, out GridManager.PlayerVisionRangeInfo vision))
			return;

		foreach (KeyValuePair<Entity, HashSet<Tile>> allyVision in vision.entitiesVisionRange)
		{
			if (allyVision.Key == null || allyVision.Key.Equipment.IsDead)
				continue;

			foreach (Tile tile in allyVision.Value)
				m_lastSeenRound[tile] = m_plannedRounds;
		}
	}

	private Tile GetSearchArea ( List<Entity> _units )
	{
		if (m_lastContactTile != null && _units.Any(unit => GetDistance(GetTileOf(unit), m_lastContactTile) <= c_packRadius))
			m_lastContactTile = null;

		if (m_lastContactTile != null)
			return m_lastContactTile;

		Tile spawnCenter = GetEnemySpawnCenter(m_ownerID);
		if (spawnCenter != null && !m_lastSeenRound.ContainsKey(spawnCenter))
			return spawnCenter;

		Tile leastRecentlySeen = GetLeastRecentlySeenTile(_units);
		return leastRecentlySeen != null ? leastRecentlySeen : spawnCenter;
	}

	private Tile GetLeastRecentlySeenTile ( List<Entity> _units )
	{
		int[] walkDistances = GetWalkDistances(GetMedoid(_units.Select(GetTileOf).ToList()));
		Tile best = null;
		int bestSeenRound = int.MaxValue;
		int bestWalk = int.MaxValue;

		foreach (Tile tile in GridManager.Instance.Tiles)
		{
			if (tile == null)
				continue;

			int walk = walkDistances[tile.coordinates.ID];
			if (walk < 0 || tile.IsObstacle(true))
				continue;

			int seenRound = m_lastSeenRound.TryGetValue(tile, out int round) ? round : -1;
			if (seenRound > bestSeenRound || (seenRound == bestSeenRound && walk >= bestWalk))
				continue;

			best = tile;
			bestSeenRound = seenRound;
			bestWalk = walk;
		}

		return best;
	}

	public static Tile GetEnemySpawnCenter ( int _ownerID )
	{
		List<Tile> spawnTiles = new();
		EntityAnchor[] anchors = GameManager.Instance.PlayersEntityAnchor;
		for (int ownerID = 0; ownerID < anchors.Length; ownerID++)
		{
			if (ownerID == _ownerID)
				continue;

			foreach (EntityAnchor.Spawn spawn in anchors[ownerID].AllSpawns)
			{
				Tile tile = spawn.coordinates.GetTile();
				if (tile != null && !tile.IsObstacle(true))
					spawnTiles.Add(tile);
			}
		}

		return spawnTiles.Count == 0 ? null : GetMedoid(spawnTiles);
	}

	private Tile GetPackAnchor ( Tile _packTile, Entity _packUnit, Tile _goal, int _stopShort, int _reach )
	{
		if (_goal != null && _goal.IsObstacle(true))
			_goal = _goal.Neighbors.Where(neighbor => neighbor != null && !neighbor.IsObstacle(true))
				.OrderBy(neighbor => GetDistance(neighbor, _packTile)).FirstOrDefault();

		if (_goal == null || _goal == _packTile)
			return _packTile;

		List<Tile> path = GridManager.Instance.GetPath(_packTile, _goal, true, _movingEntity: _packUnit, _canTraverseAllies: true);
		if (path == null || path.Count < 2)
			return _packTile;

		path.Reverse();
		int advance = Mathf.Clamp(path.Count - 1 - _stopShort, 0, Mathf.Max(0, _reach - c_packRadius));
		return path[advance];
	}

	private Tile TakePackSlot ( Entity _unit, Tile _anchor, Entity _target, int[] _walkDistances, out bool _canFireOnTarget )
	{
		Tile best = null;
		int bestScore = int.MaxValue;
		_canFireOnTarget = false;

		foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(_anchor, 0, c_packRadius, true, true, false))
		{
			int walk = _walkDistances[tile.coordinates.ID];
			if (walk < 0 || walk >= bestScore || m_takenTiles.Contains(tile) || !IsFreeFor(tile, _unit))
				continue;

			bool canFire = _target != null && _unit.AI.CanFireFrom(tile, _target);
			int score = walk + (_target == null || canFire ? 0 : 1000);
			if (score >= bestScore)
				continue;

			best = tile;
			bestScore = score;
			_canFireOnTarget = canFire;
		}

		if (best == null)
			return TakeFreeTileNear(_anchor, _unit, _walkDistances, c_packRadius + 1);

		TakeTile(best);
		return best;
	}

	private Tile TakeFreeTileNear ( Tile _center, Entity _unit, int[] _walkDistances, int _radius )
	{
		if (_center == null)
			return null;

		Tile best = null;
		int bestScore = int.MaxValue;
		for (int radius = _radius; best == null && radius <= _radius + 3; radius += 3)
		{
			foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(_center, 0, radius, true, true, false))
			{
				int walk = _walkDistances[tile.coordinates.ID];
				if (walk < 0 || m_takenTiles.Contains(tile) || !IsFreeFor(tile, _unit))
					continue;

				int score = GetDistance(tile, _center) * 1000 + walk;
				if (score >= bestScore)
					continue;

				best = tile;
				bestScore = score;
			}
		}

		TakeTile(best);
		return best;
	}

	private void TakeTile ( Tile _tile )
	{
		if (_tile != null)
			m_takenTiles.Add(_tile);
	}

	private int GetRingRadius ( Entity _unit )
	{
		int minRange = 1;
		int maxRange = c_minRingRadius;
		if (_unit.AI.TryGetMainWeaponRange(out int weaponMinRange, out int weaponMaxRange))
		{
			minRange = Mathf.Max(1, weaponMinRange);
			maxRange = Mathf.Clamp(weaponMaxRange, c_minRingRadius, c_maxRingRadius);
		}

		return Mathf.Max(minRange, maxRange - m_ringTightening);
	}

	private static int GetEngageDistance ( List<Entity> _units )
	{
		List<int> ranges = _units.Select(unit => unit.AI.TryGetMainWeaponRange(out _, out int maxRange) ? maxRange : 1).OrderBy(range => range).ToList();
		return Mathf.Clamp(ranges[ranges.Count / 2], 2, 6);
	}

	private static int GetRoundReach ( List<Entity> _units )
	{
		int slowestSpeed = _units.Select(unit => unit.AI.GetWalkingAction()).Where(data => data != null).Select(data => Mathf.Max(1, data.movementSpeed)).DefaultIfEmpty(1).Min();
		return GameConfig.current.game.actionTokenPerRound * slowestSpeed;
	}

	#endregion

	#region Grid helpers

	private int[] GetWalkDistances ( Tile _from )
	{
		if (_from == null)
			return GridManager.Instance.GetWalkDistances(null, m_ownerID);

		if (!m_walkDistancesCache.TryGetValue(_from, out int[] distances))
		{
			distances = GridManager.Instance.GetWalkDistances(_from, m_ownerID);
			m_walkDistancesCache.Add(_from, distances);
		}

		return distances;
	}

	private static int GetWalkDistance ( int[] _walkDistances, Tile _tile )
	{
		int distance = _walkDistances[_tile.coordinates.ID];
		return distance < 0 ? c_unreachable : distance;
	}

	private static bool IsFreeFor ( Tile _tile, Entity _unit )
	{
		if (_tile == null || _tile.IsObstacle(true))
			return false;

		Entity occupant = _tile.GetEntity(true);
		return occupant == null || occupant == _unit;
	}

	private bool IsVisible ( Entity _entity )
	{
		return GridManager.Instance.IsTileSeenByTeam(m_ownerID, GetTileOf(_entity));
	}

	private static Tile GetTileOf ( Entity _entity )
	{
		return _entity.Displacement.Coordinates.GetTile();
	}

	private static int GetDistance ( Tile _from, Tile _to )
	{
		return _from.coordinates.DistanceTo(_to.coordinates);
	}

	private static Tile GetMedoid ( List<Tile> _tiles )
	{
		Tile medoid = null;
		int bestSum = int.MaxValue;
		foreach (Tile candidate in _tiles)
		{
			int sum = 0;
			foreach (Tile other in _tiles)
				sum += GetDistance(candidate, other);

			if (sum >= bestSum)
				continue;

			medoid = candidate;
			bestSum = sum;
		}

		return medoid;
	}

	private static float GetAngle ( Tile _from, Tile _to )
	{
		Vector3 delta = _to.transform.position - _from.transform.position;
		return Mathf.Atan2(delta.z, delta.x);
	}

	private static float GetAngleGap ( float _a, float _b )
	{
		float gap = Mathf.Abs(_a - _b) % (2f * Mathf.PI);
		return gap > Mathf.PI ? 2f * Mathf.PI - gap : gap;
	}

	#endregion
}
