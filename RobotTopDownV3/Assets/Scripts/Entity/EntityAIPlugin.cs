using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityAIPlugin : EntityPlugin
{
	private List<Entity> m_entitiesInVisionRange = new();
	private List<EntityInRangeInfo> m_entitiesInActionRangeInfos = new();
	private Dictionary<EntityActionData.MainActionType, ActionReplacements> m_actionPriorityQueues = new();
	public Dictionary<EntityActionData.MainActionType, ActionReplacements> ActionPriorityQueues => m_actionPriorityQueues;

	[System.Serializable]
	public class ActionReplacements
	{
		public List<EntityActionEnumID> priorityQueue = new();
	}

	private struct EntityInRangeInfo
	{
		public Entity entity;
		public EntityActionEnumID actionID;
		public string linkedEquipmentID;
	}

	private List<Entity> m_lastEntitiesTargeted;
	public List<Entity> LastTargetedEntities => m_lastEntitiesTargeted;

	public struct CheckActionResultInfo
	{
		public bool isActionChanging;
		public AEntityAction replacedAction;
		public AEntityAction replacedFreeAction;
		public string replacementReasonTxt;

		public void ReplaceAction ( AEntityAction _replacedAction, string _reasonTxt )
		{
			isActionChanging = true;
			replacedAction = _replacedAction;
			replacementReasonTxt = _reasonTxt;
		}

		public void ReplaceFreeAction ( AEntityAction _replacedFreeAction, string _reasonTxt )
		{
			isActionChanging = true;
			replacedFreeAction = _replacedFreeAction;
			replacementReasonTxt = _reasonTxt;
		}
	}

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		foreach (EntityActionEnumID actionID in m_linkedEntity.KnownedActions)
		{
			EntityActionData.MainActionType mainType = GameAssets.current.game.entityActionsData[actionID].GetMainActionType();

			if (!m_actionPriorityQueues.ContainsKey(mainType))
				m_actionPriorityQueues.Add(mainType, new());

			m_actionPriorityQueues[mainType].priorityQueue.Add(actionID);
		}
	}

	public void SetActionPriorityQueue ( EntityActionData.MainActionType _mainType, List<EntityActionEnumID> _actionsInOrder )
	{
		m_actionPriorityQueues[_mainType].priorityQueue = new(_actionsInOrder);
	}

	public CheckActionResultInfo CheckAction ( TurnManager.RecordedAction _recordedAction )
	{
		CheckActionResultInfo resultInfo = new CheckActionResultInfo() { isActionChanging = false, replacedAction = _recordedAction.action, replacedFreeAction = _recordedAction.freeAction };
		if (_recordedAction.action.lifetime > 0 && !m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun))
		{
			//already started doing an action so no changes
			return resultInfo;
		}

		DOAllPrewarmCheck(_recordedAction.action);

		EntityActionData movementAction = GetMovementAction();
		bool canMove = !m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun) && !m_linkedEntity.Status.Contains(EntityStatusEnumID.Rooted) && movementAction != null;
		bool hasEnemyInWeaponRange = HasEnemyWeaponInRange(out List<Entity> enemies, out EntityActionEnumID attackEnumID, out string equipmentID);

		if (m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun))
		{
			WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction);
			waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(waitAction, "Unit is stun");
		}
		else if (_recordedAction.entityState == Entity.EntityState.Guarding)
		{
			//no action change if in guard
		}
		else if (hasEnemyInWeaponRange)
		{
			// if eneemy in weapon range
			//  => shoot directly
			m_lastEntitiesTargeted = enemies;

			//here
			//TODO : check action priority queue and choose appropriate action

			AttackAction attackAction = (TurnManager.Instance.GetAction(attackEnumID, m_linkedEntity.ID, equipmentID, _recordedAction.timeAtStart) as AttackAction);

			int maxAmount = Mathf.Min(attackAction.Data.GetMaxTargetAmount(attackAction, m_linkedEntity, null), m_lastEntitiesTargeted.Count);
			int[] targetTilesID = new int[maxAmount];
			int[] targetEntitiesID = new int[maxAmount];
			for (int i = 0; i < maxAmount; i++)
			{
				targetTilesID[i] = m_lastEntitiesTargeted[i].Displacement.Coordinates.ID;
				targetEntitiesID[i] = m_lastEntitiesTargeted[i].ID;
			}

			attackAction.linkedEquipmentId = equipmentID;
			attackAction.targetedEntityIDs = targetEntitiesID;
			attackAction.targetTileIDs = targetTilesID;
			attackAction.Init(GameAssets.current.game.entityActionsData[attackAction.enumID], equipmentID, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(attackAction, "Has enemy in range");
		}
		else if (canMove && HasEnemyInVisionRange() && !hasEnemyInWeaponRange)
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
						resultInfo.ReplaceFreeAction(rotateAction, "Unit in vision but not in correct orientation");
					}
				}
				else
				{
					List<Tile> pathToEnemy = GridManager.Instance.GetPath(closestEntity.Displacement.Coordinates.GetTile(), m_linkedEntity.Displacement.Coordinates.GetTile(), true);
					if (pathToEnemy == null || pathToEnemy.Count < 2)
						return resultInfo;

					//do not change movement target if current target is as close as new one

					pathToEnemy.Reverse();

					List<int> tileIDs = new();
					for (int i = 0; i < movementAction.movementSpeed && i + 1 < pathToEnemy.Count; i++)
						tileIDs.Add(pathToEnemy[i + 1].coordinates.ID);

					MoveToTargetAction moveToAction = (TurnManager.Instance.GetAction(movementAction.enumID, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as MoveToTargetAction);
					moveToAction.mode = MoveToTargetAction.MoveActionMode.Entity;
					moveToAction.targetEntiyID = closestEntity.ID;
					moveToAction.targetTileIDs = tileIDs.ToArray();
					moveToAction.Init(GameAssets.current.game.entityActionsData[movementAction.enumID], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
					resultInfo.ReplaceAction(moveToAction, "Gets closer to entity");

					if (!isAtCorrectOrientation)
					{
						RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
						rotateAction.targetedOrientationID = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile());
						rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceFreeAction(rotateAction, "Unit in vision but not in correct orientation");
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
					resultInfo.ReplaceFreeAction(rotateAction, "Unit in vision but not in correct orientation");
				}

				if (!isEntityInRangeWeaponsPossibleRange)
					TargetEntity(null);


			}
		}
		else if (!canMove && (_recordedAction.action.Data.type == EntityActionData.ActionType.Movement || _recordedAction.action.Data.type == EntityActionData.ActionType.Rotation
			 || _recordedAction.action.Data.codeType == EntityActionData.ActionCodeType.MoveThenAttack))
		{
			WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction);
			waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(waitAction, "Unit cannot move");
		}

		return resultInfo;
	}

	public void DOAllPrewarmCheck ( AEntityAction _action )
	{
		VisionCheck(_action);
		WeaponCheck(_action);
	}

	private List<System.Tuple<EntityActionData, string>> GetAvailableAttackAction ()
	{
		List<System.Tuple<EntityActionData, string>> actionEquipmentPairs = new();
		foreach (EntityActionEnumID actionEnumID in m_linkedEntity.ComponentLinkedToAction.Keys)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
			if (data.type == EntityActionData.ActionType.DistanceAttack || data.type == EntityActionData.ActionType.MeleeAttack)
			{
				foreach (string equipmentID in m_linkedEntity.ComponentLinkedToAction[actionEnumID])
				{
					if (Condition.UseConditionPredicate(TurnManager.Instance.GetAction(actionEnumID, m_linkedEntity.ID, m_linkedEntity.ComponentLinkedToAction[actionEnumID][0], TurnManager.Instance.currentTick)
						, m_linkedEntity, m_lastEntitiesTargeted.Count > 0 ? m_lastEntitiesTargeted[0] : null, data.conditionType))
					{
						actionEquipmentPairs.Add(new(GameAssets.current.game.entityActionsData[actionEnumID], equipmentID));
					}

				}
			}
		}

		return actionEquipmentPairs;
	}

	public EntityActionData GetMovementAction ()
	{
		if (m_actionPriorityQueues.ContainsKey(EntityActionData.MainActionType.Movement))
		{
			foreach(EntityActionEnumID actionEnumID in m_actionPriorityQueues[EntityActionData.MainActionType.Movement].priorityQueue)
			{
				EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
				if (Condition.UseConditionPredicate( TurnManager.Instance.GetAction(actionEnumID, m_linkedEntity.ID, m_linkedEntity.ComponentLinkedToAction[actionEnumID][0], TurnManager.Instance.currentTick)
						, m_linkedEntity, m_lastEntitiesTargeted.Count > 0 ? m_lastEntitiesTargeted[0] : null, data.conditionType))
				{
					return data;
				}
			}
			return null;
		}
		else
		{
			/*foreach (EntityActionEnumID actionEnumID in m_linkedEntity.KnownedActions)
			{
				EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
				if (data.type == EntityActionData.ActionType.Movement && Condition.UseConditionPredicate
					(TurnManager.Instance.GetAction(actionEnumID, m_linkedEntity.ID, m_linkedEntity.ComponentLinkedToAction[actionEnumID][0], TurnManager.Instance.currentTick)
						, m_linkedEntity, m_lastEntitiesTargeted.Count > 0 ? m_lastEntitiesTargeted[0] : null, data.conditionType))
					return data;
			}*/

			return null;
		}
	}

	#region Vision

	private bool HasEnemyWeaponInRange ( out List<Entity> _enemies, out EntityActionEnumID _attackEnumID, out string _equipmentID )
	{
		if (m_entitiesInActionRangeInfos == null || m_entitiesInActionRangeInfos.Count == 0)
		{
			_enemies = null;
			_attackEnumID = EntityActionEnumID.Unknowned;
			_equipmentID = null;
			return false;
		}
		else
		{
			m_entitiesInActionRangeInfos.OrderBy(e => m_actionPriorityQueues[EntityActionData.MainActionType.Attack].priorityQueue.IndexOf(e.actionID));

			_enemies = new();
			_attackEnumID = m_entitiesInActionRangeInfos[0].actionID;
			_equipmentID = m_entitiesInActionRangeInfos[0].linkedEquipmentID;
			foreach (EntityInRangeInfo info in m_entitiesInActionRangeInfos)
			{
				if (info.actionID == _attackEnumID && string.Equals(info.linkedEquipmentID, _equipmentID))
					_enemies.Add(info.entity);
			}

			_enemies.OrderBy(e => e.Displacement.Coordinates.GetTile().Distance);
			return true;
		}
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
		m_entitiesInVisionRange = GridManager.Instance.GetEntitiesInRange(m_linkedEntity.Displacement.Coordinates.GetTile(), m_linkedEntity.Data.NeuronalMembraneData.visionRange, _isThisTurn);

		return m_entitiesInVisionRange;
	}

	private List<EntityInRangeInfo> WeaponCheck ( AEntityAction _action, bool _isThisTurn = true )
	{
		m_entitiesInActionRangeInfos.Clear();

		foreach (System.Tuple<EntityActionData, string> pair in GetAvailableAttackAction())
		{
			AEntityAction relatedAction = _action.enumID == pair.Item1.enumID ? _action : TurnManager.Instance.GetAction(GameAssets.current.game.entityActionsData[pair.Item1.enumID], m_linkedEntity.ID, pair.Item2, _action.timeAtStart);
			List<Tile> tilesInWeaponCone = m_linkedEntity.Equipment.GetTilesInWeaponRange(relatedAction, pair.Item2);
			foreach (Tile tile in tilesInWeaponCone)
			{
				Entity entityOnTile = tile.GetEntity(_isThisTurn);
				if (entityOnTile != null && !entityOnTile.IsAlliedTo(m_linkedEntity.OwnerID))
					m_entitiesInActionRangeInfos.Add(new() { actionID = pair.Item1.enumID, entity = entityOnTile, linkedEquipmentID = pair.Item2 });
			}
		}
		return m_entitiesInActionRangeInfos;
	}


	#endregion

	#region Targeting

	public bool IsEntityInWeaponRange ( Entity _targetEntity, string _attackingWeaponId )
	{
		foreach (EntityInRangeInfo entityInfo in m_entitiesInActionRangeInfos)
		{
			if (entityInfo.entity == _targetEntity && string.Equals(entityInfo.linkedEquipmentID, _attackingWeaponId))
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

		foreach (Weapon weapon in m_linkedEntity.Equipment.Weapons.Values)
		{
			foreach (EntityActionEnumID actionID in weapon.Data.knownedActions)
			{
				if (GameAssets.current.game.entityActionsData[actionID].GetMaxRange(TurnManager.Instance.GetAction(actionID, m_linkedEntity.ID, weapon.ID, TurnManager.Instance.currentTick), m_linkedEntity, _entity) >= _entity.Displacement.Coordinates.GetTile().Distance)
				{
					_weapon = weapon.ID;
					return true;
				}
			}
		}

		return false;
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
		m_lastEntitiesTargeted = new() { _targetedEntity };
	}

	#endregion
}
