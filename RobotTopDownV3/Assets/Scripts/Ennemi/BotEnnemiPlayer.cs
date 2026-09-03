using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotEnnemiPlayer : MonoBehaviour
{
	[SerializeField, Min(1)] private int m_supportFollowDistance = 2;

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

		foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[1].Entities)
		{
			if (entity.Equipment.IsDead)
				continue;

			DetermineEntityActions(entity);
		}
	}

	private void DetermineEntityActions ( Entity _entity )
	{
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

	private void DetermineAggressiveActions ( Entity _entity )
	{
		Entity target = GetClosestVisibleEnemy(_entity);
		if (target == null)
		{
			DeterminePatrolPathActions(_entity, Entity.EntityState.Patroling, Entity.EntityState.Patroling);
			return;
		}

		Tile firingTile = _entity.AI.GetClosestFiringTile(target, true);
		if (firingTile == null)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.Patroling);
			return;
		}

		int spentTokens = PlanPathTo(_entity, firingTile, Entity.EntityState.Patroling, out bool isInFiringPosition);

		if (!isInFiringPosition)
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
		Tile lastDestination = from;
		NodePath closestPath = NodePathManager.Instance.GetClosestPath(from, out Tile closestTile);

		if (closestPath == null || closestTile == null)
		{
			AddWaitActionsFrom(_entity, 0, _idleState);
			return;
		}

		List<Tile> pathToClosestTileInPath = GridManager.Instance.GetPath(from, closestTile, true, _movingEntity: _entity, _canTraverseAllies: true);
		pathToClosestTileInPath?.Reverse();

		for (int i = 0; i < GameConfig.current.game.actionTokenPerRound;)
		{
			EntityActionData movementActionData = _entity.AI.GetMovementAction();
			if (movementActionData == null)
			{
				AddWaitActionsFrom(_entity, i, _idleState);
				return;
			}

			MoveToTargetAction movementAction = TurnManager.Instance.GetAction(movementActionData, _entity.ID, _entity.ComponentLinkedToAction[movementActionData.enumID][0], i) as MoveToTargetAction;

			List<int> thisActionPath = new();
			for (int j = 0; j < movementAction.TotalDuration; j++)
			{
				Tile nextDestination = pathToClosestTileInPath != null && i + j + 1 < pathToClosestTileInPath.Count
					? pathToClosestTileInPath[i + j + 1]
					: closestPath.GetNextTile(lastDestination);

				if (nextDestination == null)
					break;

				lastDestination = nextDestination;
				thisActionPath.Add(lastDestination.coordinates.ID);
			}

			if (thisActionPath.Count == 0)
			{
				AddWaitActionsFrom(_entity, i, _idleState);
				return;
			}

			movementAction.targetTileIDs = thisActionPath.ToArray();
			movementAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			movementAction.targetTileID = thisActionPath[^1];

			movementAction.Init(movementActionData, movementAction.linkedEquipmentId, _entity.ID
				, movementAction.supposedPositionAtActionStartID, i);

			TurnManager.Instance.AddAction(_entity.ID, movementAction, _moveState);

			i += movementAction.TotalDuration;
		}
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
		while (spentTokens < GameConfig.current.game.actionTokenPerRound)
		{
			EntityActionData movementActionData = _entity.AI.GetMovementAction();
			if (movementActionData == null)
				return spentTokens;

			MoveToTargetAction movementAction = TurnManager.Instance.GetAction(movementActionData, _entity.ID, _entity.ComponentLinkedToAction[movementActionData.enumID][0], spentTokens) as MoveToTargetAction;

			List<int> thisActionPath = new();
			for (int j = 0; j < movementAction.TotalDuration && spentTokens + j + 1 < _path.Count; j++)
				thisActionPath.Add(_path[spentTokens + j + 1].coordinates.ID);

			if (thisActionPath.Count == 0)
				return spentTokens;

			_didReachEnd = thisActionPath[^1] == _path[^1].coordinates.ID;

			movementAction.targetTileIDs = thisActionPath.ToArray();
			movementAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			movementAction.targetTileID = thisActionPath[^1];

			movementAction.Init(movementActionData, movementAction.linkedEquipmentId, _entity.ID
				, movementAction.supposedPositionAtActionStartID, spentTokens);

			TurnManager.Instance.AddAction(_entity.ID, movementAction, _state);

			spentTokens += movementAction.TotalDuration;
		}

		return spentTokens;
	}

	private void AddWaitActionsFrom ( Entity _entity, int _fromToken, Entity.EntityState _state )
	{
		for (int i = _fromToken; i < GameConfig.current.game.actionTokenPerRound; i++)
			TurnManager.Instance.AddAction(_entity.ID, EntityActionEnumID.Wait, _state, null);
	}

	private void AddAttackActionsFrom ( Entity _entity, int _fromToken, Entity _target )
	{
		int tokenPerRound = GameConfig.current.game.actionTokenPerRound;
		for (int i = _fromToken; i < tokenPerRound;)
		{
			EntityActionData attackData = _entity.AI.GetAttackOrSpecialAction(tokenPerRound - i, _target);
			if (attackData == null || !TurnManager.Instance.AddAction(_entity.ID, attackData.enumID, Entity.EntityState.Patroling
					, _entity.ComponentLinkedToAction[attackData.enumID][0]))
			{
				AddWaitActionsFrom(_entity, i, Entity.EntityState.Patroling);
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
