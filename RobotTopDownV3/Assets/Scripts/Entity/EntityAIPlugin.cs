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
		public Tile tile;
		public Entity entity;
		public EntityActionEnumID actionID;
		public string linkedEquipmentID;
	}

	private List<Entity> m_lastEntitiesTargeted = new();
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
		else if (m_linkedEntity.Equipment.IsDead)
		{
			//we dont handle it here
			return resultInfo;
		}
		else if (m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun))
		{
			resultInfo.ReplaceAction(GetWaitActionFor(_recordedAction), "Unit is stun");
			return resultInfo;
		}

		bool needsTarget = _recordedAction.action.Data.DoesResolveItsOwnTarget();
		if (needsTarget)
		{
			if (TryResolveEntityTarget(_recordedAction, ref resultInfo))
				return resultInfo;

			if (_recordedAction.entityState == Entity.EntityState.NoAIChange)
			{
				resultInfo.ReplaceAction(GetWaitActionFor(_recordedAction), "No target in reach for " + _recordedAction.type);
				return resultInfo;
			}
		}

		if (_recordedAction.entityState == Entity.EntityState.NoAIChange)
		{
			//no action change if in NoAIChange
			return resultInfo;
		}

		DOAllPrewarmCheck(_recordedAction.action);

		EntityActionData movementAction = GetMovementAction();
		bool canMove = !m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun) && !m_linkedEntity.Status.Contains(EntityStatusEnumID.Rooted) && movementAction != null;
		bool hasEnemyInWeaponRange = HasEnemyInWeaponRange(out List<Entity> enemies, out EntityActionEnumID attackEnumID, out string equipmentID);
		bool hasEnemyInVisionRange = HasEnemyInVisionRange();
		EntityActionData.MainActionType currentActionMainType = _recordedAction.action.Data.GetMainActionType();

		if (hasEnemyInWeaponRange && currentActionMainType == EntityActionData.MainActionType.Movement)
		{
			// if eneemy in weapon range
			//  => shoot directly
			m_lastEntitiesTargeted = enemies;

			AttackAction attackAction = (TurnManager.Instance.GetAction(attackEnumID, m_linkedEntity.ID, equipmentID, _recordedAction.timeAtStart) as AttackAction);

			if (attackAction == null)
			{
				Debug.LogError("error, action " + attackEnumID + " isnt an AttackAction code type", gameObject);
				return resultInfo;
			}

			bool shouldAddTileInDirectionToTarget = attackAction.Data.aoeType != EntityActionData.AOEType.Noone && attackAction.Data.aoECenterType == EntityActionData.AOECenterType.Self;
			Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();
			//int maxAmount = Mathf.Min(attackAction.Data.GetMaxTargetAmount(attackAction, m_linkedEntity, null), m_lastEntitiesTargeted.Count);
			int maxAmount = attackAction.Data.GetMaxTargetAmount(attackAction, m_linkedEntity, null) * attackAction.actualDuration;
			int[] targetTilesID = new int[maxAmount];
			int[] targetEntitiesID = new int[maxAmount];
			for (int i = 0; i < maxAmount; i++)
			{
				targetTilesID[i] = shouldAddTileInDirectionToTarget
					? from.GetNeighbor((HexDirection)GridManager.Instance.GetClosestOrientation(from, m_lastEntitiesTargeted[i % m_lastEntitiesTargeted.Count].Displacement.Coordinates.GetTile())).coordinates.ID
					: m_lastEntitiesTargeted[i % m_lastEntitiesTargeted.Count].Displacement.Coordinates.ID;
				targetEntitiesID[i] = m_lastEntitiesTargeted[i % m_lastEntitiesTargeted.Count].ID;
			}

			attackAction.linkedEquipmentId = equipmentID;
			attackAction.targetedEntityIDs = targetEntitiesID;
			attackAction.targetTileIDs = targetTilesID;
			attackAction.Init(GameAssets.current.game.entityActionsData[attackAction.enumID], equipmentID, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(attackAction, "Has enemy in range, action replaced with " + attackAction);
		}
		else if (canMove && !hasEnemyInVisionRange)
		{
			int orientationTowardTarget = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[_recordedAction.action.positionAtActionEndID]);
			bool isAtCorrectOrientation = orientationTowardTarget == m_linkedEntity.Displacement.CurrentOrientation;
			if (!isAtCorrectOrientation)
			{
				RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
				rotateAction.targetedOrientationID = new int[1] { orientationTowardTarget };
				rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
				resultInfo.ReplaceFreeAction(rotateAction, null);
			}
		}
		else if (canMove && hasEnemyInVisionRange && !hasEnemyInWeaponRange)
		{
			Entity closestEntity = GetClosestEnemyInVisionRange(true);
			bool isEntityInRangeWeaponsPossibleRange = IsEntityInWeaponPossibleRange(closestEntity, out string _weapon, true);
			int orientationTowardTarget = GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile());
			bool isAtCorrectOrientation = orientationTowardTarget == m_linkedEntity.Displacement.CurrentOrientation;

			if (_recordedAction.entityState == Entity.EntityState.Patroling)
			{
				//only rotate weapon, no movement if entity is too far
				TargetEntity(closestEntity);
				if (!isAtCorrectOrientation && isEntityInRangeWeaponsPossibleRange)
				{
					if (!isAtCorrectOrientation)
					{
						RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
						rotateAction.targetedOrientationID = new int[1] { orientationTowardTarget };
						rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceFreeAction(rotateAction, "Rotates toward target");
					}
				}
				else
				{
					Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();
					Tile targetTile = closestEntity.Displacement.Coordinates.GetTile();
					Tile firingTile = GetCommittedFiringTile(_recordedAction.action, closestEntity, true);

					if (firingTile == null)
						firingTile = GetClosestFiringTile(closestEntity, true);

					if (firingTile == null)
					{
						firingTile = GetClosestFreeNeighborOf(targetTile, true);
						if (firingTile == null)
							return resultInfo;
					}

					List<int> tileIDs = new();
					if (firingTile != from)
					{
						List<Tile> pathToFiringTile = GridManager.Instance.GetPath(from, firingTile, true, _movingEntity: m_linkedEntity, _canTraverseAllies: true);
						if (pathToFiringTile == null || pathToFiringTile.Count < 2)
							return resultInfo;

						pathToFiringTile.Reverse();
						for (int i = 0; i < movementAction.movementSpeed && i + 1 < pathToFiringTile.Count; i++)
							tileIDs.Add(pathToFiringTile[i + 1].coordinates.ID);
					}

					if (tileIDs.Count > 0)
					{
						MoveToTargetAction moveToAction = (TurnManager.Instance.GetAction(movementAction.enumID, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as MoveToTargetAction);
						moveToAction.mode = MoveToTargetAction.MoveActionMode.Coordinate;
						moveToAction.targetTileID = firingTile.coordinates.ID;
						moveToAction.targetTileIDs = tileIDs.ToArray();
						moveToAction.Init(GameAssets.current.game.entityActionsData[movementAction.enumID], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceAction(moveToAction, "Gets in position to shoot target");
					}
					else
					{
						WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction);
						waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceAction(waitAction, "Already in position to shoot target");
					}

					Tile orientationFrom = tileIDs.Count > 0 ? GridManager.Instance.Tiles[tileIDs[^1]] : from;
					int firingOrientation;

					if (CanFireFrom(orientationFrom, closestEntity, true))
					{
						Tile orientationTo = GridManager.Instance.Tiles[TurnManager.Instance.GetEntityPositionAtEndOfTick(closestEntity.ID, targetTile.coordinates.ID)];
						firingOrientation = orientationTo == orientationFrom
							? m_linkedEntity.Displacement.CurrentOrientation
							: GridManager.Instance.GetClosestOrientation(orientationFrom, orientationTo);
					}
					else
					{
						firingOrientation = orientationFrom == from
							? m_linkedEntity.Displacement.CurrentOrientation
							: GridManager.Instance.GetClosestOrientation(from, orientationFrom);
					}

					if (firingOrientation != m_linkedEntity.Displacement.CurrentOrientation)
					{
						RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
						rotateAction.targetedOrientationID = new int[1] { firingOrientation };
						rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
						resultInfo.ReplaceFreeAction(rotateAction, "Rotate to shoot target");
					}
				}
			}
			/*else if (_recordedAction.entityState == Entity.EntityState.NoAIChange)
			{
				//rotate weapon or move toward enemy if too far
				if (!isAtCorrectOrientation)
				{
					TargetEntity(closestEntity);
					RotateEntityAction rotateAction = (TurnManager.Instance.GetAction(EntityActionEnumID.RotateEntity, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction);
					rotateAction.targetedOrientationID = orientationTowardTarget;
					rotateAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
					resultInfo.ReplaceFreeAction(rotateAction, "Unit in vision but not in correct orientation");
				}

				if (!isEntityInRangeWeaponsPossibleRange)
					TargetEntity(null);


			}*/
		}
		else if (!canMove && _recordedAction.type != EntityActionEnumID.Wait && (_recordedAction.action.Data.type == EntityActionData.ActionType.Movement || _recordedAction.action.Data.type == EntityActionData.ActionType.Rotation
			 || _recordedAction.action.Data.codeType == EntityActionData.ActionCodeType.MoveThenAttack))
		{
			resultInfo.ReplaceAction(GetWaitActionFor(_recordedAction), "Unit cannot move");
		}

		if (needsTarget && resultInfo.replacedAction == _recordedAction.action)
			resultInfo.ReplaceAction(GetWaitActionFor(_recordedAction), "No target in reach for " + _recordedAction.type);

		return resultInfo;
	}

	private WaitAction GetWaitActionFor ( TurnManager.RecordedAction _recordedAction )
	{
		WaitAction waitAction = TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction;
		waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID
			, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
		return waitAction;
	}

	public void DOAllPrewarmCheck ( AEntityAction _action )
	{
		VisionCheck(_action);
		WeaponCheck(_action);
	}

	private List<System.Tuple<EntityActionData, string>> GetAvailableAttackAction ( bool _ignoreRemainingTokens = false )
	{
		List<System.Tuple<EntityActionData, string>> actionEquipmentPairs = new();
		foreach (EntityActionEnumID actionEnumID in m_linkedEntity.ComponentLinkedToAction.Keys)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
			if (!_ignoreRemainingTokens && data.GetTokenTotalCost(null, m_linkedEntity, null) > (GameConfig.current.game.actionTokenPerRound - TurnManager.currentTick))
				continue;

			if (data.type == EntityActionData.ActionType.DistanceAttack || data.type == EntityActionData.ActionType.MeleeAttack)
			{
				foreach (string equipmentID in m_linkedEntity.ComponentLinkedToAction[actionEnumID])
				{
					if (Condition.UseConditionPredicate(TurnManager.Instance.GetAction(actionEnumID, m_linkedEntity.ID, m_linkedEntity.ComponentLinkedToAction[actionEnumID][0], TurnManager.currentTick)
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
		if (!m_actionPriorityQueues.ContainsKey(EntityActionData.MainActionType.Movement))
			return null;

		foreach (EntityActionEnumID actionEnumID in m_actionPriorityQueues[EntityActionData.MainActionType.Movement].priorityQueue)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
			if (Condition.UseConditionPredicate(TurnManager.Instance.GetAction(actionEnumID, m_linkedEntity.ID, m_linkedEntity.ComponentLinkedToAction[actionEnumID][0], TurnManager.currentTick)
					, m_linkedEntity, m_lastEntitiesTargeted.Count > 0 ? m_lastEntitiesTargeted[0] : null, data.conditionType))
			{
				return data;
			}
		}
		return null;

	}

	public EntityActionData GetAttackOrSpecialAction ( int _remainingTokens, Entity _target = null )
	{
		EntityActionData specialAction = GetFirstAvailableActionIn(EntityActionData.MainActionType.Special, _remainingTokens, _target);
		return specialAction != null ? specialAction : GetFirstAvailableActionIn(EntityActionData.MainActionType.Attack, _remainingTokens, _target);
		/*EntityActionData attackData = GetFirstAvailableActionIn(EntityActionData.MainActionType.Attack, _remainingTokens, _target);
		return attackData != null ? attackData : GetFirstAvailableActionIn(EntityActionData.MainActionType.Special, _remainingTokens, _target);*/
	}

	private EntityActionData GetFirstAvailableActionIn ( EntityActionData.MainActionType _mainType, int _remainingTokens, Entity _target )
	{
		if (!m_actionPriorityQueues.ContainsKey(_mainType))
			return null;

		foreach (EntityActionEnumID actionEnumID in m_actionPriorityQueues[_mainType].priorityQueue)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
			if (!data.DoesResolveItsOwnTarget() || data.GetTokenTotalCost(null, m_linkedEntity, _target) > _remainingTokens)
				continue;

			if (Condition.UseConditionPredicate(TurnManager.Instance.GetAction(actionEnumID, m_linkedEntity.ID, m_linkedEntity.ComponentLinkedToAction[actionEnumID][0], TurnManager.currentTick)
					, m_linkedEntity, _target, data.conditionType))
				return data;
		}

		return null;
	}

	#region Vision

	private List<Entity> VisionCheck ( AEntityAction _action, bool _isThisTurn = true )
	{
		return RefreshEnemiesInVisionRange(_isThisTurn);
	}

	public List<Entity> RefreshEnemiesInVisionRange ( bool _isThisTurn = true )
	{
		m_entitiesInVisionRange.Clear();
		if (!GridManager.Instance.EntitiesVisions[m_linkedEntity.OwnerID].entitiesVisionRange.TryGetValue(m_linkedEntity, out HashSet<Tile> tilesInRange))
			return m_entitiesInVisionRange;

		foreach (Tile tile in tilesInRange)
		{
			int entityID = tile.GetEntityId(_isThisTurn);
			if (entityID != -1 && GameManager.Instance.GetEntityFromID(out Entity entity, entityID) && !entity.IsAlliedTo(m_linkedEntity.OwnerID))
				m_entitiesInVisionRange.Add(entity);
		}

		return m_entitiesInVisionRange;
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

	private List<EntityInRangeInfo> WeaponCheck ( AEntityAction _action, bool _isThisTurn = true )
	{
		m_entitiesInActionRangeInfos.Clear();

		foreach (System.Tuple<EntityActionData, string> pair in GetAvailableAttackAction())
		{
			AEntityAction relatedAction = _action.enumID == pair.Item1.enumID ? _action : TurnManager.Instance.GetAction(GameAssets.current.game.entityActionsData[pair.Item1.enumID], m_linkedEntity.ID, pair.Item2, _action.timeAtStart);

			List<Tile> tilesInWeaponCone = m_linkedEntity.Equipment.GetTilesInWeaponRange(relatedAction, _isThisTurn);
			foreach (Tile tile in tilesInWeaponCone)
			{
				Entity entityOnTile = tile.GetEntity(_isThisTurn);
				if (entityOnTile != null && !entityOnTile.IsAlliedTo(m_linkedEntity.OwnerID))
					m_entitiesInActionRangeInfos.Add(new() { tile = tile, actionID = pair.Item1.enumID, entity = entityOnTile, linkedEquipmentID = pair.Item2 });
			}
		}

		return m_entitiesInActionRangeInfos;
	}

	private bool HasEnemyInWeaponRange ( out List<Entity> _enemies, out EntityActionEnumID _attackEnumID, out string _equipmentID )
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
			return _enemies.Count > 0;
		}
	}

	#endregion

	#region Targeting

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
				if (GameAssets.current.game.entityActionsData[actionID].GetMaxRange(TurnManager.Instance.GetAction(actionID, m_linkedEntity.ID, weapon.ID, TurnManager.currentTick), m_linkedEntity, _entity) >= _entity.Displacement.Coordinates.GetTile().Distance)
				{
					_weapon = weapon.ID;
					return true;
				}
			}
		}

		return false;
	}

	private Tile GetClosestFreeNeighborOf ( Tile _tile, bool _isThisTurn = true )
	{
		Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();
		Tile closest = null;

		for (int i = 0; i < 6; i++)
		{
			Tile neighbor = _tile.GetNeighbor((HexDirection)i);
			if (neighbor == null || neighbor.Distance == int.MaxValue || neighbor.IsObstacle(_isThisTurn))
				continue;

			if (neighbor != from && neighbor.GetEntity(_isThisTurn) != null)
				continue;

			if (closest == null || neighbor.Distance < closest.Distance)
				closest = neighbor;
		}

		return closest;
	}

	private Tile GetCommittedFiringTile ( AEntityAction _currentAction, Entity _target, bool _isThisTurn = true )
	{
		if (_currentAction is not MoveToTargetAction currentMove || currentMove.finalTargetTileID == -1)
			return null;

		Tile committed = GridManager.Instance.Tiles[currentMove.finalTargetTileID];
		if (committed == null || committed.IsObstacle(_isThisTurn))
			return null;

		Entity occupant = committed.GetEntity(_isThisTurn);
		if (occupant != null && occupant != m_linkedEntity)
			return null;

		return CanFireFrom(committed, _target, _isThisTurn) ? committed : null;
	}

	public bool CanFireFrom ( Tile _from, Entity _target, bool _isThisTurn = true )
	{
		if (_from == null || _target == null)
			return false;

		Tile targetTile = _target.Displacement.Coordinates.GetTile();
		int orientation = GridManager.Instance.GetClosestOrientation(_from, targetTile);

		foreach (System.Tuple<EntityActionData, string> pair in GetAvailableAttackAction(_ignoreRemainingTokens: true))
		{
			AEntityAction attackAction = TurnManager.Instance.GetAction(pair.Item1, m_linkedEntity.ID, pair.Item2, TurnManager.currentTick);
			if (m_linkedEntity.Equipment.GetTilesInWeaponRange(attackAction, _isThisTurn, _from, orientation).Contains(targetTile))
				return true;
		}

		return false;
	}

	public Tile GetClosestFiringTile ( Entity _target, bool _isThisTurn = true )
	{
		if (_target == null)
			return null;

		Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();
		Tile targetTile = _target.Displacement.Coordinates.GetTile();

		if (CanFireFrom(from, _target, _isThisTurn))
			return from;

		GridManager.Instance.BFS(from, -1, null, _isThisTurn);

		Tile closestTile = null;
		int closestDistance = int.MaxValue;
		int closestRange = -1;

		foreach (System.Tuple<EntityActionData, string> pair in GetAvailableAttackAction(_ignoreRemainingTokens: true))
		{
			AEntityAction rangeAction = TurnManager.Instance.GetAction(pair.Item1, m_linkedEntity.ID, pair.Item2, TurnManager.currentTick);
			int maxRange = pair.Item1.GetMaxRange(rangeAction, m_linkedEntity, _target);

			foreach (Tile candidate in GridManager.Instance.GetTilesInVisionRange(targetTile, pair.Item1.minDistance, maxRange, false, _isThisTurn, true))
			{
				if (candidate.Distance == int.MaxValue || candidate.Distance > closestDistance || candidate.IsObstacle(_isThisTurn))
					continue;

				if (candidate != from && candidate.GetEntity(_isThisTurn) != null)
					continue;

				int candidateRange = Mathf.Max(Mathf.Abs(candidate.coordinates.X - targetTile.coordinates.X)
					, Mathf.Abs(candidate.coordinates.Y - targetTile.coordinates.Y)
					, Mathf.Abs(candidate.coordinates.Z - targetTile.coordinates.Z));
				if (candidate.Distance == closestDistance && candidateRange <= closestRange)
					continue;

				if (!CanFireFrom(candidate, _target, _isThisTurn))
					continue;

				closestTile = candidate;
				closestDistance = candidate.Distance;
				closestRange = candidateRange;
			}
		}

		return closestTile;
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

	private bool TryResolveEntityTarget ( TurnManager.RecordedAction _recordedAction, ref CheckActionResultInfo _resultInfo )
	{
		AEntityAction action = _recordedAction.action;
		Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();

		if (!TryGetTargetsInReach(action, from, out List<Entity> targets, out int orientation))
			return false;

		m_lastEntitiesTargeted = targets;

		AEntityAction targetedAction = TurnManager.Instance.GetAction(action.enumID, m_linkedEntity.ID, action.linkedEquipmentId, _recordedAction.timeAtStart);
		if (targetedAction == null)
			return false;

		targetedAction.Init(action.Data, action.linkedEquipmentId, m_linkedEntity.ID, action.supposedPositionAtActionStartID, action.timeAtStart);
		FillActionTargets(targetedAction, targets, from, out int[] targetTileIDs, out int[] targetedEntityIDs);
		targetedAction.SetResolvedTargets(targetTileIDs, targetedEntityIDs);
		_resultInfo.ReplaceAction(targetedAction, action.enumID + " targets " + targets[0].Data.name);

		if (orientation != m_linkedEntity.Displacement.CurrentOrientation)
		{
			EntityActionEnumID rotateEnumID = EntityActionEnumID.RotateToEntity;
			if (!GameAssets.current.game.entityActionsData.ContainsKey(rotateEnumID))
			{
				Debug.LogError("No action data registered for " + rotateEnumID + ", press ReloadActions on GameAssets. Falling back to " + EntityActionEnumID.RotateEntity, gameObject);
				rotateEnumID = EntityActionEnumID.RotateEntity;
			}

			RotateEntityAction rotateAction = TurnManager.Instance.GetAction(rotateEnumID, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as RotateEntityAction;
			rotateAction.Init(GameAssets.current.game.entityActionsData[rotateEnumID], null, m_linkedEntity.ID
				, action.supposedPositionAtActionStartID, action.timeAtStart);

			rotateAction.targetedOrientationID = new int[1] { orientation };
			rotateAction.SetResolvedTargets(targetTileIDs, targetedEntityIDs);
			_resultInfo.ReplaceFreeAction(rotateAction, "Rotates toward target");
		}

		return true;
	}

	private bool TryGetTargetsInReach ( AEntityAction _action, Tile _from, out List<Entity> _targets, out int _orientation )
	{
		_targets = new();
		_orientation = m_linkedEntity.Displacement.CurrentOrientation;

		Entity stickyTarget = m_lastEntitiesTargeted.Count > 0 ? m_lastEntitiesTargeted[0] : null;

		if (_action.Data.GetMainActionType() != EntityActionData.MainActionType.Attack)
		{
			_targets = GetEnemiesOn(GridManager.Instance.GetTilesInVisionRange(_from, _action.Data.minDistance
				, _action.Data.GetMaxRange(_action, m_linkedEntity, null), false, true, false), _from, stickyTarget);
			return _targets.Count > 0;
		}

		int bestRange = int.MaxValue;
		bool bestHoldsSticky = false;

		for (int i = 0; i < 6; i++)
		{
			int orientation = (m_linkedEntity.Displacement.CurrentOrientation + i) % 6;
			List<Entity> targetsInCone = GetEnemiesOn(m_linkedEntity.Equipment.GetTilesInWeaponRange(_action, true, _from, orientation), _from, stickyTarget);
			if (targetsInCone.Count == 0)
				continue;

			bool holdsSticky = stickyTarget != null && targetsInCone[0] == stickyTarget;
			int range = GetRangeBetween(_from, targetsInCone[0].Displacement.Coordinates.GetTile());

			if (_targets.Count > 0 && (bestHoldsSticky || (!holdsSticky && range >= bestRange)))
				continue;

			_targets = targetsInCone;
			_orientation = orientation;
			bestRange = range;
			bestHoldsSticky = holdsSticky;
		}

		return _targets.Count > 0;
	}

	private List<Entity> GetEnemiesOn ( IEnumerable<Tile> _tiles, Tile _from, Entity _stickyTarget )
	{
		List<Entity> enemies = new();
		foreach (Tile tile in _tiles)
		{
			Entity entity = tile.GetCurrentEntity();
			if (entity == null || entity == m_linkedEntity || entity.Equipment.IsDead
				|| entity.IsAlliedTo(m_linkedEntity.OwnerID) || enemies.Contains(entity))
				continue;

			enemies.Add(entity);
		}

		enemies.Sort(( _a, _b ) =>
		{
			bool isAsticky = _a == _stickyTarget;
			bool isBsticky = _b == _stickyTarget;
			if (isAsticky != isBsticky)
				return isAsticky ? -1 : 1;

			return GetRangeBetween(_from, _a.Displacement.Coordinates.GetTile())
				.CompareTo(GetRangeBetween(_from, _b.Displacement.Coordinates.GetTile()));
		});

		return enemies;
	}

	private void FillActionTargets ( AEntityAction _action, List<Entity> _targets, Tile _from, out int[] _targetTileIDs, out int[] _targetedEntityIDs )
	{
		bool shouldAddTileInDirectionToTarget = _action.Data.aoeType != EntityActionData.AOEType.Noone
			&& _action.Data.aoECenterType == EntityActionData.AOECenterType.Self;
		int maxAmount = _action.Data.GetMaxTargetAmount(_action, m_linkedEntity, null) * _action.actualDuration;

		_targetTileIDs = new int[maxAmount];
		_targetedEntityIDs = new int[maxAmount];
		for (int i = 0; i < maxAmount; i++)
		{
			Entity target = _targets[i % _targets.Count];
			_targetTileIDs[i] = shouldAddTileInDirectionToTarget
				? _from.GetNeighbor((HexDirection)GridManager.Instance.GetClosestOrientation(_from, target.Displacement.Coordinates.GetTile())).coordinates.ID
				: target.Displacement.Coordinates.ID;
			_targetedEntityIDs[i] = target.ID;
		}
	}

	private static int GetRangeBetween ( Tile _from, Tile _to )
	{
		return Mathf.Max(Mathf.Abs(_from.coordinates.X - _to.coordinates.X)
			, Mathf.Abs(_from.coordinates.Y - _to.coordinates.Y)
			, Mathf.Abs(_from.coordinates.Z - _to.coordinates.Z));
	}

	#endregion
}
