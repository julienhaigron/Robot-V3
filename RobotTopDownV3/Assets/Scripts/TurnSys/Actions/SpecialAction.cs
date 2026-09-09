using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpecialAction : AEntityAction
{
	private const float ProjectileTimeout = 10f;

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

	protected virtual bool DoesOwnItsCompletion => false;

	protected virtual void ApplyActionEffect ()
	{
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		if (DoesOwnItsCompletion)
		{
			DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, EndTick);
			return;
		}

		GameManager.Instance.StartCoroutine(PlayVisualEffectCR());
	}

	private IEnumerator PlayVisualEffectCR ()
	{
		switch (Data.visualEffectType)
		{
			case EntityActionData.VisualEffectType.Projectile:
				yield return ProjectileVisualCR();
				break;

			case EntityActionData.VisualEffectType.Spell:
				yield return SpellVisualCR();
				break;

			default:
				yield return CharacterAnimationVisualCR();
				break;
		}

		EndTick();
	}

	//The action animation is already triggered by OnStartPerform, so this only holds the tick open for it.
	private IEnumerator CharacterAnimationVisualCR ()
	{
		yield return new WaitForSeconds(Data.visualEffectDuration);
		ApplyActionEffect();
	}

	private IEnumerator SpellVisualCR ()
	{
		if (Data.spellVfxPrefab == null)
		{
			yield return CharacterAnimationVisualCR();
			yield break;
		}

		GameObject vfx = Object.Instantiate(Data.spellVfxPrefab, GetVisualTargetPosition(), Quaternion.identity);

		yield return new WaitForSeconds(Data.visualEffectDuration);
		ApplyActionEffect();

		Object.Destroy(vfx, Data.spellVfxLifetime);
	}

	private IEnumerator ProjectileVisualCR ()
	{
		Entity user = PerformingEntity;
		Tile targetTile = GetVisualTargetTile();
		Entity targetEntity = GetVisualTargetEntity();

		if (Data.projectilePool == null || user == null || (targetTile == null && targetEntity == null))
		{
			yield return CharacterAnimationVisualCR();
			yield break;
		}

		ProjectileData projectileData = new()
		{
			owner = user,
			speed = Vector2.right * Data.projectileSpeed,
			attackData = Data,
			onHitSFXID = Data.onSingleAttackHitSFXID,
			onHitVFXPool = Data.onSingleAttackHitVFXPool,
			isAttackSuccessful = true,
			damages = new()
		};

		projectileData.SetTarget(targetEntity != null ? ProjectileData.TargetType.Entity : ProjectileData.TargetType.Tile
			, targetEntity != null ? null : targetTile, targetEntity, null);

		bool isProjectileDone = false;
		bool wasEffectApplied = false;

		Projectile projectile = Data.projectilePool.Get<Projectile>(GetProjectileOrigin(user), Quaternion.identity);
		projectile.SetProjectileDataAndLaunch(projectileData
			, ( impactTile ) =>
			{
				wasEffectApplied = true;
				ApplyActionEffect();
			}
			, () => isProjectileDone = true
			, false);

		//A projectile that never despawns would hold the tick open for good, so the sequence gives up on it.
		float elapsed = 0f;
		while (!isProjectileDone && elapsed < ProjectileTimeout)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}

		if (!wasEffectApplied)
			ApplyActionEffect();
	}

	private Vector3 GetProjectileOrigin ( Entity _user )
	{
		return _user.Skin != null && _user.Skin.Center != null ? _user.Skin.Center.position : _user.transform.position;
	}

	private Vector3 GetVisualTargetPosition ()
	{
		Entity targetEntity = GetVisualTargetEntity();
		if (targetEntity != null && targetEntity.Skin != null && targetEntity.Skin.Center != null)
			return targetEntity.Skin.Center.position;

		Tile targetTile = GetVisualTargetTile();
		if (targetTile != null)
			return targetTile.transform.position;

		Entity user = PerformingEntity;
		return user == null ? Vector3.zero : user.transform.position;
	}

	private Tile GetVisualTargetTile ()
	{
		if (targetTileIDs == null)
			return null;

		foreach (int tileID in targetTileIDs)
		{
			if (tileID >= 0 && tileID < GridManager.Instance.Tiles.Length)
				return GridManager.Instance.Tiles[tileID];
		}

		return null;
	}

	private Entity GetVisualTargetEntity ()
	{
		if (targetedEntityIDs == null)
			return null;

		foreach (int entityID in targetedEntityIDs)
		{
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			if (entity != null && !entity.Equipment.IsDead)
				return entity;
		}

		return null;
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
