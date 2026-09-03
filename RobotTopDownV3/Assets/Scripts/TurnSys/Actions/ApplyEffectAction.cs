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
		//Guarded: a Tile targeted effect can legitimately have no entity recorded at all.
		int targetAmount = targetedEntityIDs == null ? 0 : targetedEntityIDs.Length;
		for (int targetCount = 0; targetCount < targetAmount; targetCount++)
		{
			if (Data.aoeType != EntityActionData.AOEType.Noone)
			{
				Entity user = GameManager.Instance.GetEntityFromID(performingEntityID);
				int maxDist = Data.GetMaxRange(this, PerformingEntity, null);
				int minDist = Data.minDistance;
				List<Tile> tilesInEffectRange = GridManager.Instance.GetTilesInVisionRange(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], minDist, maxDist, false, true, false);
				foreach (Tile tile in tilesInEffectRange)
				{
					if (Data.targetType == EntityActionData.TargetType.Tile)
					{
						foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
							GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(tile);
					}
					else
					{
						foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
							GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(tile.GetCurrentEntity(), GameManager.Instance.GetEntityFromID(targetedEntityIDs[targetCount]), effect);
					}
				}
			}
			else
			{
				foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
					GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(GameManager.Instance.GetEntityFromID(performingEntityID), GameManager.Instance.GetEntityFromID(targetedEntityIDs[targetCount]), effect);
			}
		}

		//base.Perform now schedules EndTick itself, scheduling it here too would end the action twice.
		base.Perform(_state);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
