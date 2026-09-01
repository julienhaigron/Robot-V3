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

	private List<Tile> m_activeAttackRangeTiles = new();

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

	private void Awake ()
	{
		m_linkedEntity.onEndTick += OnEndTick;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.onEndTick -= OnEndTick;
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

	private void OnEndTick ()
	{
		foreach (Tile tile in m_activeAttackRangeTiles)
		{
			tile.UI.ResetOutline();
		}
		m_activeAttackRangeTiles.Clear();
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
			WaitAction waitAction = (TurnManager.Instance.GetAction(EntityActionEnumID.Wait, m_linkedEntity.ID, null, _recordedAction.timeAtStart) as WaitAction);
			waitAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], null, m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID, _recordedAction.action.timeAtStart);
			resultInfo.ReplaceAction(waitAction, "Unit is stun");
			return resultInfo;
		}
		else if (_recordedAction.entityState == Entity.EntityState.NoAIChange)
		{
			//no action change if in guard
			return resultInfo;
		}

		DOAllPrewarmCheck(_recordedAction.action);

		EntityActionData movementAction = GetMovementAction();
		bool canMove = !m_linkedEntity.Status.Contains(EntityStatusEnumID.Stun) && !m_linkedEntity.Status.Contains(EntityStatusEnumID.Rooted) && movementAction != null;
		bool hasEnemyInWeaponRange = HasEnemyInWeaponRange(out List<Entity> enemies, out EntityActionEnumID attackEnumID, out string equipmentID);
		bool hasEnemyInVisionRange = HasEnemyInVisionRange();
		EntityActionData.MainActionType currentActionMainType = _recordedAction.action.Data.GetMainActionType();

		if (hasEnemyInWeaponRange && currentActionMainType != EntityActionData.MainActionType.Attack)
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
					//Head for the closest tile the unit could actually fire from rather than for the target
					//itself, so it stops as soon as it is in position instead of walking into the enemy.
					Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();
					Tile targetTile = closestEntity.Displacement.Coordinates.GetTile();
					//Several tiles around a target are equally good firing positions, all the more so in melee
					//where every neighbour qualifies. Re-picking one every tick makes the destination hop, the
					//path with it, and the facing flip, so the committed one is kept while it stays valid.
					Tile firingTile = GetCommittedFiringTile(_recordedAction.action, closestEntity, true);

					if (firingTile == null)
						firingTile = GetClosestFiringTile(closestEntity, true);

					if (firingTile == null)
					{
						//No reachable firing position: close in on the target, but never take its own tile as a
						//destination. The pathfinding exempts the destination from its occupancy check, so an
						//occupied destination is walked straight into.
						firingTile = GetClosestFreeNeighborOf(targetTile, true);
						if (firingTile == null)
							return resultInfo;
					}

					List<int> tileIDs = new();
					if (firingTile != from)
					{
						//GetPath walks back from its destination, so the origin comes first once reversed
						List<Tile> pathToFiringTile = GridManager.Instance.GetPath(from, firingTile, true, _movingEntity: m_linkedEntity, _canTraverseAllies: true);
						if (pathToFiringTile == null || pathToFiringTile.Count < 2)
							return resultInfo;

						pathToFiringTile.Reverse();
						for (int i = 0; i < movementAction.movementSpeed && i + 1 < pathToFiringTile.Count; i++)
							tileIDs.Add(pathToFiringTile[i + 1].coordinates.ID);
					}

					//A lone ReplaceFreeAction would make TurnManager cancel the running action and re-enqueue
					//that very same object, so the main action is always replaced too.
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

					//Face the target from where the unit will stand at the end of THIS action, not from a firing
					//tile several ticks away: that would aim it somewhere that means nothing yet. It converges on
					//the firing orientation as the unit arrives.
					Tile orientationFrom = tileIDs.Count > 0 ? GridManager.Instance.Tiles[tileIDs[^1]] : from;
					//Aim where the target will stand once this tick is played, from where this unit will stand.
					//orientationFrom is an end of tick position, so taking the target's start of tick one aims a
					//step behind, and a target zig zagging to travel straight then flips the facing every tick.
					Tile orientationTo = GridManager.Instance.Tiles[TurnManager.Instance.GetEntityPositionAtEndOfTick(closestEntity.ID, targetTile.coordinates.ID)];
					int firingOrientation = orientationTo == orientationFrom
						? m_linkedEntity.Displacement.CurrentOrientation
						: GridManager.Instance.GetClosestOrientation(orientationFrom, orientationTo);

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

	//_ignoreRemainingTokens is for geometry questions only ("could I shoot from there"): whether an attack still
	//fits in this round is a scheduling matter, and mixing the two makes a unit believe it is weaponless on the
	//last tick, leave its firing position and walk off looking for one.
	private List<System.Tuple<EntityActionData, string>> GetAvailableAttackAction ( bool _ignoreRemainingTokens = false )
	{
		List<System.Tuple<EntityActionData, string>> actionEquipmentPairs = new();
		foreach (EntityActionEnumID actionEnumID in m_linkedEntity.ComponentLinkedToAction.Keys)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionEnumID];
			//remaining ticks include the current one, same count as TurnManager.remainingActionTickThisTurn
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

	#region Vision

	private List<Entity> VisionCheck ( AEntityAction _action, bool _isThisTurn = true )
	{
		m_entitiesInVisionRange.Clear();
		HashSet<Tile> tilesInRange = GridManager.Instance.EntitiesVisions[m_linkedEntity.OwnerID].entitiesVisionRange[m_linkedEntity];
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

		List<Tile> tilesInRange = new();
		foreach (System.Tuple<EntityActionData, string> pair in GetAvailableAttackAction())
		{
			AEntityAction relatedAction = _action.enumID == pair.Item1.enumID ? _action : TurnManager.Instance.GetAction(GameAssets.current.game.entityActionsData[pair.Item1.enumID], m_linkedEntity.ID, pair.Item2, _action.timeAtStart);

			List<Tile> tilesInWeaponCone = m_linkedEntity.Equipment.GetTilesInWeaponRange(relatedAction, _isThisTurn);
			tilesInRange.AddRange(tilesInWeaponCone);
			foreach (Tile tile in tilesInWeaponCone)
			{
				Entity entityOnTile = tile.GetEntity(_isThisTurn);
				if (entityOnTile != null && !entityOnTile.IsAlliedTo(m_linkedEntity.OwnerID))
					m_entitiesInActionRangeInfos.Add(new() { tile = tile, actionID = pair.Item1.enumID, entity = entityOnTile, linkedEquipmentID = pair.Item2 });
			}
		}
		DisplayActiveAttackRange(tilesInRange);
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

	private void DisplayActiveAttackRange ( List<Tile> _tilesInRange )
	{
		m_activeAttackRangeTiles = _tilesInRange;
		foreach (Tile tile in _tilesInRange)
		{
			tile.UI.SetOutlineColor(Color.blue);
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

	//Closest tile next to _tile that the unit could actually stop on. Relies on the distance map left by
	//GetClosestFiringTile, which always runs its BFS before it can return null.
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

	//Destination this action is already committed to, when it still holds up as a firing position. Returns null
	//when there is nothing to keep, so a fresh one has to be picked.
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

	//Can the unit hit _target standing on _from? Rotating is a free action, so the facing is taken as the one
	//that aims at the target, and the real weapon cone answers for every attack the unit currently has.
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

	//Closest tile the unit could actually fire on _target from, so the AI gets in position instead of walking
	//into the enemy. Rotating is a free action, so orientation never rules a tile out: only range and line of
	//sight do, and the real weapon cone from that tile confirms the candidate. Returns null if there is none.
	public Tile GetClosestFiringTile ( Entity _target, bool _isThisTurn = true )
	{
		if (_target == null)
			return null;

		Tile from = m_linkedEntity.Displacement.Coordinates.GetTile();
		Tile targetTile = _target.Displacement.Coordinates.GetTile();

		//The candidate pre filter below walks line of sight outwards from the target, which is not exactly the
		//check the weapon cone does from the shooter side. Never let that approximation reject the tile the unit
		//already stands on, or it steps aside only to come back into position next tick.
		if (CanFireFrom(from, _target, _isThisTurn))
			return from;

		//full distance map from the unit, GetClosestEnemyInVisionRange stops its own BFS at the target
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

				//Ties are broken on the range kept to the target: a deterministic second key, otherwise the
				//pick flips between equally close firing tiles as the unit walks and it weaves sideways.
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

	#endregion
}
