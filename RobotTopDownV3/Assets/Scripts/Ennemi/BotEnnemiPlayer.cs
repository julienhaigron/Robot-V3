using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotEnnemiPlayer : MonoBehaviour
{

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
		if (NodePathManager.Instance == null)
		{
			AddWaitActionsFrom(_entity, 0);
			return;
		}

		Tile from = _entity.Displacement.Coordinates.GetTile();
		Tile lastDestination = from;
		NodePath closestPath = NodePathManager.Instance.GetClosestPath(from, out Tile closestTile);

		//GetClosestPath leaves closestTile null when no path tile is reachable at all, and everything below needs
		//both. Waiting is the honest fallback: throwing here cost this entity its whole round and, because
		//InputPhase iterates every enemy in one loop, every enemy still to be planned after it.
		if (closestPath == null || closestTile == null)
		{
			AddWaitActionsFrom(_entity, 0);
			return;
		}

		List<Tile> pathToClosestTileInPath = GridManager.Instance.GetPath(from, closestTile, true, _movingEntity: _entity, _canTraverseAllies: true);
		//GetPath walks back from the destination, every other caller reverses it before use
		pathToClosestTileInPath?.Reverse();

		for (int i = 0; i < GameConfig.current.game.actionTokenPerRound;)
		{
			EntityActionData movementActionData = _entity.AI.GetMovementAction();
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
				AddWaitActionsFrom(_entity, i);
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

			TurnManager.Instance.AddAction(_entity.ID, movementAction, Entity.EntityState.NoAIChange);

			i += movementAction.TotalDuration;
		}
	}

	private void AddWaitActionsFrom ( Entity _entity, int _fromToken )
	{
		for (int i = _fromToken; i < GameConfig.current.game.actionTokenPerRound; i++)
			TurnManager.Instance.AddAction(_entity.ID, EntityActionEnumID.Wait, Entity.EntityState.Patroling, null);
	}
}
