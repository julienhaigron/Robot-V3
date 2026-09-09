using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class ApplyEffectAction : SpecialAction
{

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?
		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		Entity user = PerformingEntity;

		if (user != null && Data.passiveEffects != null && Data.passiveEffects.Length > 0)
		{
			if (Data.aoeType != EntityActionData.AOEType.Noone)
				ApplyOnAoEZone(user);
			else
				ApplyOnRecordedTargets(user);
		}

		//base.Perform now schedules EndTick itself, scheduling it here too would end the action twice.
		base.Perform(_state);
	}

	private void ApplyOnAoEZone ( Entity _user )
	{
		HashSet<Tile> affectedTiles = new();
		List<Entity> affectedEntities = new();
		List<Tile> effectOrigins = new();

		//The whole zone is read before anything is applied: an effect that displaces a unit would otherwise
		//change the occupancy of tiles this scan has not reached yet.
		foreach (Tile center in GetAoECenterTiles())
		{
			foreach (Tile tile in _user.Equipment.GetTilesInAoERange(this, center))
			{
				if (!affectedTiles.Add(tile))
					continue;

				if (Data.doesAffectTile)
				{
					foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
						GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(tile);
				}

				Entity hitEntity = tile.GetCurrentEntity();
				if (hitEntity == null || affectedEntities.Contains(hitEntity))
					continue;

				affectedEntities.Add(hitEntity);
				effectOrigins.Add(center);
			}
		}

		for (int i = 0; i < affectedEntities.Count; i++)
			ApplyEveryEffectOn(_user, affectedEntities[i], effectOrigins[i]);
	}

	private void ApplyOnRecordedTargets ( Entity _user )
	{
		int targetAmount = targetedEntityIDs == null ? 0 : targetedEntityIDs.Length;
		for (int targetCount = 0; targetCount < targetAmount; targetCount++)
		{
			Entity target = GameManager.Instance.GetEntityFromID(targetedEntityIDs[targetCount]);
			if (target != null)
				ApplyEveryEffectOn(_user, target, null);
		}
	}

	private void ApplyEveryEffectOn ( Entity _user, Entity _target, Tile _originTile )
	{
		foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
			GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(_user, _target, effect, _originTile);
	}

	private List<Tile> GetAoECenterTiles ()
	{
		List<Tile> centers = new();

		if (Data.aoECenterType == EntityActionData.AOECenterType.Target && targetTileIDs != null)
		{
			foreach (int tileID in targetTileIDs)
			{
				if (tileID < 0 || tileID >= GridManager.Instance.Tiles.Length)
					continue;

				Tile tile = GridManager.Instance.Tiles[tileID];
				if (!centers.Contains(tile))
					centers.Add(tile);
			}
		}

		if (centers.Count == 0)
			centers.Add(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)]);

		return centers;
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
