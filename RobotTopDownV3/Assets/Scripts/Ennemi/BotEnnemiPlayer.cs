using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotEnnemiPlayer : MonoBehaviour
{
	//How far behind the unit it escorts a Support role stops. Only tunable the planner has so far, so it sits
	//here rather than in GameConfig.
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

	//The round plan only. Everything that happens once the round resolves is EntityAIPlugin.CheckAction's job,
	//and it only ever runs on actions tagged Patroling: a NoAIChange action is handed back untouched. So the
	//state a role tags its actions with is as much part of the role as the path it picks.
	private void DetermineEntityActions ( Entity _entity )
	{
		switch (_entity.Data.aiRole)
		{
			case EnnemiAIRole.Immobile:
				//Target dummy: never moves, never reacts, whatever happens around it.
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
				//Walks its route blindly, but defends itself once it has nothing left to walk: this asymmetry
				//is the behaviour every enemy had before roles existed, keep it as is.
				DeterminePatrolPathActions(_entity, Entity.EntityState.NoAIChange, Entity.EntityState.Patroling);
				break;
		}
	}

	#region Roles

	//Closes in on the nearest enemy it can see and lets CheckAction run the fight from there: the round plan
	//only has to put the unit somewhere it has something to shoot at.
	private void DetermineAggressiveActions ( Entity _entity )
	{
		Entity target = GetClosestVisibleEnemy(_entity);
		if (target == null)
		{
			//Nothing in sight: walk the patrol path, but stay reactive so contact triggers the fight.
			DeterminePatrolPathActions(_entity, Entity.EntityState.Patroling, Entity.EntityState.Patroling);
			return;
		}

		//A tile the unit could actually fire from, never the target's own tile: GetPath exempts its
		//destination from the occupancy check, so an occupied destination is walked straight into.
		Tile firingTile = _entity.AI.GetClosestFiringTile(target, true);
		if (firingTile == null)
		{
			//No firing position in reach. Holding is the safe plan: CheckAction re-picks one every tick and
			//has the closing in fallback the planner does not.
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.Patroling);
			return;
		}

		PlanPathTo(_entity, firingTile, Entity.EntityState.Patroling);
	}

	//Scout: walks its path until it spots someone, then holds and keeps it in sight instead of engaging.
	//Everything stays NoAIChange so the unit never opens fire on its own. Falling back once spotted would
	//need EntityState.Fleeing, which nothing implements yet.
	private void DetermineReconActions ( Entity _entity )
	{
		if (GetClosestVisibleEnemy(_entity) != null)
		{
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.NoAIChange);
			return;
		}

		DeterminePatrolPathActions(_entity, Entity.EntityState.NoAIChange, Entity.EntityState.NoAIChange);
	}

	//Escorts the closest ally, stopping m_supportFollowDistance tiles short of it so it stays behind the line.
	//Tagged Patroling so it still defends itself if something reaches it.
	private void DetermineSupportActions ( Entity _entity )
	{
		List<Tile> pathToEscorted = GetPathToClosestEscortedAlly(_entity);

		if (pathToEscorted == null)
		{
			DeterminePatrolPathActions(_entity, Entity.EntityState.Patroling, Entity.EntityState.Patroling);
			return;
		}

		//The path holds the unit's own tile first and the ally's last, so it takes m_supportFollowDistance + 2
		//tiles for the trimmed path to still contain a step to walk.
		if (pathToEscorted.Count < m_supportFollowDistance + 2)
		{
			//Already in position: hold rather than walk into the unit being escorted.
			AddWaitActionsFrom(_entity, 0, Entity.EntityState.Patroling);
			return;
		}

		PlanPathAlong(_entity, pathToEscorted.GetRange(0, pathToEscorted.Count - m_supportFollowDistance), Entity.EntityState.Patroling);
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
		//GetPath walks back from the destination, every other caller reverses it before use
		pathToClosestTileInPath?.Reverse();

		for (int i = 0; i < GameConfig.current.game.actionTokenPerRound;)
		{
			EntityActionData movementActionData = _entity.AI.GetMovementAction();
			//No movement action passes its condition (rooted, damaged legs...): waiting is all that is left,
			//and GetAction would dereference a null data.
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

				//GetNextTile returns null as soon as lastDestination is not one of the path tiles, which happens
				//whenever the unit drifted off it. Dereferencing that null aborted the whole planning pass, and
				//the entity simply stopped moving from that round on.
				if (nextDestination == null)
					break;

				lastDestination = nextDestination;
				thisActionPath.Add(lastDestination.coordinates.ID);
			}

			//Nowhere left to walk: spend the remaining tokens waiting rather than registering a movement with an
			//empty path, which throws on thisActionPath[^1].
			if (thisActionPath.Count == 0)
			{
				AddWaitActionsFrom(_entity, i, _idleState);
				return;
			}

			movementAction.targetTileIDs = thisActionPath.ToArray();
			movementAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			movementAction.targetTileID = thisActionPath[^1];

			//GetAction already ran Init while targetTileIDs was still null, which left positionAtActionEndID
			//on the start tile and froze it there for the whole round. Re-run it now that the path is known,
			//the way every other caller does.
			movementAction.Init(movementActionData, movementAction.linkedEquipmentId, _entity.ID
				, movementAction.supposedPositionAtActionStartID, i);

			TurnManager.Instance.AddAction(_entity.ID, movementAction, _moveState);

			i += movementAction.TotalDuration;
		}
	}

	#endregion

	#region Planning

	private void PlanPathTo ( Entity _entity, Tile _destination, Entity.EntityState _state )
	{
		Tile from = _entity.Displacement.Coordinates.GetTile();
		if (_destination == null || _destination == from)
		{
			AddWaitActionsFrom(_entity, 0, _state);
			return;
		}

		List<Tile> path = GridManager.Instance.GetPath(from, _destination, true, _movingEntity: _entity, _canTraverseAllies: true);
		//GetPath walks back from the destination, every other caller reverses it before use
		path?.Reverse();

		PlanPathAlong(_entity, path, _state);
	}

	//Spends the round's tokens walking _path, which starts on the tile the unit stands on. Shared by every
	//role that heads for a destination; the patrol role keeps its own loop because it walks its NodePath past
	//the destination instead of stopping on it.
	private void PlanPathAlong ( Entity _entity, List<Tile> _path, Entity.EntityState _state )
	{
		if (_path == null || _path.Count < 2)
		{
			AddWaitActionsFrom(_entity, 0, _state);
			return;
		}

		for (int i = 0; i < GameConfig.current.game.actionTokenPerRound;)
		{
			EntityActionData movementActionData = _entity.AI.GetMovementAction();
			//No movement action passes its condition (rooted, damaged legs...): waiting is all that is left,
			//and GetAction would dereference a null data.
			if (movementActionData == null)
			{
				AddWaitActionsFrom(_entity, i, _state);
				return;
			}

			MoveToTargetAction movementAction = TurnManager.Instance.GetAction(movementActionData, _entity.ID, _entity.ComponentLinkedToAction[movementActionData.enumID][0], i) as MoveToTargetAction;

			List<int> thisActionPath = new();
			for (int j = 0; j < movementAction.TotalDuration && i + j + 1 < _path.Count; j++)
				thisActionPath.Add(_path[i + j + 1].coordinates.ID);

			//Destination reached: spend the remaining tokens waiting rather than registering a movement with an
			//empty path, which throws on thisActionPath[^1].
			if (thisActionPath.Count == 0)
			{
				AddWaitActionsFrom(_entity, i, _state);
				return;
			}

			movementAction.targetTileIDs = thisActionPath.ToArray();
			movementAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
			movementAction.targetTileID = thisActionPath[^1];

			//GetAction already ran Init while targetTileIDs was still null, which left positionAtActionEndID
			//on the start tile and froze it there for the whole round. Re-run it now that the path is known,
			//the way every other caller does.
			movementAction.Init(movementActionData, movementAction.linkedEquipmentId, _entity.ID
				, movementAction.supposedPositionAtActionStartID, i);

			TurnManager.Instance.AddAction(_entity.ID, movementAction, _state);

			i += movementAction.TotalDuration;
		}
	}

	private void AddWaitActionsFrom ( Entity _entity, int _fromToken, Entity.EntityState _state )
	{
		for (int i = _fromToken; i < GameConfig.current.game.actionTokenPerRound; i++)
			TurnManager.Instance.AddAction(_entity.ID, EntityActionEnumID.Wait, _state, null);
	}

	#endregion

	#region Perception

	//Vision is only refreshed by DOAllPrewarmCheck during the tick loop, and planning runs on onEndInputPhase,
	//before any of that: without this rescan the planner reads whatever the last tick of the previous round
	//left behind.
	private Entity GetClosestVisibleEnemy ( Entity _entity )
	{
		_entity.AI.RefreshEnemiesInVisionRange(true);
		return _entity.AI.GetClosestEnemyInVisionRange(true);
	}

	//Path to the closest living ally that is not another Support - two supports would otherwise escort each
	//other and both stand still. One GetPath per ally rather than a single BFS, because that ranks them on the
	//real walkable distance and hands back the path to the winner, with no second pass needed.
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

		//GetPath walks back from the destination, every other caller reverses it before use
		closestPath?.Reverse();
		return closestPath;
	}

	#endregion
}
