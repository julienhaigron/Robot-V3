using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

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

	public virtual void PerformAttack ( AttackAction _attackAction, bool _isSuccess, Action _onPerformEnd )
	{
		//TODO:
		//if success => show attack reaching target
		//else => show attack failing to reach target

		Entity performingEntity = GameManager.Instance.GetEntityFromID(_attackAction.performingEntityID);
		Entity targetEntity = GameManager.Instance.GetEntityFromID(_attackAction.targetedEntityID);
		if (_isSuccess)
		{
			//apply damage
			Dictionary<WeaponEquipmentData.DamageType, int> damages = new Dictionary<WeaponEquipmentData.DamageType, int>();
			for(int i = 0; i < _attackAction.damageTypes.Length; i++)
			{
				damages.Add((WeaponEquipmentData.DamageType)_attackAction.damageTypes[i], _attackAction.damages[i]);
			}
			targetEntity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damages });
			performingEntity.Skin.OverrideAnimation(m_data.attackAnimationSuccessId);

			//aplly effects here
			for (int i = 0; i < _attackAction.areEffectsSuccess.Length; i++)
			{
				if (_attackAction.areEffectsSuccess[i])
					GameAssets.current.game.entityEffects[(AEntityEffect.EntityEffectEnumID)_attackAction.effectsIds[i]].ApplyEffect(targetEntity);
			}

			//if is bullet weapon :
			//1) instantiate X bullet at weapon muzzle
			//2) _isSuccess ? send bullet towards target : (send bullet next to target || target to evade animation)

			foreach(ParticleSystem ps in m_onPerformPS)
			{
				ps.Play();
			}

			DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
		}
		else
		{
			//show failure
			performingEntity.Skin.OverrideAnimation(m_data.attackAnimationFailureId);
			DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
		}
	}

	public virtual Dictionary<WeaponEquipmentData.DamageType, int> GetDamages (Entity _user, Entity _target)
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		float flankBonus = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(_user, _target)];

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in Data.baseDamages)
		{
			float damage = ((float)pair.Value * (_user.Equipment.ApplyedDamageTypeBuffs.ContainsKey(pair.Key) ? _user.Equipment.ApplyedDamageTypeBuffs[pair.Key] : 1)
					* (_user.Equipment.ApplyedDamageCategoryBuffs.ContainsKey(Data.damageCategory) ? _user.Equipment.ApplyedDamageCategoryBuffs[Data.damageCategory] : 1))
				* _user.Equipment.GeneralDamageBuff * _user.Data.GetStaticDamageBonus() * flankBonus;
			damages.Add(pair.Key, (int)damage);
		}

		return damages;
	} 
}
