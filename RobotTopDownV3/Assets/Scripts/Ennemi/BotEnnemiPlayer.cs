using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotEnnemiPlayer : MonoBehaviour
{
	[SerializeField, Min(1)] private int m_supportFollowDistance = 2;

	private const int c_botOwnerID = 1;
	private TeamBrain m_teamBrain;
	private MissionData m_teamBrainMission;

	private void Awake ()
	{
		TurnManager.onEndInputPhase += InputPhase;
	}

	private void OnDestroy ()
	{
		TurnManager.onEndInputPhase -= InputPhase;
	}

	public void InputPhase ()
	{
		if (GameManager.Instance.IsOnline)
			return;

		MissionData mission = GameManager.Instance.CurrentMission;
		if (mission != null && mission.TryGetTeamStrategy(out MatchStrategy strategy))
		{
			PlanTeamRound(mission, strategy);
			return;
		}

		foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[c_botOwnerID].Entities)
		{
			if (entity.Equipment.IsDead)
				continue;

			DetermineEntityActions(entity);
		}
	}

	private void PlanTeamRound ( MissionData _mission, MatchStrategy _strategy )
	{
		if (m_teamBrain == null || m_teamBrainMission != _mission || TurnManager.Instance.RoundCount <= 1)
		{
			m_teamBrain = new TeamBrain(c_botOwnerID);
			m_teamBrainMission = _mission;
		}

		List<Entity> units = new();
		foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[c_botOwnerID].Entities)
		{
			if (entity.Equipment.IsDead)
				continue;

			if (entity.MatchRole != Entity.MatchRoleType.None)
			{
				entity.AI.SetLeash(null, -1);
				PlanMatchRoleRound(entity);
				continue;
			}

			units.Add(entity);
		}

		Dictionary<Entity, TeamBrain.Order> orders = m_teamBrain.PlanRound(_strategy, units);
		foreach (Entity unit in units)
		{
			TeamBrain.Order order = orders.TryGetValue(unit, out TeamBrain.Order unitOrder) ? unitOrder : null;
			unit.AI.SetTeamTactics(_strategy, m_teamBrain.FocusTarget, order?.destination);
			ExecuteOrder(unit, order);
		}
	}

	private void PlanMatchRoleRound ( Entity _entity )
	{
		switch (_entity.MatchRole)
		{
			case Entity.MatchRoleType.Technician:
				PlanTechnicianRound(_entity);
				break;
			case Entity.MatchRoleType.Demolisher:
				PlanDemolisherRound(_entity);
				break;
			default:
				DetermineEntityActions(_entity);
				break;
		}
	}

	private void PlanDemolisherRound ( Entity _entity )
	{
		Tile point = m_teamBrain.GetClosestEnemySabotagePoint(_entity.Displacement.Coordinates.GetTile());
		if (point == null || !_entity.AI.CanEngageATarget())
		{
			DetermineEntityActions(_entity);
			return;
		}

		_entity.AI.SetSabotageTarget(point);
		Tile firingTile = _entity.AI.GetClosestFiringTileAt(point);
		if (firingTile == null)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.NoAIChange);
			return;
		}

		int spentTokens = PlanPathTo(_entity, firingTile, Entity.EntityState.NoAIChange, out bool didReachDestination);
		if (didReachDestination)
			AddAttackActionsFrom(_entity, spentTokens, null, Entity.EntityState.NoAIChange);
		else
			AddWaitActionsFrom(_entity, spentTokens, Entity.EntityState.NoAIChange);
	}

	private void PlanTechnicianRound ( Entity _entity )
	{
		EntityActionData cableData = GetCableActionOf(_entity);
		EntityActionData walkingData = _entity.AI.GetWalkingAction();
		List<Tile> cableTiles = cableData == null ? null : GetCableTilesToLay(_entity.OwnerID, _entity.Displacement.Coordinates.GetTile());
		if (cableTiles == null || cableTiles.Count == 0)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.NoAIChange);
			return;
		}

		string cableEquipmentID = _entity.ComponentLinkedToAction[cableData.enumID][0];
		int cableReach = cableData.GetMaxRange(null, _entity, null);
		int tokenPerRound = GameConfig.current.game.actionTokenPerRound;
		Tile position = _entity.Displacement.Coordinates.GetTile();
		int token = 0;
		int index = 0;

		while (token < tokenPerRound && index < cableTiles.Count)
		{
			Tile next = cableTiles[index];
			if (position.coordinates.DistanceTo(next.coordinates) <= cableReach)
			{
				if (next.IsObstacle(true) || next.GetItem(true) != null || next.GetPlannedItemAt(token) != null)
					break;

				InvokeItemAction cableAction = TurnManager.Instance.GetAction(cableData, _entity.ID, cableEquipmentID, token) as InvokeItemAction;
				if (cableAction == null)
					break;

				cableAction.PlaceItemOn(next);
				if (!TurnManager.Instance.AddAction(_entity.ID, cableAction, Entity.EntityState.NoAIChange))
				{
					cableAction.CancelAction();
					break;
				}

				token += Mathf.Max(1, cableAction.TotalDuration);
				index++;
				continue;
			}

			if (walkingData == null)
				break;

			List<Tile> path = GridManager.Instance.GetPath(position, next, true, _movingEntity: _entity, _canTraverseAllies: true);
			if (path == null || path.Count < 2)
				break;

			path.Reverse();
			Tile step = path[1];
			MoveToTargetAction moveAction = TurnManager.Instance.GetAction(walkingData, _entity.ID, _entity.ComponentLinkedToAction[walkingData.enumID][0], token) as MoveToTargetAction;
			if (moveAction == null)
				break;

			moveAction.targetTileIDs = new int[] { step.coordinates.ID };
			moveAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			moveAction.targetTileID = next.coordinates.ID;
			moveAction.Init(walkingData, moveAction.linkedEquipmentId, _entity.ID, moveAction.supposedPositionAtActionStartID, token);
			if (!TurnManager.Instance.AddAction(_entity.ID, moveAction, Entity.EntityState.NoAIChange))
				break;

			position = step;
			token += Mathf.Max(1, moveAction.TotalDuration);
		}

		AddWaitActionsFrom(_entity, token, Entity.EntityState.NoAIChange);
	}

	private static EntityActionData GetCableActionOf ( Entity _entity )
	{
		foreach (EntityActionEnumID actionID in _entity.KnownedActions)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionID];
			if (data.codeType == EntityActionData.ActionCodeType.InvokeItem && data.invocatedItem is CableItemData)
				return data;
		}

		return null;
	}

	private static List<Tile> GetCableTilesToLay ( int _ownerID, Tile _from )
	{
		List<HashSet<Tile>> commPoints = GridManager.Instance.GetCommPoints(_ownerID);
		if (commPoints == null || commPoints.Count < 2)
			return null;

		HashSet<Tile> network = new(CableItemData.GetPlacedCableTiles(_ownerID));
		foreach (HashSet<Tile> commPoint in commPoints)
			network.UnionWith(commPoint);

		HashSet<Tile> connected = GridManager.GetConnectedTiles(commPoints[0], network);
		HashSet<Tile> targets = new(commPoints.Where(commPoint => !commPoint.Overlaps(connected)).SelectMany(commPoint => commPoint));
		if (targets.Count == 0)
			return new List<Tile>();

		Dictionary<Tile, int> costs = new();
		Dictionary<Tile, Tile> parents = new();
		LinkedList<Tile> frontier = new();
		foreach (Tile tile in connected)
		{
			costs[tile] = 0;
			frontier.AddLast(tile);
		}

		Tile reached = null;
		while (frontier.Count > 0)
		{
			Tile current = frontier.First.Value;
			frontier.RemoveFirst();
			if (targets.Contains(current))
			{
				reached = current;
				break;
			}

			foreach (Tile neighbor in current.Neighbors)
			{
				if (neighbor == null)
					continue;

				bool isNetwork = network.Contains(neighbor);
				if (!isNetwork && (neighbor.IsObstacle(true) || neighbor.GetItem(true) != null))
					continue;

				int cost = costs[current] + (isNetwork ? 0 : 1);
				if (costs.TryGetValue(neighbor, out int knownCost) && knownCost <= cost)
					continue;

				costs[neighbor] = cost;
				parents[neighbor] = current;
				if (isNetwork)
					frontier.AddFirst(neighbor);
				else
					frontier.AddLast(neighbor);
			}
		}

		if (reached == null)
			return null;

		List<Tile> tilesToLay = new();
		for (Tile tile = reached; tile != null && !connected.Contains(tile); tile = parents.TryGetValue(tile, out Tile parent) ? parent : null)
		{
			if (!network.Contains(tile))
				tilesToLay.Add(tile);
		}

		if (_from != null && tilesToLay.Count > 1
			&& _from.coordinates.DistanceTo(tilesToLay[0].coordinates) < _from.coordinates.DistanceTo(tilesToLay[^1].coordinates))
			return tilesToLay;

		tilesToLay.Reverse();
		return tilesToLay;
	}

	private void ExecuteOrder ( Entity _entity, TeamBrain.Order _order )
	{
		_entity.AI.SetLeash(_order?.leashCenter, _order == null ? -1 : _order.leashRadius);
		if (_order == null || _order.destination == null)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.Patroling);
			return;
		}

		if (_order.target != null)
			_entity.AI.CommitTo(_order.target);

		int spentTokens = PlanPathTo(_entity, _order.destination, Entity.EntityState.Patroling, out bool didReachDestination);
		if (didReachDestination && _order.fireTarget != null)
			AddAttackActionsFrom(_entity, spentTokens, _order.fireTarget);
		else
			AddWaitActionsFrom(_entity, spentTokens, Entity.EntityState.Patroling);
	}

	private void DetermineEntityActions ( Entity _entity )
	{
		if (_entity.Data.aiRole == EnnemiAIRole.PatrolPath && TryPlanHuntOfCommittedTarget(_entity))
			return;

		switch (_entity.Data.aiRole)
		{
			case EnnemiAIRole.Immobile:
				AddWaitActionsFrom(_entity, 0, Entity.EntityState.NoAIChange);
				break;

			case EnnemiAIRole.Aggressive:
				DetermineAggressiveActions(_entity);
				break;

			case EnnemiAIRole.Recon:
				DetermineReconActions(_entity);
				break;

			case EnnemiAIRole.Support:
				DetermineSupportActions(_entity);
				break;

			case EnnemiAIRole.PatrolPath:
			default:
				DeterminePatrolPathActions(_entity, Entity.EntityState.NoAIChange, Entity.EntityState.Patroling);
				break;
		}
	}

	#region Roles

	private bool TryPlanHuntOfCommittedTarget ( Entity _entity )
	{
		if (!_entity.AI.CanEngageATarget())
			return false;

		_entity.AI.RefreshEnemiesInVisionRange(true);

		Entity target = _entity.AI.GetCommittedTarget();
		if (target == null || _entity.AI.IsInVisionRange(target))
			return false;

		if (!_entity.AI.TryGetLastKnownTileOf(target, out Tile lastKnownTile))
		{
			_entity.AI.CommitTo(null);
			return false;
		}

		AddWaitActionsFrom(_entity, PlanPathTo(_entity, _entity.AI.GetReachableDestinationFor(lastKnownTile, true)
			, Entity.EntityState.Patroling, out bool _), Entity.EntityState.Patroling);
		return true;
	}

	private void DetermineAggressiveActions ( Entity _entity )
	{
		if (!_entity.AI.CanEngageATarget())
		{
			DeterminePatrolPathActions(_entity, Entity.EntityState.Patroling, Entity.EntityState.Patroling);
			return;
		}

		_entity.AI.RefreshEnemiesInVisionRange(true);

		Entity target = _entity.AI.GetCommittedTarget();
		if (target == null)
			target = _entity.AI.CommitTo(_entity.AI.GetClosestEnemyInVisionRange(true));

		if (target != null && !_entity.AI.IsInVisionRange(target))
		{
			if (TryPlanHuntOfCommittedTarget(_entity))
				return;

			target = null;
		}

		if (target == null && !_entity.AI.HasEngagedTarget)
			target = GetClosestReachableEnemy(_entity);

		if (target == null)
		{
			DeterminePatrolPathActions(_entity, Entity.EntityState.Patroling, Entity.EntityState.Patroling);
			return;
		}

		Tile firingTile = _entity.AI.GetClosestFiringTile(target, true);
		Tile destination = firingTile != null ? firingTile : _entity.AI.GetClosestFreeNeighborOf(target.Displacement.Coordinates.GetTile(), true);

		if (destination == null)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.Patroling);
			return;
		}

		int spentTokens = PlanPathTo(_entity, destination, Entity.EntityState.Patroling, out bool didReachDestination);

		if (firingTile == null || !didReachDestination)
		{
			AddWaitActionsFrom(_entity, spentTokens, Entity.EntityState.Patroling);
			return;
		}

		AddAttackActionsFrom(_entity, spentTokens, target);
	}

	private void DetermineReconActions ( Entity _entity )
	{
		if (GetClosestVisibleEnemy(_entity) != null)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.NoAIChange);
			return;
		}

		DeterminePatrolPathActions(_entity, Entity.EntityState.NoAIChange, Entity.EntityState.NoAIChange);
	}

	private void DetermineSupportActions ( Entity _entity )
	{
		List<Tile> pathToEscorted = GetPathToClosestEscortedAlly(_entity);

		if (pathToEscorted == null)
		{
			DeterminePatrolPathActions(_entity, Entity.EntityState.Patroling, Entity.EntityState.Patroling);
			return;
		}

		if (pathToEscorted.Count < m_supportFollowDistance + 2)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.Patroling);
			return;
		}

		int spentTokens = PlanPathAlong(_entity, pathToEscorted.GetRange(0, pathToEscorted.Count - m_supportFollowDistance)
			, Entity.EntityState.Patroling, out bool _);
		AddWaitActionsFrom(_entity, spentTokens, Entity.EntityState.Patroling);
	}

	private void DeterminePatrolPathActions ( Entity _entity, Entity.EntityState _moveState, Entity.EntityState _idleState )
	{
		if (NodePathManager.Instance == null)
		{
			AddWaitActionsFrom(_entity, 0, _idleState);
			return;
		}

		Tile from = _entity.Displacement.Coordinates.GetTile();
		NodePath closestPath = NodePathManager.Instance.GetClosestPath(from, out Tile closestTile);

		if (closestPath == null || closestTile == null)
		{
			AddWaitActionsFrom(_entity, 0, _idleState);
			return;
		}

		Tile entryNode = closestTile;
		Tile rememberedNode = _entity.AI.PatrolNode;
		if (rememberedNode != null && System.Array.IndexOf(closestPath.Path, rememberedNode) >= 0)
			entryNode = rememberedNode;

		Tile lastDestination = from;
		Queue<Tile> pendingSteps = new();

		List<Tile> pathToEntryNode = entryNode == from ? null
			: GridManager.Instance.GetPath(from, entryNode, true, _movingEntity: _entity, _canTraverseAllies: true);

		if (pathToEntryNode == null && entryNode != closestTile && closestTile != from)
		{
			entryNode = closestTile;
			pathToEntryNode = GridManager.Instance.GetPath(from, closestTile, true, _movingEntity: _entity, _canTraverseAllies: true);
		}

		Tile targetNode = entryNode;

		if (pathToEntryNode != null)
		{
			pathToEntryNode.Reverse();
			for (int i = 1; i < pathToEntryNode.Count; i++)
				pendingSteps.Enqueue(pathToEntryNode[i]);
		}

		for (int i = 0; i < GameConfig.current.game.actionTokenPerRound;)
		{
			EntityActionData movementActionData = _entity.AI.GetWalkingAction();
			if (movementActionData == null)
			{
				AddWaitActionsFrom(_entity, i, _idleState);
				return;
			}

			MoveToTargetAction movementAction = TurnManager.Instance.GetAction(movementActionData, _entity.ID, _entity.ComponentLinkedToAction[movementActionData.enumID][0], i) as MoveToTargetAction;

			List<int> thisActionPath = new();
			bool doesStopOnDestination = false;
			for (int j = 0; j < movementActionData.movementSpeed; j++)
			{
				Tile nextDestination = GetNextPatrolStep(_entity, closestPath, pendingSteps, lastDestination, ref targetNode);

				if (nextDestination == null)
					break;

				lastDestination = nextDestination;
				thisActionPath.Add(lastDestination.coordinates.ID);

				if (closestPath.DoesStopAt(lastDestination))
				{
					doesStopOnDestination = true;
					break;
				}
			}

			if (thisActionPath.Count == 0)
			{
				AddWaitActionsFrom(_entity, i, _idleState);
				return;
			}

			movementAction.targetTileIDs = thisActionPath.ToArray();
			movementAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			movementAction.targetTileID = targetNode == null ? thisActionPath[^1] : targetNode.coordinates.ID;

			movementAction.Init(movementActionData, movementAction.linkedEquipmentId, _entity.ID
				, movementAction.supposedPositionAtActionStartID, i);

			TurnManager.Instance.AddAction(_entity.ID, movementAction, _moveState);
			_entity.AI.PatrolNode = targetNode;

			i += movementAction.TotalDuration;

			if (!doesStopOnDestination || i >= GameConfig.current.game.actionTokenPerRound)
				continue;

			TurnManager.Instance.AddAction(_entity.ID, EntityActionEnumID.Wait, _idleState, null);
			i++;
		}
	}

	private Tile GetNextPatrolStep ( Entity _entity, NodePath _path, Queue<Tile> _pendingSteps, Tile _from, ref Tile _targetNode )
	{
		if (_pendingSteps.Count > 0)
			return _pendingSteps.Dequeue();

		for (int i = 0; i < _path.Path.Length; i++)
		{
			_targetNode = _path.GetNextTile(_targetNode);

			if (_targetNode == null)
				return null;

			if (_targetNode == _from)
				continue;

			if (_targetNode.TryGetCurrentEntity(out Entity occupant) && occupant != _entity)
				continue;

			List<Tile> pathToNode = GridManager.Instance.GetPath(_from, _targetNode, true, _movingEntity: _entity, _canTraverseAllies: true);
			if (pathToNode == null || pathToNode.Count < 2)
				continue;

			pathToNode.Reverse();
			for (int j = 1; j < pathToNode.Count; j++)
				_pendingSteps.Enqueue(pathToNode[j]);

			return _pendingSteps.Dequeue();
		}

		return null;
	}

	#endregion

	#region Planning

	private int PlanPathTo ( Entity _entity, Tile _destination, Entity.EntityState _state, out bool _didReachDestination )
	{
		_didReachDestination = false;
		Tile from = _entity.Displacement.Coordinates.GetTile();
		if (_destination == null)
			return 0;

		if (_destination == from)
		{
			_didReachDestination = true;
			return 0;
		}

		List<Tile> path = GridManager.Instance.GetPath(from, _destination, true, _movingEntity: _entity, _canTraverseAllies: true);
		path?.Reverse();

		return PlanPathAlong(_entity, path, _state, out _didReachDestination);
	}

	private int PlanPathAlong ( Entity _entity, List<Tile> _path, Entity.EntityState _state, out bool _didReachEnd )
	{
		_didReachEnd = false;
		if (_path == null || _path.Count < 2)
			return 0;

		int spentTokens = 0;
		int walkedTiles = 0;
		while (spentTokens < GameConfig.current.game.actionTokenPerRound)
		{
			EntityActionData movementActionData = _entity.AI.GetWalkingAction();
			if (movementActionData == null)
				return spentTokens;

			MoveToTargetAction movementAction = TurnManager.Instance.GetAction(movementActionData, _entity.ID, _entity.ComponentLinkedToAction[movementActionData.enumID][0], spentTokens) as MoveToTargetAction;

			List<int> thisActionPath = new();
			for (int j = 0; j < movementActionData.movementSpeed && walkedTiles + j + 1 < _path.Count; j++)
				thisActionPath.Add(_path[walkedTiles + j + 1].coordinates.ID);

			if (thisActionPath.Count == 0)
				return spentTokens;

			_didReachEnd = thisActionPath[^1] == _path[^1].coordinates.ID;

			movementAction.targetTileIDs = thisActionPath.ToArray();
			movementAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			movementAction.targetTileID = _path[^1].coordinates.ID;

			movementAction.Init(movementActionData, movementAction.linkedEquipmentId, _entity.ID
				, movementAction.supposedPositionAtActionStartID, spentTokens);

			TurnManager.Instance.AddAction(_entity.ID, movementAction, _state);

			spentTokens += movementAction.TotalDuration;
			walkedTiles += thisActionPath.Count;
		}

		return spentTokens;
	}

	private void AddWaitActionsFrom ( Entity _entity, int _fromToken, Entity.EntityState _state )
	{
		for (int i = _fromToken; i < GameConfig.current.game.actionTokenPerRound; i++)
			TurnManager.Instance.AddAction(_entity.ID, EntityActionEnumID.Wait, _state, null);
	}

	private void AddAttackActionsFrom ( Entity _entity, int _fromToken, Entity _target, Entity.EntityState _state = Entity.EntityState.Patroling )
	{
		int tokenPerRound = GameConfig.current.game.actionTokenPerRound;
		for (int i = _fromToken; i < tokenPerRound;)
		{
			EntityActionData attackData = _entity.AI.GetAttackOrSpecialAction(tokenPerRound - i, _target);
			if (attackData == null || !TurnManager.Instance.AddAction(_entity.ID, attackData.enumID, _state
					, _entity.ComponentLinkedToAction[attackData.enumID][0]))
			{
				AddWaitActionsFrom(_entity, i, _state);
				return;
			}

			i += Mathf.Max(1, attackData.GetTokenTotalCost(null, _entity, _target));
		}
	}

	#endregion

	#region Perception

	private Entity GetClosestVisibleEnemy ( Entity _entity )
	{
		_entity.AI.RefreshEnemiesInVisionRange(true);
		return _entity.AI.GetClosestEnemyInVisionRange(true);
	}

	private Entity GetClosestReachableEnemy ( Entity _entity )
	{
		Tile from = _entity.Displacement.Coordinates.GetTile();
		Entity closestEnemy = null;
		int closestDistance = int.MaxValue;

		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity == _entity || entity.Equipment.IsDead || entity.IsAlliedTo(_entity.OwnerID))
					continue;

				List<Tile> path = GridManager.Instance.GetPath(from, entity.Displacement.Coordinates.GetTile(), true, _movingEntity: _entity, _canTraverseAllies: true);

				if (path == null || path.Count >= closestDistance)
					continue;

				closestDistance = path.Count;
				closestEnemy = entity;
			}
		}

		return closestEnemy;
	}

	private List<Tile> GetPathToClosestEscortedAlly ( Entity _entity )
	{
		Tile from = _entity.Displacement.Coordinates.GetTile();
		List<Tile> closestPath = null;

		foreach (Entity ally in GameManager.Instance.PlayersEntityAnchor[_entity.OwnerID].Entities)
		{
			if (ally == _entity || ally.Equipment.IsDead || ally.Data.aiRole == EnnemiAIRole.Support)
				continue;

			List<Tile> path = GridManager.Instance.GetPath(from, ally.Displacement.Coordinates.GetTile(), true, _movingEntity: _entity, _canTraverseAllies: true);
			if (path == null || (closestPath != null && path.Count >= closestPath.Count))
				continue;

			closestPath = path;
		}

		closestPath?.Reverse();
		return closestPath;
	}

	#endregion
}
