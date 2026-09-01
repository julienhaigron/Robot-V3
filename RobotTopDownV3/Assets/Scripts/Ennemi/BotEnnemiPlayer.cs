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
		bool isTherePathsInScene = NodePathManager.Instance != null;

		if (isTherePathsInScene)
		{
			Tile from = _entity.Displacement.Coordinates.GetTile();
			Tile lastDestination = from;
			NodePath closestPath = NodePathManager.Instance.GetClosestPath(from, out Tile closestTile);
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
					lastDestination = pathToClosestTileInPath != null && i + j + 1 < pathToClosestTileInPath.Count ? pathToClosestTileInPath[i + j + 1] : closestPath.GetNextTile(lastDestination);
					thisActionPath.Add(lastDestination.coordinates.ID);
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
		else
		{
			for (int i = 0; i < GameConfig.current.game.actionTokenPerRound; i++)
				TurnManager.Instance.AddAction(_entity.ID, EntityActionEnumID.Wait, Entity.EntityState.Patroling, null);
		}


	}


}
