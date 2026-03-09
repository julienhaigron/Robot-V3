using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BulletWeapon : Weapon
{
	[SerializeField] private Transform m_bulletPoint;
	[SerializeField] private Transform m_leftHandTarget;

	[SerializeField, Min(1)] private int m_bulletPerShoot = 1;
	[SerializeField] private float m_speed;
	[SerializeField] private float m_timeBetweenEachBullet = .5f;
	[SerializeField] private float m_aimDuration = 1f;
	[SerializeField] private float m_shootCooldownDuration = .3f;
	[SerializeField] private PoolData m_bulletPool;

	private ProjectileData m_bulletData;
	private WaitForSeconds m_timeBetweenBulletsWFS;
	private WaitForSeconds m_aimDurationWFS;
	private WaitForSeconds m_shootCooldownDurationWFS;
	private Coroutine m_shootCR;

	private List<Entity> m_entitiesHitByLastShot = new();
	private AttackAction m_lastPerformedAction;
	private Action m_onPerformAttackEnd;

	private void OnDestroy ()
	{
		if (m_shootCR != null)
			StopCoroutine(m_shootCR);
	}

	public override void Init ( Entity _user, WeaponEquipmentData _data, bool _isFirstSide )
	{
		base.Init(_user, _data, _isFirstSide);

		m_bulletData = new()
		{
			//gravityMultiplier = gravityMultiplier,
			owner = _user,
			speed = Vector2.right * m_speed,
			weapon = _data
		};

		m_timeBetweenBulletsWFS = new WaitForSeconds(m_timeBetweenEachBullet);
		m_aimDurationWFS = new WaitForSeconds(m_aimDuration);
		m_shootCooldownDurationWFS = new WaitForSeconds(m_shootCooldownDuration);
	}

	public override void PerformAttack ( AttackAction _attackAction, Action _onPerformEnd )
	{
		m_lastPerformedAction = _attackAction;
		m_entitiesHitByLastShot.Clear();
		m_onPerformAttackEnd = _onPerformEnd;

		Entity performingEntity = GameManager.Instance.GetEntityFromID(_attackAction.performingEntityID);
		Entity targetEntity = GameManager.Instance.GetEntityFromID((int)_attackAction.targetedEntityID);
		Vector3 targetPosition = targetEntity.Skin.Center.position;

		//1) aim at position
		if (_attackAction.isAttackSuccessfull)
		{
			List<Entity> targetEntities = new();
			EntityActionData attackData = GameAssets.current.game.entityActionsData[_attackAction.enumID];

			if (attackData.isAoe)
			{
				List<Tile> tilesInRange = m_user.Equipment.GetTilesInAoERange(_attackAction, true);
				foreach (Tile tile in tilesInRange)
				{
					Entity entityOnTIle = tile.GetEntity(true);
					if (entityOnTIle != null /*&& !entityOnTIle.IsAlliedTo(m_user.OwnerID)*/)
						targetEntities.Add(entityOnTIle);
				}

				Dictionary<WeaponEquipmentData.DamageType, int> damages = new Dictionary<WeaponEquipmentData.DamageType, int>();
				for (int i = 0; i < m_lastPerformedAction.damageTypes.Length; i++)
				{
					damages.Add((WeaponEquipmentData.DamageType)m_lastPerformedAction.damageTypes[i], m_lastPerformedAction.damages[i]);
				}

				foreach (Entity entity in targetEntities)
				{
					entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damages });

					/*//aplly status here
					for (int i = 0; i < _attackAction.areStatusesSuccess.Length; i++)
					{
						if (_attackAction.areStatusesSuccess[i])
							GameAssets.current.game.entityStatus[(EntityStatusEnumID)_attackAction.statusIds[i]].ApplyStatus(entity);
					}*/

					foreach (EntityPassiveEffectEnumID passiveEffectID in m_user.KnownedPassiveEffectsPerAction[_attackAction.enumID])
					{
						GameAssets.current.game.entityEffects[passiveEffectID].ApplyEffect(m_user, entity);
					}
				}

				foreach (EntityPassiveEffectEnumID passiveEffectID in m_user.KnownedPassiveEffectsPerAction[_attackAction.enumID])
				{
					if (GameAssets.current.game.entityEffects[passiveEffectID] is ApplyStatusPassiveEffect applyStatus && applyStatus.doApplyToTile)
					{
						foreach (Tile tile in tilesInRange)
							applyStatus.ApplyEffect(tile);
					}
				}

				//TODO : add aoe damage anim/viosual/effect here
				DOVirtual.DelayedCall(1f, () => EndAttack(_attackAction));
				return;
			}
			else
				GameManager.Instance.GetEntityFromID(_attackAction.targetedEntityID);

			performingEntity.Skin.VisualyAimAt(_attackAction.attackingWeaponId, targetPosition);
		}
		else
		{
			//show failure
			Vector3 OT = (targetPosition - m_bulletPoint.position).normalized;
			Vector3 perpendicular = Vector3.Cross(OT, Vector3.up).normalized;
			float distance = 1f;
			Vector3 adjacentPos = UnityEngine.Random.Range(0, 2) == 0 ? targetPosition + perpendicular * distance : targetPosition - perpendicular * distance;
			performingEntity.Skin.VisualyAimAt(_attackAction.attackingWeaponId, adjacentPos);
		}

		//2) shoot at aimed position
		if (m_shootCR != null)
			StopCoroutine(m_shootCR);
		m_shootCR = StartCoroutine(ShootCR(_attackAction));
	}


	private IEnumerator ShootCR ( AttackAction _attackAction )
	{
		yield return m_aimDurationWFS;

		for (int i = 0; i < m_bulletPerShoot; i++)
		{
			m_bulletPool.Get<Projectile>(m_bulletPoint.transform.position, m_bulletPoint.rotation).SetProjectileDataAndLaunch(m_bulletData, OnBulletHit);

			yield return m_timeBetweenBulletsWFS;
		}

		yield return m_shootCooldownDurationWFS;

		EndAttack(m_lastPerformedAction);
		m_shootCR = null;
	}

	private void OnBulletHit ( Entity _entityHit )
	{
		if (m_entitiesHitByLastShot.Contains(_entityHit) || m_lastPerformedAction == null || m_lastPerformedAction.damageTypes == null)
			return;

		m_entitiesHitByLastShot.Add(_entityHit);
		//apply damage
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new Dictionary<WeaponEquipmentData.DamageType, int>();
		for (int i = 0; i < m_lastPerformedAction.damageTypes.Length; i++)
		{
			damages.Add((WeaponEquipmentData.DamageType)m_lastPerformedAction.damageTypes[i], m_lastPerformedAction.damages[i]);
		}

		_entityHit.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damages });

		//aplly effects
		/*for (int i = 0; i < m_lastPerformedAction.areStatusesSuccess.Length; i++)
		{
			if (m_lastPerformedAction.areStatusesSuccess[i])
				GameAssets.current.game.entityStatus[(EntityStatusEnumID)m_lastPerformedAction.statusIds[i]].ApplyStatus(_entityHit);
		}*/

		foreach (EntityPassiveEffectEnumID passiveEffectID in m_user.KnownedPassiveEffectsPerAction[m_lastPerformedAction.enumID])
		{
			GameAssets.current.game.entityEffects[passiveEffectID].ApplyEffect(m_user, _entityHit);
		}

	}

	private void EndAttack ( AttackAction _attackAction )
	{
		GameManager.Instance.GetEntityFromID(_attackAction.performingEntityID).Skin.ReleaseAim(_attackAction.attackingWeaponId);
		m_onPerformAttackEnd?.Invoke();
	}
}
