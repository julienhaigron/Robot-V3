using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpecialAction : AEntityAction
{
	private const float ProjectileTimeout = 3f;
	private float m_visualBudget;
	public const float WeaponAimDuration = .3f;
	public const float WeaponAimHold = .4f;
	public bool isActionCanceled;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref isActionCanceled);
	}

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

		List<string> targetNames = GetTargetedEntityNames();
		Entity firstTarget = targetNames.Count == 0 || targetedEntityIDs == null
			? null : GameManager.Instance.GetEntityFromID(targetedEntityIDs[0]);

		LocalizationManager localization = LocalizationManager.Instance;

		LogConsole.AddLog(TagLogMessage(string.Format(localization.Get(LocalizationKey.log_use_tool), user.Data.name
				, Data.GetLocalizedName()
				, targetNames.Count == 0 ? localization.Get(LocalizationKey.log_use_weapon_ground) : string.Join(", ", targetNames)))
			, LogConsole.LogEventType.UseTool
			, new LogConsole.LogDetails(new ActionTooltipContent(Data, user.Data)));

		foreach (ApplyStatusPassiveEffect statusEffect in Data.GetAppliedStatusEffects(this, user, firstTarget))
		{
			AEntityStatus status = statusEffect.Status;

			LogConsole.AddLog(string.Format(localization.Get(LocalizationKey.log_effect), status.GetLocalizedName())
				, LogConsole.LogEventType.UseTool
				, new LogConsole.LogDetails(LogCard.Status(status, null, statusEffect.duration, statusEffect.duration)));
		}
	}

	public override void ConflictCheckPrewarm ()
	{
		base.ConflictCheckPrewarm();
		isActionCanceled = false;
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (!Data.ClaimsTargetTiles || isActionCanceled || _otherAction is not MoveToTargetAction otherMove || !otherMove.TargetsAnyOf(targetTileIDs))
			return default;

		if (WinsTieBreak(EntityActionData.PFC(Data, otherMove.Data)))
		{
			otherMove.DropPath();
			return new() { isSecondActionConflicted = true };
		}

		isActionCanceled = true;
		return new() { isFirstActionConflicted = true };
	}

	protected virtual bool DoesOwnItsCompletion => false;

	protected virtual void ApplyActionEffect ()
	{
		Entity user = PerformingEntity;
		if (user == null || Data.passiveEffects == null || Data.passiveEffects.Length == 0)
			return;

		if (HasEffectOnOthers())
		{
			if (Data.aoeType != EntityActionData.AOEType.Noone)
				ApplyOnAoEZone(user);
			else
				ApplyOnRecordedTargets(user);
		}

		foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
		{
			if (effect.targetType == EntityActionData.TargetType.Self)
				GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(user, user, effect, null);
		}
	}

	private bool HasEffectOnOthers ()
	{
		foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
		{
			if (effect.targetType != EntityActionData.TargetType.Self)
				return true;
		}

		return false;
	}

	private void ApplyOnAoEZone ( Entity _user )
	{
		HashSet<Tile> affectedTiles = new();
		List<Entity> affectedEntities = new();
		List<Tile> effectOrigins = new();

		foreach (Tile center in GetAoECenterTiles())
		{
			foreach (Tile tile in _user.Equipment.GetTilesInAoERange(this, center))
			{
				if (!affectedTiles.Add(tile))
					continue;

				if (Data.doesAffectTile)
				{
					foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
					{
						if (effect.targetType != EntityActionData.TargetType.Self)
							GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(tile);
					}
				}

				Entity hitEntity = tile.GetCurrentEntity();
				if (hitEntity == null || affectedEntities.Contains(hitEntity))
					continue;

				affectedEntities.Add(hitEntity);
				effectOrigins.Add(center);
			}
		}

		for (int i = 0; i < affectedEntities.Count; i++)
			ApplyEffectsOnTarget(_user, affectedEntities[i], effectOrigins[i]);
	}

	private void ApplyOnRecordedTargets ( Entity _user )
	{
		int targetAmount = targetedEntityIDs == null ? 0 : targetedEntityIDs.Length;
		for (int targetCount = 0; targetCount < targetAmount; targetCount++)
		{
			Entity target = GameManager.Instance.GetEntityFromID(targetedEntityIDs[targetCount]);
			if (target != null)
				ApplyEffectsOnTarget(_user, target, null);
		}
	}

	private void ApplyEffectsOnTarget ( Entity _user, Entity _target, Tile _originTile )
	{
		foreach (AEntityPassiveEffect.PassiveEffectContainer effect in Data.passiveEffects)
		{
			if (effect.targetType != EntityActionData.TargetType.Self)
				GameAssets.current.game.entityEffects[effect.enumID].ApplyEffect(_user, _target, effect, _originTile);
		}
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

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		if (DoesOwnItsCompletion)
		{
			DG.Tweening.DOVirtual.DelayedCall(TickTimeLeft, EndTick);
			return;
		}

		m_visualBudget = TickTimeLeft;
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

	private IEnumerator CharacterAnimationVisualCR ()
	{
		yield return new WaitForSeconds(Mathf.Min(Data.visualEffectDuration, m_visualBudget));
		ApplyActionEffect();
	}

	private IEnumerator SpellVisualCR ()
	{
		if (Data.spellVfxPrefab == null)
		{
			yield return CharacterAnimationVisualCR();
			yield break;
		}

		GameObject vfx = UnityEngine.Object.Instantiate(Data.spellVfxPrefab, GetVisualTargetPosition(), Data.spellVfxPrefab.transform.rotation);

		yield return new WaitForSeconds(Mathf.Min(Data.visualEffectDuration, m_visualBudget));
		ApplyActionEffect();

		UnityEngine.Object.Destroy(vfx, Data.spellVfxLifetime);
	}

	private IEnumerator ProjectileVisualCR ()
	{
		Entity user = PerformingEntity;
		Tile targetTile = GetVisualTargetTile();
		Entity targetEntity = GetVisualTargetEntity();

		bool isProjectileDone = false;
		bool wasEffectApplied = false;
		bool isAiming = Data.projectilePool != null && user != null && (targetTile != null || targetEntity != null) && AimLinkedWeapon(user, targetEntity);
		if (isAiming)
			yield return new WaitForSeconds(WeaponAimDuration);

		if (Data.projectilePool == null || user == null || (targetTile == null && targetEntity == null)
			|| !TryLaunchProjectile(user, targetTile, targetEntity
				, () => { wasEffectApplied = true; ApplyActionEffect(); }
				, () => isProjectileDone = true))
		{
			if (isAiming)
				ReleaseLinkedWeaponAim(user);
			yield return CharacterAnimationVisualCR();
			yield break;
		}

		float elapsed = 0f;
		while (!isProjectileDone && elapsed < ProjectileTimeout)
		{
			elapsed += Time.deltaTime;
			if (isAiming && elapsed >= WeaponAimHold)
			{
				isAiming = false;
				ReleaseLinkedWeaponAim(user);
			}
			yield return null;
		}

		if (isAiming)
			ReleaseLinkedWeaponAim(user);
		if (!wasEffectApplied)
			ApplyActionEffect();
	}

	private bool AimLinkedWeapon ( Entity _user, Entity _targetEntity )
	{
		if (string.IsNullOrEmpty(linkedEquipmentId) || _user.Skin == null || !_user.Equipment.Weapons.TryGetValue(linkedEquipmentId, out Weapon weapon) || weapon == null)
			return false;

		Vector3 target = GetVisualTargetPosition();
		if (_targetEntity == null)
			target += Vector3.up * .5f;
		_user.Skin.VisualyAimAt(linkedEquipmentId, target, Data);
		return true;
	}

	private void ReleaseLinkedWeaponAim ( Entity _user )
	{
		if (_user != null && _user.Skin != null)
			_user.Skin.ReleaseAim();
	}

	private bool TryLaunchProjectile ( Entity _user, Tile _targetTile, Entity _targetEntity, Action _onImpact, Action _onDespawn )
	{
		try
		{
			Vector3 origin = GetProjectileOrigin(_user);
			Vector3 direction = GetVisualTargetPosition() - origin;

			Quaternion rotation = direction.sqrMagnitude > .0001f
				? Quaternion.LookRotation(direction.normalized)
				: _user.transform.rotation;

			Projectile projectile = Data.projectilePool.Get<Projectile>(origin, rotation);
			if (projectile == null)
				return false;

			if (_user.IsAlliedTo(GameManager.Instance.PlayerID) || _user.IsVisible)
				Weapon.PlayOrientedVFX(Data.onPerformVFXPool, origin, direction, Data.name);
			if (!string.IsNullOrEmpty(linkedEquipmentId) && _user.Equipment.Weapons.TryGetValue(linkedEquipmentId, out Weapon weapon) && weapon != null)
				weapon.PlayShotRecoil(Data);

			ProjectileData projectileData = new()
			{
				owner = _user,
				speed = Vector2.right * Mathf.Max(Data.projectileSpeed, direction.magnitude / Mathf.Max(.1f, m_visualBudget - WeaponAimDuration)),
				attackData = Data,
				onHitSFXID = Data.onSingleAttackHitSFXID,
				onHitVFXPool = Data.onSingleAttackHitVFXPool,
				isAttackSuccessful = true,
				damages = new()
			};

			projectileData.SetTarget(_targetEntity != null ? ProjectileData.TargetType.Entity : ProjectileData.TargetType.Tile
				, _targetEntity != null ? null : _targetTile, _targetEntity, null);

			projectile.SetProjectileDataAndLaunch(projectileData, ( impactTile ) => _onImpact(), _onDespawn);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("Projectile visual effect failed for action " + Data.name + ", falling back on the animation: " + ex);
			return false;
		}
	}

	private Vector3 GetProjectileOrigin ( Entity _user )
	{
		if (!string.IsNullOrEmpty(linkedEquipmentId) && _user.Equipment.Weapons.TryGetValue(linkedEquipmentId, out Weapon weapon) && weapon != null)
			return weapon.ProjectileOrigin;

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

	private HashSet<Tile> m_tilesInRangeCache;
	private int m_tilesInRangeCacheFrame = -1;
	private int m_tilesInRangeCacheOrigin = -1;
	private int m_tilesInRangeCacheMin = -1;
	private int m_tilesInRangeCacheMax = -1;

	private HashSet<Tile> GetTilesInRangeOf ( int _originID, int _minDist, int _maxDist )
	{
		if (m_tilesInRangeCache == null || m_tilesInRangeCacheFrame != Time.frameCount || m_tilesInRangeCacheOrigin != _originID
			|| m_tilesInRangeCacheMin != _minDist || m_tilesInRangeCacheMax != _maxDist)
		{
			m_tilesInRangeCache = new HashSet<Tile>(GridManager.Instance.GetTilesInVisionRange(GridManager.Instance.Tiles[_originID], _minDist, _maxDist, false, true, true));
			m_tilesInRangeCacheFrame = Time.frameCount;
			m_tilesInRangeCacheOrigin = _originID;
			m_tilesInRangeCacheMin = _minDist;
			m_tilesInRangeCacheMax = _maxDist;
		}

		return m_tilesInRangeCache;
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		int originID = TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID);
		if (Data.targetType == EntityActionData.TargetType.Self && _tile.coordinates.ID == originID)
			return true;

		int maxDist = Data.GetMaxRange(this, PerformingEntity, null);
		int minDist = Data.minDistance;
		bool isInRange = GetTilesInRangeOf(originID, minDist, maxDist).Contains(_tile);

		if (Data.targetType == EntityActionData.TargetType.Tile && isInRange)
			return true;

		Entity entity = _tile.GetCurrentEntity();
		return entity != null && isInRange && !entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID);
	}
}
