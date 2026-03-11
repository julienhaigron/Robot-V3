using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Linq;

public class Weapon : MonoBehaviour
{

	protected WeaponEquipmentData m_data;
	public WeaponEquipmentData Data => m_data;

	[SerializeField] protected List<ParticleSystem> m_onPerformPS;

	protected Entity m_user;

	public virtual void Init ( Entity _user, WeaponEquipmentData _data, bool _isFirstSide )
	{
		m_user = _user;
		m_data = _data;
	}

	public virtual void PerformAttack ( AttackAction _attackAction, Action _onPerformEnd )
	{
		if (_attackAction.isAttackSuccessfull)
		{
			List<Entity> targetEntities = new();
			EntityActionData attackData = GameAssets.current.game.entityActionsData[_attackAction.enumID];

			if (attackData.isAoe)
			{
				foreach (Tile tile in m_user.Equipment.GetTilesInAoERange(_attackAction, true))
				{
					Entity entityOnTIle = tile.GetEntity(true);
					if (entityOnTIle != null /*&& !entityOnTIle.IsAlliedTo(m_user.OwnerID)*/)
						targetEntities.Add(entityOnTIle);
				}
			}
			else
				GameManager.Instance.GetEntityFromID(_attackAction.targetedEntityID);

			//apply damage
			Dictionary<WeaponEquipmentData.DamageType, int> damages = new Dictionary<WeaponEquipmentData.DamageType, int>();
			for (int i = 0; i < _attackAction.damageTypes.Length; i++)
			{
				damages.Add((WeaponEquipmentData.DamageType)_attackAction.damageTypes[i], _attackAction.damages[i]);
			}

			m_user.Skin.OverrideAnimation(m_data.attackAnimationSuccessId);

			foreach (Entity entity in targetEntities)
			{
				for (int i = 0; i < _attackAction.Data.GetHitAmount(_attackAction, m_user, entity); i++)
				{
					entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damages });


					//aplly effects here
					/*for (int i = 0; i < _attackAction.areStatusesSuccess.Length; i++)
					{
						if (_attackAction.areStatusesSuccess[i])
							GameAssets.current.game.entityStatus[(EntityStatusEnumID)_attackAction.statusIds[i]].ApplyStatus(entity);
					}*/

					foreach (EntityPassiveEffectEnumID passiveEffectID in _attackAction.effects)
					{
						GameAssets.current.game.entityEffects[passiveEffectID].ApplyEffect(m_user, entity);
					}
				}
			}

			foreach (ParticleSystem ps in m_onPerformPS)
			{
				ps.Play();
			}

			DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
		}
		else
		{
			//show failure
			m_user.Skin.OverrideAnimation(m_data.attackAnimationFailureId);
			DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
		}
	}

	public virtual Dictionary<WeaponEquipmentData.DamageType, int> GetDamages ( Entity _user, Entity _target, AEntityAction _action, EntityActionData.PFCResultType _pfcResultType )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		bool didWinPFC = _pfcResultType == EntityActionData.PFCResultType.FirstWins;
		float flankMod = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(_user, _target, didWinPFC)];

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in Data.baseDamages)
		{
			if (!_action.Data.usedDamageChannels.Contains(pair.Key))
				continue;

			float damage = (((float)pair.Value * (_action.Data.damageFactor + _action.Data.GetDamageFactorAmountForType(_action, _user, _target, pair.Key)))
				*
				(
					1 + Mathf.Max((_user.Equipment.ApplyedDamageTypeBuffs.ContainsKey(pair.Key) ? _user.Equipment.ApplyedDamageTypeBuffs[pair.Key] : 0f)
					- (_target.Equipment.ApplyedDamageTypeResistance.ContainsKey(pair.Key) ? _target.Equipment.ApplyedDamageTypeResistance[pair.Key] : 0), -1f)
				)
				*
				(
					1 + Mathf.Max((_user.Equipment.ApplyedDamageCategoryBuffs.ContainsKey(GameConfig.current.game.damageCateforyPerDamageType[pair.Key]) ? _user.Equipment.ApplyedDamageCategoryBuffs[GameConfig.current.game.damageCateforyPerDamageType[pair.Key]] : 0)
					- (_target.Equipment.ApplyedDamageTypeCategoryResitance.ContainsKey(GameConfig.current.game.damageCateforyPerDamageType[pair.Key]) ? _target.Equipment.ApplyedDamageTypeCategoryResitance[GameConfig.current.game.damageCateforyPerDamageType[pair.Key]] : 0), -1f)
				)
				*
				(1 + Mathf.Max(_user.Equipment.GeneralDamageBuff
					- _user.Equipment.GeneralDamageResistance, -1f)
				)
				*
				(
					1 + Mathf.Max(flankMod
					+ _user.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FlankBonus)
					- _target.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FlankResistance), -1f)
				)
				*
				(
					1 + Mathf.Max(_user.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FinalDamageBonus), -1f)
				)
				);
			damages.Add(pair.Key, Mathf.RoundToInt(damage));
		}

		return damages;
	}
}
