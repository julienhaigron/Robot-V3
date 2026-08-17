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

	public override void Init ( Entity _user, WeaponEquipmentData _data, bool _isFirstSide )
	{
		base.Init(_user, _data, _isFirstSide);

		m_timeBetweenBulletsWFS = new WaitForSeconds(m_timeBetweenEachBullet);
		m_aimDurationWFS = new WaitForSeconds(m_aimDuration);
		m_shootCooldownDurationWFS = new WaitForSeconds(m_shootCooldownDuration);
	}

	protected override IEnumerator AimTargetCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		if (_attackAction.Data.aoeType == EntityActionData.AOEType.Noone || (_attackAction.Data.aoeType == EntityActionData.AOEType.Circle && _attackAction.Data.targetType != EntityActionData.TargetType.Self))
		{
			Entity targetEntity = GameManager.Instance.GetEntityFromID(_attackAction.targetedEntityIDs[_attackIndex * _attackAction.ActiveLifetime]);
			Tile targetTile = GridManager.Instance.Tiles[_attackAction.targetTileIDs[_attackIndex]];
			Vector3 targetPosition = _attackAction.Data.targetType == EntityActionData.TargetType.Tile ? targetTile.transform.position : targetEntity.Skin.Center.position;
			yield return AimSingleTargetAnim(_attackAction, _attackInfo, targetPosition);
		}
		else
		{
			//handle aoe aim anims
		}

	}

	#region Aim Animations

	private IEnumerator AimSingleTargetAnim ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, Vector3 _targetPosition )
	{
		if (_attackInfo.isAttackSuccessfull)
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
			WeaponTarget target = targets[_attackIndex % targets.Count];
			//Entity targetEntity = GameManager.Instance.GetEntityFromID((int)_attackAction.targetedEntityIDs[_attackIndex]);
			yield return ShootAtEntityAnim(_attackAction, _attackIndex, _attackInfo, _lastSuccessfullAttackIndex, target);
		}
		else
		{
			if (!_attackInfo.isAttackSuccessfull)
				yield break;

			//handle aoe perform anims
			List<WeaponTarget> targets = GetTargets(_attackAction, _attackIndex);
			Dictionary<WeaponEquipmentData.DamageType, int> damages = BuildDamageDictionary(_attackInfo);

			foreach (ParticleSystem ps in m_onPerformPS)
				ps.Play();
			SoundManager.Instance.Play(_attackAction.Data.onPerformSingleAttackSFXID);

			foreach (WeaponTarget target in targets)
			{
				Entity targetEntity = target.targetEntity != null ? target.targetEntity : target.targetTile.GetEntity(true);
				if (targetEntity == null)
					continue;
				int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, targetEntity);
				for (int i = 0; i < hitAmount; i++)
				{
					targetEntity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
					{
						entityAttacker = m_user,
						entityTargeted = targetEntity,
						damages = damages
					});

					ApplyStatuses(targetEntity, _attackInfo);
					//ApplyEffects(entity);
				}
			}
			foreach (AEntityPassiveEffect.PassiveEffectContainer pe in m_lastPerformedAction.effects)
			{
				List<Entity> effectsTargets = GetPassiveEffectTargets(_attackAction, pe, _attackIndex);
				foreach (Entity entity in effectsTargets)
					ApplyEffects(entity, pe);
			}
		}
	}

	#region Shoot Animations

	private IEnumerator ShootAtEntityAnim ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex, WeaponTarget _target )
	{
		bool hasTrajectoryProjectileBuff = m_lastPerformedAction.effects.Any(e => e.enumID == EntityPassiveEffectEnumID.TrajectoryControl);
		int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, _target.targetEntity != null ? _target.targetEntity : _target.targetTile.GetEntity(true));
		ProjectileData bulletData = new()
		{
			owner = m_user,
			speed = Vector2.right * m_speed,
			attackData = _attackAction.Data,
			weapon = m_data,
			onHitSFXID = _attackAction.Data.onSingleAttackHitSFXID
		};
		ProjectileData.TargetType targetType = !_attackInfo.isAttackSuccessfull && _attackInfo.hittedTileID != -1
			? ProjectileData.TargetType.Wall :  _attackAction.Data.targetType == EntityActionData.TargetType.Tile
			? ProjectileData.TargetType.Tile : ProjectileData.TargetType.Entity;
		bulletData.SetTarget(targetType, targetType == ProjectileData.TargetType.Tile ? _target.targetTile : null
			, targetType == ProjectileData.TargetType.Entity ? _target.targetEntity : null
			, targetType == ProjectileData.TargetType.Wall ? GridManager.Instance.Tiles[_attackInfo.hittedTileID].Wall : null);

		for (int i = 0; i < hitAmount; i++)
		{
			foreach (ParticleSystem ps in m_onPerformPS)
				ps.Play();
			SoundManager.Instance.Play(_attackAction.Data.onPerformSingleAttackSFXID);

			bool isLastBullet = i == hitAmount - 1 && _attackIndex == _lastSuccessfullAttackIndex;
			m_bulletPool.Get<Projectile>(m_bulletPoint.position, m_bulletPoint.rotation).SetProjectileDataAndLaunch(bulletData
				, ( entity ) => ApplyBulletHit(entity, _attackInfo, isLastBullet), () => OnProjectileDespawn(isLastBullet), hasTrajectoryProjectileBuff);

			yield return m_timeBetweenBulletsWFS;
		}
	}

	#endregion

	private void ApplyBulletHit ( Entity _entity, AttackAction.SingleAttackInfo _attackInfo, bool _isLastBullet )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = BuildDamageDictionary(_attackInfo);
		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			damages = damages
		});

		ApplyStatuses(_entity, _attackInfo);

		foreach (AEntityPassiveEffect.PassiveEffectContainer pe in m_lastPerformedAction.effects)
			ApplyEffects(_entity, pe);

		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	private void OnProjectileDespawn ( bool _isLastBullet )
	{
		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	protected override IEnumerator OnEndAttackingCR ( AttackAction _attackAction )
	{
		m_user.Skin.ReleaseAim(_attackAction.linkedEquipmentId);

		yield return m_shootCooldownDurationWFS;
	}
}