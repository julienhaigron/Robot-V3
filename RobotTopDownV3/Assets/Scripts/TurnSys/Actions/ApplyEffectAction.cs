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
		if (Data.isAoe)
		{
			Entity user = GameManager.Instance.GetEntityFromID(performingEntityID);
			int maxDist = GameAssets.current.equipments[user.ComponentLinkedToAction[enumID]] is WeaponEquipmentData ?
				(GameAssets.current.equipments[user.ComponentLinkedToAction[enumID]] as WeaponEquipmentData).range
				: (GameAssets.current.equipments[user.ComponentLinkedToAction[enumID]] as ToolEquipmentData).range;
			List<Tile> tilesInEffectRange = GridManager.Instance.GetTilesInVisionRange(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], maxDist, true);
			foreach(Tile tile in tilesInEffectRange)
			{
				if(Data.targetType == EntityActionData.TargetType.Tile)
				{
					foreach (EntityPassiveEffectEnumID effect in Data.passiveEffects)
						GameAssets.current.game.entityEffects[effect].ApplyEffect(tile);
				}
				else
				{
					foreach (EntityPassiveEffectEnumID effect in Data.passiveEffects)
						GameAssets.current.game.entityEffects[effect].ApplyEffect(tile.GetEntity(true), GameManager.Instance.GetEntityFromID(targetTileID));
				}
			}
		}
		else
		{
			foreach (EntityPassiveEffectEnumID effect in Data.passiveEffects)
				GameAssets.current.game.entityEffects[effect].ApplyEffect(GameManager.Instance.GetEntityFromID(performingEntityID), GameManager.Instance.GetEntityFromID(targetTileID));
		}

		base.Perform(_state);
		DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, EndTick);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
