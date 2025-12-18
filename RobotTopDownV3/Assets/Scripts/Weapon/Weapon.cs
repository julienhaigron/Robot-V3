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
		Entity targetEntity = GameManager.Instance.GetEntityFromID((int)_attackAction.targetedEntityID);
		if (_isSuccess)
		{
			//apply damage
			int damageAmount = m_data.damage;
			targetEntity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damage = damageAmount });
			performingEntity.Skin.OverrideAnimation(m_data.attackAnimationSuccessId);

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
}
