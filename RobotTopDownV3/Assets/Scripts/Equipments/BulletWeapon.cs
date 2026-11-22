using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Sirenix.OdinInspector;

public class BulletWeapon : Weapon
{
	[Title("Dependencies")]
	[SerializeField] private Transform m_bulletPoint;
	[SerializeField] private PoolData m_bulletPool;

	[Title("Parameters")]
	[SerializeField] private float m_speed;
	[SerializeField] private float m_timeBetweenEachBullet = .5f;
	[SerializeField] private float m_aimDuration = 1f;
	[SerializeField] private float m_shootCooldownDuration = .3f;

	protected override Transform LaunchPoint => m_bulletPoint != null ? m_bulletPoint : transform;

	private static bool FiresBullets ( EntityActionData _data )
	{
		return _data.aoeType == EntityActionData.AOEType.Noone || (_data.aoeType == EntityActionData.AOEType.Circle && _data.targetType != EntityActionData.TargetType.Self);
	}

	protected override float EstimateAttackDuration ( AttackAction _attackAction )
	{
		int attackCount = _attackAction.attacksInfos.Length;
		if (!FiresBullets(_attackAction.Data))
			return attackCount * m_aimDuration + base.EstimateAttackDuration(_attackAction);

		int bulletCount = 0;
		for (int i = 0; i < attackCount; i++)
			bulletCount += _attackAction.Data.GetHitAmount(_attackAction, m_user, _attackAction.GetTargetEntityAt(i));

		return attackCount * m_aimDuration + Mathf.Max(0, bulletCount - 1) * m_timeBetweenEachBullet
			+ GetDistanceToTarget(_attackAction, attackCount - 1) / Mathf.Max(1f, m_speed);
	}

