using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpecialAction : AEntityAction
{

	public override void Prepare ( Entity.EntityState _state )
	{
		if (TurnManager.currentTick == TimeAtStartPerform)
			LogUseSpecialAction();
	}

	protected void LogUseSpecialAction ()
	{
		Entity user = PerformingEntity;
		if (user == null)
			return;

		List<string> targetNames = new();
		if (targetedEntityIDs != null)
		{
			foreach (int targetID in targetedEntityIDs)
			{
				Entity target = GameManager.Instance.GetEntityFromID(targetID);
				if (target != null && !targetNames.Contains(target.Data.name))
					targetNames.Add(target.Data.name);
			}
		}

		Entity firstTarget = targetNames.Count == 0 || targetedEntityIDs == null
			? null : GameManager.Instance.GetEntityFromID(targetedEntityIDs[0]);

		LocalizationManager localization = LocalizationManager.Instance;

		LogConsole.AddLog(TagLogMessage(string.Format(localization.Get(LocalizationKey.log_use_tool), user.Data.name
				, Data.GetLocalizedName()
				, targetNames.Count == 0 ? localization.Get(LocalizationKey.log_use_weapon_ground) : string.Join(", ", targetNames)))
			, LogConsole.LogEventType.UseTool
			, new LogConsole.LogDetails("usetool_" + LogConsole.Instance.LogsDetails.Keys.Count, Data.GetLocalizedName(), Data.GetDescription()));

		foreach (AEntityStatus status in Data.GetAppliedStatuses(this, user, firstTarget))
		{
			LogConsole.AddLog(string.Format(localization.Get(LocalizationKey.log_effect), status.GetLocalizedName())
				, LogConsole.LogEventType.UseTool
				, new LogConsole.LogDetails("effect_" + LogConsole.Instance.LogsDetails.Keys.Count, status.GetLocalizedName(), status.GetTooltip(status.duration)));
		}
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, EndTick);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (targetTileIDs == null)
			return;

		foreach (int tileID in targetTileIDs)
		{
			if (tileID == -1 || GridManager.Instance.Tiles.Length <= tileID)
				continue;

			Tile tile = GridManager.Instance.Tiles[tileID];
			tile.UI.SetOutlineColor(Color.yellow);
		}
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (Data.targetType == EntityActionData.TargetType.Self && _tile.coordinates.ID == TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID))
			return true;

		int maxDist = Data.GetMaxRange(this, PerformingEntity, null);
		int minDist = Data.minDistance;
		bool isInRange = GridManager.Instance.GetTilesInVisionRange(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], minDist, maxDist, false, true, true).Contains(_tile);

		if (Data.targetType == EntityActionData.TargetType.Tile && isInRange)
			return true;

		Entity entity = _tile.GetCurrentEntity();
		return entity != null && isInRange && !entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID);
	}
}
