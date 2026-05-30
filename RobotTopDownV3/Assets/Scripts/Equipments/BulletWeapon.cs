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

	private ProjectileData m_bulletData;

	private WaitForSeconds m_timeBetweenBulletsWFS;
	private WaitForSeconds m_aimDurationWFS;
	private WaitForSeconds m_shootCooldownDurationWFS;

	public override void Init ( Entity _user, WeaponEquipmentData _data, bool _isFirstSide )
	{
		base.Init(_user, _data, _isFirstSide);

		m_bulletData = new()
		{
			owner = _user,
			speed = Vector2.right * m_speed,
			weapon = _data
		};

		m_timeBetweenBulletsWFS = new WaitForSeconds(m_timeBetweenEachBullet);
		m_aimDurationWFS = new WaitForSeconds(m_aimDuration);
		m_shootCooldownDurationWFS = new WaitForSeconds(m_shootCooldownDuration);
	}

	protected override IEnumerator AimTargetCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		Entity targetEntity = GameManager.Instance.GetEntityFromID((int)_attackAction.targetedEntityIDs[_attackIndex]);
		Vector3 targetPosition = targetEntity.Skin.Center.position;

		if (_attackInfo.isAttackSuccessfull)
			m_user.Skin.VisualyAimAt(_attackAction.attackingWeaponId, targetPosition);
		else
		{
			Vector3 OT = (targetPosition - m_bulletPoint.position).normalized;
			Vector3 perpendicular = Vector3.Cross(OT, Vector3.up).normalized;
			float distance = 1f;

			Vector3 adjacentPos = UnityEngine.Random.Range(0, 2) == 0
				? targetPosition + perpendicular * distance
				: targetPosition - perpendicular * distance;

			m_user.Skin.VisualyAimAt( _attackAction.attackingWeaponId, adjacentPos);
		}

		yield return m_aimDurationWFS;
	}

	protected override IEnumerator PerformSingleAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex )
	{
		if (!_attackInfo.isAttackSuccessfull)
			yield break;

		Entity targetEntity = GameManager.Instance.GetEntityFromID((int)_attackAction.targetedEntityIDs[_attackIndex]);
		int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, targetEntity);
		bool hasTrajectoryProjectileBuff = m_lastPerformedAction.effects.Any(e => e.enumID == EntityPassiveEffectEnumID.TrajectoryControl);

		for (int i = 0; i < hitAmount; i++)
		{
			bool isLastBullet = i == hitAmount - 1 && _attackIndex == _lastSuccessfullAttackIndex;
			m_bulletPool.Get<Projectile>(m_bulletPoint.position, m_bulletPoint.rotation)
				.SetProjectileDataAndLaunch(m_bulletData
				, ( entity ) => ApplyBulletHit(entity, _attackInfo, isLastBullet), () => OnProjectileDespawn(isLastBullet), hasTrajectoryProjectileBuff);

			yield return m_timeBetweenBulletsWFS;
		}
	}

	private void ApplyBulletHit ( Entity _entity, AttackAction.SingleAttackInfo _attackInfo, bool _isLastBullet )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = BuildDamageDictionary(_attackInfo);
		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			damages = damages
		});

		ApplyStatuses(_entity, _attackInfo);
		ApplyEffects(_entity);

		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	private void OnProjectileDespawn (bool _isLastBullet)
	{
		if (_isLastBullet)
			EndAttack(m_lastPerformedAction);
	}

	protected override IEnumerator OnEndAttackingCR ( AttackAction _attackAction )
	{
		yield return m_shootCooldownDurationWFS;

		m_user.Skin.ReleaseAim(_attackAction.attackingWeaponId);
	}
}