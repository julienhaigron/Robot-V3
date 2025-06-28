using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityAIPlugin : EntityPlugin
{
	//triggers

	private List<Entity> m_entitiesInVisionRange = new();
	private Dictionary<string, List<Entity>> m_entitiesInWeaponRange = new();

	private Entity m_lastEntityTargeted;
	public Entity TargetedEntity => m_lastEntityTargeted;

	public struct CheckActionResultInfo
	{
		public bool isActionChanging;
		public AEntityAction replacedAction;

		public void ReplaceAction ( AEntityAction _replacedAction )
		{
			isActionChanging = true;
			replacedAction = _replacedAction;
		}
	}

	public CheckActionResultInfo CheckAction ( TurnManager.RecordedAction _recordedAction )
	{
		CheckActionResultInfo resultInfo = new CheckActionResultInfo() { isActionChanging = false, replacedAction = _recordedAction.action };
		// 1) Do all prewarm check (enemyInSeight, weaponRange, ...)
		DOAllPrewarmCheck();
		// 2) react depending on those factor

		if (HasEnemyWeaponInRange()/* && _recordedAction.entityState == Entity.EntityState.Patroling*/)
		{
			// if eneemy in weapon range
			//  => shoot directly
			m_lastEntityTargeted = GetClosestEnemyInWeaponRange(out string _weaponId, true);

			AttackAction attackAction = (TurnManager.Instance.GetAction(EntityActionType.Attack, m_linkedEntity.ID) as AttackAction);
			attackAction.attackingWeaponId = _weaponId;
			resultInfo.ReplaceAction(attackAction);
		}
		else if (HasEnemyInVisionRange() && !HasEnemyWeaponInRange())
		{
			Entity closestEntity = GetClosestEnemyInVisionRange(true);
			bool isEntityInRangeWeaponsRange = IsEntityInWeaponPossibleRange(closestEntity, out string _weapon, true);
			if (_recordedAction.entityState == Entity.EntityState.Patroling)
			{
				//only rotate weapon, no movement if entity is too far
				m_lastEntityTargeted = closestEntity;
				if (isEntityInRangeWeaponsRange)
				{
					RotateWeaponAction rotateAction = (TurnManager.Instance.GetAction(EntityActionType.RotateWeapon, m_linkedEntity.ID) as RotateWeaponAction);
					rotateAction.rotatingWeaponID = _weapon;
					resultInfo.ReplaceAction(rotateAction);
				}
				else
				{
					List<Tile> pathToEnemy = GridManager.Instance.GetPath(m_linkedEntity.Displacement.Coordinates.GetTile(), closestEntity.Displacement.Coordinates.GetTile(), true);
					if (pathToEnemy == null || pathToEnemy.Count < 2)
						return resultInfo;

					MoveToTargetAction moveToAction = (TurnManager.Instance.GetAction(EntityActionType.TargetTileMove, m_linkedEntity.ID) as MoveToTargetAction);
					moveToAction.mode = MoveToTargetAction.MoveActionMode.Entity;
					moveToAction.targetEntiyID = closestEntity.ID;
					moveToAction.thisActionDestinationID = pathToEnemy[1].coordinates.ID;
					moveToAction.Init(GameAssets.current.game.entityActionsData[EntityActionType.NeighborMove], m_linkedEntity.ID, _recordedAction.action.supposedPositionAtActionStartID);
					resultInfo.ReplaceAction(moveToAction);
				}
			}
			else if (_recordedAction.entityState == Entity.EntityState.Guarding)
			{
				//rotate weapon or move toward enemy if too far
				if (isEntityInRangeWeaponsRange)
				{
					m_lastEntityTargeted = closestEntity;
					RotateWeaponAction rotateAction = (TurnManager.Instance.GetAction(EntityActionType.RotateWeapon, m_linkedEntity.ID) as RotateWeaponAction);
					rotateAction.rotatingWeaponID = _weapon;
					resultInfo.ReplaceAction(rotateAction);
				}
				else
				{
					m_lastEntityTargeted = null;
				}
			}
		}

		return resultInfo;
	}

	public void DOAllPrewarmCheck ()
	{
		VisionCheck();
		WeaponCheck();
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
		foreach(Entity entity in m_entitiesInVisionRange)
		{
			if (entity.Faction != m_linkedEntity.Faction)
				return true;
		}
		return false;
	}

	private List<Entity> VisionCheck ( bool _isThisTurn = true )
	{
		m_entitiesInVisionRange = new();
		if (m_linkedEntity.Data.capacities.Contains(EntityCapacityAsset.EntityCapacityType.VisualSensor)
			|| m_linkedEntity.Data.capacities.Contains(EntityCapacityAsset.EntityCapacityType.RadarSensor))
		{
			m_entitiesInVisionRange = GridManager.Instance.GetEntitiesInRange(m_linkedEntity.Displacement.Coordinates.GetTile(), m_linkedEntity.Data.visibilityRange, _isThisTurn);
		}

		return m_entitiesInVisionRange;
	}

	private Dictionary<string, List<Entity>> WeaponCheck ( bool _isThisTurn = true )
	{
		m_entitiesInWeaponRange.Clear();
		foreach (string weaponId in m_linkedEntity.Equipment.Weapons.Keys)
		{
			m_entitiesInWeaponRange.Add(weaponId, new());
			List<Tile> tilesInWeaponCone = m_linkedEntity.Equipment.GetTilesInRange(weaponId);
			foreach (Tile tile in tilesInWeaponCone)
			{
				Entity entityOnTile = tile.GetEntity(_isThisTurn);
				if (entityOnTile != null && entityOnTile.Faction != m_linkedEntity.Faction)
					m_entitiesInWeaponRange[weaponId].Add(entityOnTile);
			}
		}
		return m_entitiesInWeaponRange;
	}


	#endregion

	#region Targeting

	public bool IsEntityInWeaponRange ( Entity _targetEntity, out Weapon _attackingWeapon )
	{
		foreach (string _weaponId in m_entitiesInWeaponRange.Keys)
		{
			foreach (Entity entity in m_entitiesInWeaponRange[_weaponId])
			{
				if (entity == _targetEntity)
				{
					_attackingWeapon = m_linkedEntity.Equipment.Weapons[_weaponId];
					return true;
				}
			}
		}

		_attackingWeapon = null;
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
			if (entity.Faction == m_linkedEntity.Faction)
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
