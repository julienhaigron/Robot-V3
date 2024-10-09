using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAIPlugin : EntityPlugin
{
	//triggers

	private List<Entity> m_entitiesInRange = new();
	private Dictionary<string, List<Entity>> m_entitiesInWeaponRange = new();

	private Entity m_lastEntityTargeted;
	public Entity TargetedEntity => m_lastEntityTargeted;

	public struct CheckActionResultInfo
	{
		public bool isActionChanging;
		public EntityActionType replacedActionType;

		public void ReplaceAction ( EntityActionType _replacedActionType )
		{
			isActionChanging = true;
			replacedActionType = _replacedActionType;
		}
	}

	public CheckActionResultInfo CheckAction ( TurnManager.RecordedAction _recordedAction )
	{
		CheckActionResultInfo resultInfo = new CheckActionResultInfo() { isActionChanging = false, replacedActionType = _recordedAction.action.type };
		// 1) Do all prewarm check (enemyInRange, weaponRange, ...)
		DOAllPrewardCheck();
		// 2) react depending on those factor

		if (HasEnemyWeaponInRange() && _recordedAction.entityState == Entity.EntityState.Patroling)
		{
			// if eneemy in weapon range
			//  => shoot directly
			m_lastEntityTargeted = GetClosestEnemyInRange(true);
			resultInfo.ReplaceAction(EntityActionType.Attack);
		}
		else if (HasEnemyInVisionRange() && !HasEnemyWeaponInRange())
		{
			bool isEntityTooFarForWeaponsRange = false;
			if (_recordedAction.entityState == Entity.EntityState.Patroling)
			{
				//only rotate weapon, no movement if entity is too far
				m_lastEntityTargeted = GetClosestEnemyInRange(true);
				resultInfo.ReplaceAction(isEntityTooFarForWeaponsRange ? EntityActionType.TargetTileMove : EntityActionType.RotateWeapon);
			}
			else if (_recordedAction.entityState == Entity.EntityState.Guarding)
			{
				//rotate weapon or move toward enemy if too far
				if (!isEntityTooFarForWeaponsRange)
				{
					m_lastEntityTargeted = GetClosestEnemyInRange(true);
					resultInfo.ReplaceAction(EntityActionType.RotateWeapon);
				}
			}
		}

		return resultInfo;
	}

	public void DOAllPrewardCheck ()
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
		return m_entitiesInRange.Count > 0;
	}

	private List<Entity> VisionCheck ( bool _isThisTurn = true )
	{
		m_entitiesInRange = null;
		if (m_linkedEntity.Data.capacities.Contains(EntityCapacityAsset.EntityCapacityType.VisualSensor)
			|| m_linkedEntity.Data.capacities.Contains(EntityCapacityAsset.EntityCapacityType.RadarSensor))
		{
			//m_entitiesInRange

			m_entitiesInRange = GridManager.Instance.GetEntitiesInRange(m_linkedEntity.Displacement.Coordinates.GetTile(), m_linkedEntity.Data.visibilityRange, _isThisTurn);
		}

		return m_entitiesInRange;
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
				if (entityOnTile != null && entityOnTile.Faction == Entity.EntityFaction.Enemy)
					m_entitiesInWeaponRange[weaponId].Add(entityOnTile);
			}
		}
		return m_entitiesInWeaponRange;
	}

	#endregion

	#region Targeting

	public Entity GetClosestEnemyInRange (bool _isThisTurn = true)
	{
		GridManager.Instance.BFS(m_linkedEntity.Displacement.Coordinates.GetTile(), -1, null, _isThisTurn);

		Entity closestEntity = null;
		foreach (string weaponId in m_entitiesInWeaponRange.Keys)
		{
			foreach (Entity entity in m_entitiesInWeaponRange[weaponId])
			{
				if(closestEntity == null || entity.Displacement.Coordinates.GetTile().Distance < closestEntity.Displacement.Coordinates.GetTile().Distance)
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
