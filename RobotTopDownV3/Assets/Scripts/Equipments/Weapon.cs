using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Text;
using System.Linq;

public class Weapon : MonoBehaviour
{

	protected WeaponEquipmentData m_data;
	public WeaponEquipmentData Data => m_data;

	[SerializeField] protected List<ParticleSystem> m_onPerformPS;

	protected Entity m_user;
	private string m_id;
	public string ID => m_id;

	public virtual void Init ( Entity _user, WeaponEquipmentData _data, bool _isFirstSide )
	{
		m_user = _user;
		m_data = _data;
		m_id = _data.name + _user.Equipment.Tools.Values.Count;
	}

	public virtual void PerformAttack ( AttackAction _attackAction, Action _onPerformEnd )
	{
		for (int attackCount = 0; attackCount < _attackAction.attacksInfos.Length; attackCount++)
		{
			AttackAction.SingleAttackInfo attackInfo = _attackAction.attacksInfos[attackCount];
			if (attackInfo.isAttackSuccessfull)
			{
				List<Entity> targetEntities = new();
				EntityActionData attackData = GameAssets.current.game.entityActionsData[_attackAction.enumID];

				if (attackData.isAoe)
				{
					foreach (Tile tile in m_user.Equipment.GetTilesInAoERange(_attackAction, GridManager.Instance.Tiles[_attackAction.targetTileIDs[attackCount]]))
					{
						Entity entityOnTIle = tile.GetEntity(true);
						if (entityOnTIle != null /*&& !entityOnTIle.IsAlliedTo(m_user.OwnerID)*/)
							targetEntities.Add(entityOnTIle);
					}
				}
				else
					targetEntities.Add(GameManager.Instance.GetEntityFromID(_attackAction.targetedEntityIDs[attackCount]));

				//apply damage
				Dictionary<WeaponEquipmentData.DamageType, int> damages = new Dictionary<WeaponEquipmentData.DamageType, int>();
				for (int i = 0; i < attackInfo.damageTypes.Length; i++)
				{
					damages.Add((WeaponEquipmentData.DamageType)attackInfo.damageTypes[i], attackInfo.damages[i]);
				}

				m_user.Skin.OverrideAnimation(m_data.attackAnimationSuccessId);

				foreach (Entity entity in targetEntities)
				{
					for (int i = 0; i < _attackAction.Data.GetHitAmount(_attackAction, m_user, entity); i++)
					{
						entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damages });

						//aplly effects here
						for (int j = 0; j < attackInfo.areStatusesSuccess.Length; j++)
						{
							if (attackInfo.areStatusesSuccess[j])
								entity.AddStatus((EntityStatusEnumID)attackInfo.statusIds[j]);
						}

						foreach (AEntityPassiveEffect.PassiveEffectContainer passiveEffectID in _attackAction.effects)
						{
							GameAssets.current.game.entityEffects[passiveEffectID.enumID].ApplyEffect(m_user, entity, passiveEffectID);
						}
					}
				}

				foreach (ParticleSystem ps in m_onPerformPS)
				{
					ps.Play();
				}
			}
			else
			{
				//show failure
				m_user.Skin.OverrideAnimation(m_data.attackAnimationFailureId);
			}
		}

		DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
	}

	public virtual Dictionary<WeaponEquipmentData.DamageType, int> GetDamages ( Entity _user, Entity _target, AEntityAction _action, EntityActionData.PFCResultType _pfcResultType )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		bool didWinPFC = _pfcResultType == EntityActionData.PFCResultType.FirstWins;
		float flankMod = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(_user, _target, didWinPFC)];
		StringBuilder detailsBuilder = new();

		detailsBuilder.AppendLine($"<b>{_user.ID}</b> => <b>{_target.ID}</b>");
		detailsBuilder.AppendLine();

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in Data.baseDamages)
		{
			if (!_action.Data.usedDamageChannels.Contains(pair.Key))
				continue;

			int baseDamage = pair.Value;

			float actionFactor =
				_action.Data.damageFactor +
				_action.Data.GetDamageFactorAmountForType(
					_action,
					_user,
					_target,
					pair.Key
				);

			EntityEquipmentData.StatBonus.StatType damageTypeStatType = GameConfig.current.game.statTypePerDamageType[pair.Key];
			EntityEquipmentData.StatBonus.StatType resistanceTypeStatType = GameConfig.current.game.statTypePerDamageResistanceType[pair.Key];

			float typeBuff = _user.GetAdditionaryStatBonus(damageTypeStatType, _action)
				 + (_user.Equipment.ApplyedDamageTypeBuffs.ContainsKey(pair.Key)
					? _user.Equipment.ApplyedDamageTypeBuffs[pair.Key]
					: 0f)
				- _target.GetAdditionaryStatBonus(resistanceTypeStatType, null)
				- (_target.Equipment.ApplyedDamageTypeResistance.ContainsKey(pair.Key)
					? _target.Equipment.ApplyedDamageTypeResistance[pair.Key]
					: 0f);

			typeBuff = Mathf.Max(typeBuff, -1f);

			var category = GameConfig.current.game.damageCategoryPerDamageType[pair.Key];
			EntityEquipmentData.StatBonus.StatType damageCategoryStatType = GameConfig.current.game.statTypePerDamageCategory[category];
			EntityEquipmentData.StatBonus.StatType resistanceCategoryStatType = GameConfig.current.game.statTypePerDamageCategory[category];

			float categoryBuff = _user.GetAdditionaryStatBonus(damageCategoryStatType, _action)
				+ (_user.Equipment.ApplyedDamageCategoryBuffs.ContainsKey(category)
					? _user.Equipment.ApplyedDamageCategoryBuffs[category]
					: 0f)
				- _target.GetAdditionaryStatBonus(resistanceCategoryStatType, null)
				- (_target.Equipment.ApplyedDamageTypeCategoryResitance.ContainsKey(category)
					? _target.Equipment.ApplyedDamageTypeCategoryResitance[category]
					: 0f);

			categoryBuff = Mathf.Max(categoryBuff, -1f);

			float generalDamage =
				Mathf.Max(
					_user.Equipment.GeneralDamageBuff + _user.GetAdditionaryStatBonus(EntityEquipmentData.StatBonus.StatType.GeneralDamageBonus, _action)
					- _target.Equipment.GeneralDamageResistance - _target.GetAdditionaryStatBonus(EntityEquipmentData.StatBonus.StatType.GeneralDamageResistance, null),
					-1f
				);

			float flankBonus =
				Mathf.Max(
					flankMod
					+ _user.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FlankBonus) + _user.GetAdditionaryStatBonus(EntityEquipmentData.StatBonus.StatType.FlankBonus, _action)
					- _target.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FlankResistance) - _target.GetAdditionaryStatBonus(EntityEquipmentData.StatBonus.StatType.FlankResistance, null),
					-1f
				);

			float finalBonus =
				Mathf.Max(
					_user.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FinalDamageBonus) + _user.GetAdditionaryStatBonus(EntityEquipmentData.StatBonus.StatType.FinalDamageBonus, _action),
					-1f
				);

			float damage =
				baseDamage
				* actionFactor
				* (1 + typeBuff)
				* (1 + categoryBuff)
				* (1 + generalDamage)
				* (1 + flankBonus)
				* (1 + finalBonus);

			int finalDamage = Mathf.RoundToInt(damage);

			damages.Add(pair.Key, finalDamage);

			detailsBuilder.AppendLine($"<b>{pair.Key}</b>");
			detailsBuilder.AppendLine($"Base Damage: {baseDamage}");
			detailsBuilder.AppendLine($"Action Factor: x{actionFactor}");
			detailsBuilder.AppendLine($"Type Modifier: {(typeBuff >= 0 ? "+" : "")}{typeBuff}%");
			detailsBuilder.AppendLine($"Category Modifier: {(categoryBuff >= 0 ? "+" : "")}{categoryBuff}%");
			detailsBuilder.AppendLine($"General Modifier: {(generalDamage >= 0 ? "+" : "")}{generalDamage}%");
			detailsBuilder.AppendLine($"Flank Modifier: {(flankBonus >= 0 ? "+" : "")}{flankBonus}%");
			detailsBuilder.AppendLine($"Final Modifier: {(finalBonus >= 0 ? "+" : "")}{finalBonus}%");
			detailsBuilder.AppendLine($"<color=red><b>Final Damage: {finalDamage}</b></color>");
			detailsBuilder.AppendLine();
		}

		string detailsDescription = detailsBuilder.ToString();
		LogConsole.LogDetails details = new("damage_" + LogConsole.Instance.LogsDetails.Keys.Count, "Damage Details", detailsDescription);
		LogConsole.AddLog(_target.ID + " takes damages from " + _user.ID, LogConsole.LogEventType.AttackResolution, details);


		return damages;
	}
}