	protected override IEnumerator AimTargetCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		if (FiresBullets(_attackAction.Data))
		{
			Entity targetEntity = _attackAction.GetTargetEntityAt(_attackIndex);
			Tile targetTile = _attackAction.GetTargetTileAt(_attackIndex);
			if (targetTile == null)
				yield break;

			Vector3 targetPosition = _attackAction.Data.targetType == EntityActionData.TargetType.Tile || targetEntity == null
				? targetTile.transform.position
				: targetEntity.Skin.Center.position;

			Tile curveTile = _attackInfo.CurveTile;
			if (curveTile != null)
				targetPosition = curveTile.transform.position + Vector3.up * (targetPosition.y - targetTile.transform.position.y);

			yield return AimSingleTargetAnim(_attackAction, _attackInfo, targetPosition, curveTile != null);
		}
		else
		{
			Tile aimedTile = _attackAction.GetTargetTileAt(_attackIndex);
			if (aimedTile != null && m_user.Skin != null)
			{
				m_user.Skin.VisualyAimAt(_attackAction.linkedEquipmentId, aimedTile.transform.position + Vector3.up * .5f, _attackAction.Data);
				yield return new WaitForSeconds(Paced(m_aimDuration));
			}
		}

	}

	#region Aim Animations

	private IEnumerator AimSingleTargetAnim ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, Vector3 _targetPosition, bool _isCurvedShot )
	{
		if (_attackInfo.isAttackSuccessfull || _isCurvedShot)
			m_user.Skin.VisualyAimAt(_attackAction.linkedEquipmentId, _targetPosition, _attackAction.Data);
		else
		{
			Vector3 OT = (_targetPosition - ProjectileOrigin).normalized;
			Vector3 perpendicular = Vector3.Cross(OT, Vector3.up).normalized;
			float distance = 1f;

			Vector3 adjacentPos = UnityEngine.Random.Range(0, 2) == 0
				? _targetPosition + perpendicular * distance
				: _targetPosition - perpendicular * distance;

			if (_attackInfo.hittedTileID != -1)
				adjacentPos = GridManager.Instance.Tiles[_attackInfo.hittedTileID].transform.position + (Vector3.up * .25f);

			m_user.Skin.VisualyAimAt(_attackAction.linkedEquipmentId, adjacentPos, _attackAction.Data);
		}

		yield return new WaitForSeconds(Paced(m_aimDuration));
	}

	#endregion

	protected override IEnumerator PerformSingleAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex )
	{
		if (FiresBullets(_attackAction.Data))
		{
			List<WeaponTarget> targets = GetTargets(_attackAction, _attackIndex);

			if (targets.Count == 0)
			{
				if (_attackIndex == _lastSuccessfullAttackIndex)
					EndAttack(m_lastPerformedAction);
				yield break;
			}

			Tile aimedTile = _attackAction.GetTargetTileAt(_attackIndex);
			WeaponTarget target = targets.Find(candidate => candidate.targetTile == aimedTile) ?? targets[0];
			yield return ShootAtEntityAnim(_attackAction, _attackIndex, _attackInfo, _lastSuccessfullAttackIndex, target);
		}
		else
			yield return base.PerformSingleAttackCR(_attackAction, _attackIndex, _attackInfo, _lastSuccessfullAttackIndex);
	}

	#region Shoot Animations

	private IEnumerator ShootAtEntityAnim ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex, WeaponTarget _target )
	{
		int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, _target.ResolvedEntity);
		ProjectileData bulletData = new()
		{
			owner = m_user,
			speed = Vector2.right * (m_speed / Pace),
			attackData = _attackAction.Data,
			weapon = m_data,
			onHitSFXID = _attackAction.Data.onSingleAttackHitSFXID,
			onHitVFXPool = _attackAction.Data.onSingleAttackHitVFXPool,
			isAttackSuccessful = _attackInfo.isAttackSuccessfull,
			damages = BuildDamageDictionary(_attackInfo)
		};

		ProjectileData.TargetType targetType = !_attackInfo.isAttackSuccessfull && _attackInfo.hittedTileID != -1
			? ProjectileData.TargetType.Wall
			: _attackAction.Data.targetType == EntityActionData.TargetType.Tile || _target.targetEntity == null
			? ProjectileData.TargetType.Tile : ProjectileData.TargetType.Entity;
		bulletData.SetTarget(targetType, targetType == ProjectileData.TargetType.Tile ? _target.targetTile : null
			, targetType == ProjectileData.TargetType.Entity ? _target.targetEntity : null
			, targetType == ProjectileData.TargetType.Wall ? GridManager.Instance.Tiles[_attackInfo.hittedTileID].Wall : null);
		Tile curveTile = GetFlightCurveTile(_attackInfo, targetType);

		for (int i = 0; i < hitAmount; i++)
		{
			foreach (ParticleSystem ps in m_onPerformPS)
				ps.Play();
			SoundManager.Instance.Play(_attackAction.Data.onPerformSingleAttackSFXID);
			Pose launch = LaunchPose;
			if (IsAttackVisible())
				PlayOrientedVFX(_attackAction.Data.onPerformVFXPool, launch.position, launch.forward, _attackAction.Data.name);
			if (i == 0)
				PlayAttackAnimation(_attackAction, _attackInfo);
			PlayShotRecoil(_attackAction.Data);

			bool isLastBullet = i == hitAmount - 1 && _attackIndex == _lastSuccessfullAttackIndex;
			Entity missedTarget = i == hitAmount - 1 ? _target.targetEntity : null;
			m_bulletPool.Get<Projectile>(launch.position, launch.rotation).SetProjectileDataAndLaunch(bulletData
				, ( impactTile ) => ApplyBulletImpact(impactTile, _attackAction, _attackInfo, isLastBullet, _target.targetEntity), () => OnProjectileDespawn(isLastBullet, missedTarget, _attackInfo.didTargetDodge), curveTile);

			yield return new WaitForSeconds(Paced(m_timeBetweenEachBullet));
		}
	}

	private Tile GetFlightCurveTile ( AttackAction.SingleAttackInfo _attackInfo, ProjectileData.TargetType _targetType )
	{
		Tile curveTile = _attackInfo.CurveTile;
		if (curveTile == null || _targetType != ProjectileData.TargetType.Wall)
			return curveTile;

		Tile hitTile = GridManager.Instance.Tiles[_attackInfo.hittedTileID];
		return GridManager.Instance.GetLineOfSightPath(m_user.Displacement.Coordinates.GetTile(), curveTile).Contains(hitTile) ? null : curveTile;
	}

	#endregion
	
	private void ApplyBulletImpact ( Tile _impactTile, AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, bool _isLastBullet, Entity _designatedTarget )
	{
		if (_impactTile == null)
		{
			if (_isLastBullet)
				EndAttack(m_lastPerformedAction);
			return;
		}

		Dictionary<WeaponEquipmentData.DamageType, int> damages = BuildDamageDictionary(_attackInfo);

		List<Tile> impactedTiles = _attackAction.Data.aoeType != EntityActionData.AOEType.Noone
			? m_user.Equipment.GetTilesInAoERange(_attackAction, _impactTile, true)
			: new List<Tile>() { _impactTile };

		if (_attackAction.Data.aoeType != EntityActionData.AOEType.Noone)
			ShowAoEDodges(_attackAction, impactedTiles);

		bool wasDesignatedTargetHit = false;

		foreach (Tile tile in impactedTiles)
		{
			if (tile.Wall != null && tile.Wall.Health > 0)
				tile.Wall.TakeDamage(damages);

			if (!tile.TryGetCurrentEntity(out Entity entity))
				continue;

			wasDesignatedTargetHit |= entity == _designatedTarget;
			ApplyDamageAndEffects(_attackAction, entity, damages, _attackInfo);
		}

		if (!wasDesignatedTargetHit && _designatedTarget != null && !_designatedTarget.Equipment.IsDead
			&& impactedTiles.Contains(_attackAction.GetExchangeTileOf(_designatedTarget)))
			ApplyDamageAndEffects(_attackAction, _designatedTarget, damages, _attackInfo);

		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	private void ApplyDamageAndEffects ( AttackAction _attackAction, Entity _entity, Dictionary<WeaponEquipmentData.DamageType, int> _damages, AttackAction.SingleAttackInfo _attackInfo )
	{
		DealDamage(_attackAction, _attackInfo, _entity, _damages);
		ApplyStatuses(_entity, _attackInfo);

		foreach (AEntityPassiveEffect.PassiveEffectContainer pe in m_lastPerformedAction.effects)
			ApplyEffects(_entity, pe);
	}

	private void OnProjectileDespawn ( bool _isLastBullet, Entity _missedTarget, bool _didTargetDodge )
	{
		//Projectile only calls this back when the bullet hit nothing at all, which is exactly a miss
		ShowMissOn(_missedTarget, _didTargetDodge);

		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	protected override IEnumerator OnEndAttackingCR ( AttackAction _attackAction )
	{
		if (!_attackAction.Data.KeepsAimAfterVolley(_attackAction.IsLastPerformTick, _attackAction.cooldownDuration))
			m_user.Skin.ReleaseAim();

		yield return new WaitForSeconds(Paced(m_shootCooldownDuration));
	}
}