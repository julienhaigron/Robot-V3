using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityAIPlugin : EntityPlugin
{

	private List<Entity> m_entitiesInVisionRange = new();
	private Dictionary<string, List<Entity>> m_entitiesInWeaponRange = new();

	private Entity m_lastEntityTargeted;
	public Entity TargetedEntity => m_lastEntityTargeted;

	public struct CheckActionResultInfo
	{
		public bool isActionChanging;
		public AEntityAction replacedAction;
		public AEntityAction replacedFreeAction;

		public void ReplaceAction ( AEntityAction _replacedAction )
		{
			isActionChanging = true;
			replacedAction = _replacedAction;
		}

		public void ReplaceFreeAction ( AEntityAction _replacedFreeAction )
		{
			isActionChanging = true;
			replacedFreeAction = _replacedFreeAction;
		}
	}

	public CheckActionResultInfo CheckAction ( TurnManager.RecordedAction _recordedAction )
	{
		CheckActionResultInfo resultInfo = new CheckActionResultInfo() { isActionChanging = false, replacedAction = _recordedAction.action, replacedFreeAction = _recordedAction.freeAction };
		// 1) Do all prewarm check (enemyInSeight, weaponRange, ...)
		DOAllPrewarmCheck(_recordedAction.action);
		// 2) react depending on those factor

		EntityActionData movementAction = GetMovementAction();
		bool canMove = !m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun) && !m_linkedEntity.Status.Contains(EntityStatusEnumID.Rooted) && movementAction != null;
		EntityActionData availableAttackAction = GetAvailableAttackAction(out string availableEquipment);

		if (m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun))
		{
			WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction);
			waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(waitAction);
		}
		else if (HasEnemyWeaponInRange() && availableAttackAction != null /* && _recordedAction.entityState == Entity.EntityState.Patroling*/)
		{
			// if eneemy in weapon range
			//  => shoot directly
			m_lastEntityTargeted = GetClosestEnemyInWeaponRange(out string _weaponId, true);

			AttackAction attackAction = (TurnManager.Instance.GetAction(availableAttackAction, m_linkedEntity.ID, availableEquipment, _recordedAction.timeAtStart) as AttackAction);
			attackAction.attackingWeaponId = _weaponId;
			attackAction.targetedEntityID = m_lastEntityTargeted.ID;
			attackAction.targetTileID = m_lastEntityTargeted.Displacement.Coordinates.ID;
			attackAction.Init(GameAssets.current.game.entityActionsData[availableAttackAction.enumID], availableEquipment, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(attackAction);
		}
		else if (canMove && HasEnemyInVisionRange() && !HasEnemyWeaponInRange())
		{
			Entity closestEntity = GetClosestEnemyInVisionRange(true);
			bool isEntityInRangeWeaponsPossibleRange = IsEntityInWeaponPossibleRange(closestEntity, out string _weapon, true);
			int orientationTowardTarget = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile());
			bool isAtCorrectOrientation = orientationTowardTarget == m_linkedEntity.Displacement.CurrentOrientation;

			if (_recordedAction.entityState == Entity.EntityState.Patroling)
			{
				//only rotate weapon, no movement if entity is too far
				TargetEntity(closestEntity);
				if (isEntityInRangeWeaponsPossibleRange)
				{
					/*//should performing entity stop moving ?
					WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID) as WaitAction);
					waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID);
					resultInfo.ReplaceAction(waitAction);*/

					if (!isAtCorrectOrientation)
					{
						RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
						rotateAction.targetedOrientationID = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile());
						rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceFreeAction(rotateAction);
					}
				}
				else
				{
					List<Tile> pathToEnemy = GridManager.Instance.GetPath(closestEntity.Displacement.Coordinates.GetTile(), m_linkedEntity.Displacement.Coordinates.GetTile(), true);
					if (pathToEnemy == null || pathToEnemy.Count < 2)
						return resultInfo;
					pathToEnemy.Reverse();

					List<int> tileIDs = new();
					for (int i = 0; i < movementAction.movementSpeed && i + 1 < pathToEnemy.Count; i++)
						tileIDs.Add(pathToEnemy[i + 1].coordinates.ID);

					MoveToTargetAction moveToAction = (TurnManager.Instance.GetAction(movementAction.enumID, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as MoveToTargetAction);
					moveToAction.mode = MoveToTargetAction.MoveActionMode.Entity;
					moveToAction.targetEntiyID = closestEntity.ID;
					moveToAction.thisActionDestinationIDArray = tileIDs.ToArray();
					moveToAction.Init(GameAssets.current.game.entityActionsData[movementAction.enumID], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
					resultInfo.ReplaceAction(moveToAction);

					if (!isAtCorrectOrientation)
					{
						RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
						rotateAction.targetedOrientationID = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile());
						rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceFreeAction(rotateAction);
					}
				}
			}
			else if (_recordedAction.entityState == Entity.EntityState.Guarding)
			{
				//rotate weapon or move toward enemy if too far
				if (!isAtCorrectOrientation)
				{
					TargetEntity(closestEntity);
					RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
					rotateAction.targetedOrientationID = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile());
					rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
					resultInfo.ReplaceFreeAction(rotateAction);
				}

				if (!isEntityInRangeWeaponsPossibleRange)
					TargetEntity(null);


			}
		}
		else if (!canMove && (_recordedAction.action.Data.type == EntityActionData.ActionType.Movement || _recordedAction.action.Data.type == EntityActionData.ActionType.Rotation))
		{
			WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction);
			waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(waitAction);
		}

		return resultInfo;
	}

	public void DOAllPrewarmCheck ( AEntityAction _action )
	{
		VisionCheck(_action);
		WeaponCheck(_action);
	}

	private EntityActionData GetAvailableAttackAction (out string _equipmentID)
	{
		foreach (EntityActionEnumID action in m_linkedEntity.ComponentLinkedToAction.Keys)
		{
			if ((GameAssets.current.game.entityActionsData[action].type == EntityActionData.ActionType.DistanceAttack
				|| GameAssets.current.game.entityActionsData[action].type == EntityActionData.ActionType.MeleeAttack
				))
			{
				foreach(string equipmentID in m_linkedEntity.ComponentLinkedToAction[action])
				{
					if (!m_linkedEntity.Equipment.EquipmentInCooldown.Keys.Contains(equipmentID))
					{
						_equipmentID = equipmentID;
						return GameAssets.current.game.entityActionsData[action];
					}

				}
			}
		}

		_equipmentID = null;
		return null;
	}

	public EntityActionData GetMovementAction ()
	{
		foreach (EntityActionEnumID action in m_linkedEntity.KnownedActions)
		{
			if (GameAssets.current.game.entityActionsData[action].type == EntityActionData.ActionType.Movement)
				return GameAssets.current.game.entityActionsData[action];
		}

		return null;
	}

	#region Vision

	private bool HasEnemyWeaponInRange ()
	{
		foreach (List<Entity> entities in m_entitiesInWeaponRange.Values)
		{
			if (entities.Count > 0)
				return true;
		}

		return false;
	}

	private bool HasEnemyInVisionRange ()
	{
		foreach (Entity entity in m_entitiesInVisionRange)
		{
			if (!entity.IsAlliedTo(m_linkedEntity.OwnerID))
				return true;
		}
		return false;
	}

	private List<Entity> VisionCheck ( AEntityAction _action, bool _isThisTurn = true )
	{
		m_entitiesInVisionRange = new();
		if (m_linkedEntity.Data.BrainData.capacities.Contains(EntityCapacityAsset.EntityCapacityType.VisualSensor)
			|| m_linkedEntity.Data.BrainData.capacities.Contains(EntityCapacityAsset.EntityCapacityType.RadarSensor))
		{
			m_entitiesInVisionRange = GridManager.Instance.GetEntitiesInRange(m_linkedEntity.Displacement.Coordinates.GetTile(), m_linkedEntity.Data.NeuronalMembraneData.visionRange, _isThisTurn);
		}

		return m_entitiesInVisionRange;
	}

	private Dictionary<string, List<Entity>> WeaponCheck ( AEntityAction _action, bool _isThisTurn = true )
	{
		m_entitiesInWeaponRange.Clear();
		foreach (string weaponId in m_linkedEntity.Equipment.Weapons.Keys)
		{
			m_entitiesInWeaponRange.Add(weaponId, new());
			foreach (KeyValuePair<EntityActionEnumID, List<string>> pair in m_linkedEntity.ComponentLinkedToAction)
			{
				if (!pair.Value.Contains(weaponId) || (GameAssets.current.game.entityActionsData[pair.Key].type != EntityActionData.ActionType.DistanceAttack
					&& GameAssets.current.game.entityActionsData[pair.Key].type != EntityActionData.ActionType.MeleeAttack))
					continue;

				AEntityAction relatedAction = _action.enumID == pair.Key ? _action : TurnManager.Instance.GetAction(GameAssets.current.game.entityActionsData[pair.Key], m_linkedEntity.ID, weaponId, _action.timeAtStart);
				List<Tile> tilesInWeaponCone = m_linkedEntity.Equipment.GetTilesInWeaponRange(relatedAction, weaponId);
				foreach (Tile tile in tilesInWeaponCone)
				{
					Entity entityOnTile = tile.GetEntity(_isThisTurn);
					if (entityOnTile != null && !entityOnTile.IsAlliedTo(m_linkedEntity.OwnerID))
						m_entitiesInWeaponRange[weaponId].Add(entityOnTile);
				}
			}
		}
		return m_entitiesInWeaponRange;
	}


	#endregion

	#region Targeting

	public bool IsEntityInWeaponRange ( Entity _targetEntity, string _attackingWeaponId )
	{
		foreach (Entity entity in m_entitiesInWeaponRange[_attackingWeaponId])
		{
			if (entity == _targetEntity)
			{
				return true;
			}
		}

		return false;
	}

	public bool IsEntityInWeaponPossibleRange ( Entity _entity, out string _weapon, bool _isThisTurn = true )
	{
		_weapon = "";
		if (_entity == null)
			return false;

		GridManager.Instance.BFS(m_linkedEntity.Displacement.Coordinates.GetTile(), -1, _entity.Displacement.Coordinates.GetTile(), _isThisTurn);

		foreach (string weaponId in m_linkedEntity.Equipment.Weapons.Keys)
		{
			if (m_linkedEntity.Equipment.Weapons[weaponId].Data.range >= _entity.Displacement.Coordinates.GetTile().Distance)
			{
				_weapon = weaponId;
				return true;
			}
		}

		return false;
	}

	public Entity GetClosestEnemyInWeaponRange ( out string _weaponId, bool _isThisTurn = true )
	{
		GridManager.Instance.BFS(m_linkedEntity.Displacement.Coordinates.GetTile(), -1, null, _isThisTurn);

		Entity closestEntity = null;
		_weaponId = "";
		foreach (string weaponId in m_entitiesInWeaponRange.Keys)
		{
			foreach (Entity entity in m_entitiesInWeaponRange[weaponId])
			{
				if (entity.IsAlliedTo(m_linkedEntity.OwnerID)) continue;

				if (closestEntity == null || entity.Displacement.Coordinates.GetTile().Distance < closestEntity.Displacement.Coordinates.GetTile().Distance)
				{
					_weaponId = weaponId;
					closestEntity = entity;
				}
			}
		}

		return closestEntity;
	}

	public Entity GetClosestEnemyInVisionRange ( bool _isThisTurn = true )
	{
		GridManager.Instance.BFS(m_linkedEntity.Displacement.Coordinates.GetTile(), -1, null, _isThisTurn);

		Entity closestEntity = null;
		foreach (Entity entity in m_entitiesInVisionRange)
		{
			if (entity.IsAlliedTo(m_linkedEntity.OwnerID))
				continue;

			if (closestEntity == null || entity.Displacement.Coordinates.GetTile().Distance < closestEntity.Displacement.Coordinates.GetTile().Distance)
			{
				closestEntity = entity;
			}
		}

		return closestEntity;
	}

	public void TargetEntity ( Entity _targetedEntity )
	{
		m_lastEntityTargeted = _targetedEntity;
	}

	#endregion
}
