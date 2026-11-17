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

	private WaitForSeconds m_timeBetweenBulletsWFS;
	private WaitForSeconds m_aimDurationWFS;
	private WaitForSeconds m_shootCooldownDurationWFS;

	public override void Init ( Entity _user, WeaponEquipmentData _data, string _id, bool _isFirstSide )
	{
		base.Init(_user, _data, _id, _isFirstSide);

		m_timeBetweenBulletsWFS = new WaitForSeconds(m_timeBetweenEachBullet);
		m_aimDurationWFS = new WaitForSeconds(m_aimDuration);
		m_shootCooldownDurationWFS = new WaitForSeconds(m_shootCooldownDuration);
	}

	protected override IEnumerator AimTargetCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		if (_attackAction.Data.aoeType == EntityActionData.AOEType.Noone || (_attackAction.Data.aoeType == EntityActionData.AOEType.Circle && _attackAction.Data.targetType != EntityActionData.TargetType.Self))
		{
			Entity targetEntity = _attackAction.GetTargetEntityAt(_attackIndex);
			Tile targetTile = GridManager.Instance.Tiles[_attackAction.targetTileIDs[_attackIndex]];

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
			//handle aoe aim anims
		}

	}

	#region Aim Animations

	private IEnumerator AimSingleTargetAnim ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, Vector3 _targetPosition, bool _isCurvedShot )
	{
		if (_attackInfo.isAttackSuccessfull || _isCurvedShot)
			m_user.Skin.VisualyAimAt(_attackAction.linkedEquipmentId, _targetPosition);
		else
		{
			Vector3 OT = (_targetPosition - m_bulletPoint.position).normalized;
			Vector3 perpendicular = Vector3.Cross(OT, Vector3.up).normalized;
			float distance = 1f;

			Vector3 adjacentPos = UnityEngine.Random.Range(0, 2) == 0
				? _targetPosition + perpendicular * distance
				: _targetPosition - perpendicular * distance;

			if (_attackInfo.hittedTileID != -1)
				adjacentPos = GridManager.Instance.Tiles[_attackInfo.hittedTileID].transform.position + (Vector3.up * .25f);

			m_user.Skin.VisualyAimAt(_attackAction.linkedEquipmentId, adjacentPos);
		}

		yield return m_aimDurationWFS;
	}

	#endregion

	protected override IEnumerator PerformSingleAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex )
	{
		if (_attackAction.Data.aoeType == EntityActionData.AOEType.Noone || (_attackAction.Data.aoeType == EntityActionData.AOEType.Circle && _attackAction.Data.targetType != EntityActionData.TargetType.Self))
		{
			List<WeaponTarget> targets = GetTargets(_attackAction, _attackIndex);

			if (targets.Count == 0)
			{
				if (_attackIndex == _lastSuccessfullAttackIndex)
					EndAttack(m_lastPerformedAction);
				yield break;
			}

			WeaponTarget target = targets[_attackIndex % targets.Count];
			//Entity targetEntity = GameManager.Instance.GetEntityFromID((int)_attackAction.targetedEntityIDs[_attackIndex]);
			yield return ShootAtEntityAnim(_attackAction, _attackIndex, _attackInfo, _lastSuccessfullAttackIndex, target);
		}
		else
			yield return base.PerformSingleAttackCR(_attackAction, _attackIndex, _attackInfo, _lastSuccessfullAttackIndex);
	}

	#region Shoot Animations

	private IEnumerator ShootAtEntityAnim ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex, WeaponTarget _target )
	{
		int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, _target.targetEntity != null ? _target.targetEntity : _target.targetTile.GetCurrentEntity());
		ProjectileData bulletData = new()
		{
			owner = m_user,
			speed = Vector2.right * m_speed,
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

			bool isLastBullet = i == hitAmount - 1 && _attackIndex == _lastSuccessfullAttackIndex;
			Entity missedTarget = i == hitAmount - 1 ? _target.targetEntity : null;
			m_bulletPool.Get<Projectile>(m_bulletPoint.position, m_bulletPoint.rotation).SetProjectileDataAndLaunch(bulletData
				, ( impactTile ) => ApplyBulletImpact(impactTile, _attackAction, _attackInfo, isLastBullet, _target.targetEntity), () => OnProjectileDespawn(isLastBullet, missedTarget, _attackInfo.didTargetDodge), curveTile);

			yield return m_timeBetweenBulletsWFS;
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
			ApplyDamageAndEffects(entity, damages, _attackInfo);
		}

		if (!wasDesignatedTargetHit && _designatedTarget != null && !_designatedTarget.Equipment.IsDead
			&& impactedTiles.Contains(_attackAction.GetExchangeTileOf(_designatedTarget)))
			ApplyDamageAndEffects(_designatedTarget, damages, _attackInfo);

		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	private void ApplyDamageAndEffects ( Entity _entity, Dictionary<WeaponEquipmentData.DamageType, int> _damages, AttackAction.SingleAttackInfo _attackInfo )
	{
		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			entityAttacker = m_user,
			entityTargeted = _entity,
			damages = _damages
		});

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
		m_user.Skin.ReleaseAim(_attackAction.linkedEquipmentId);

		yield return m_shootCooldownDurationWFS;
	}
}