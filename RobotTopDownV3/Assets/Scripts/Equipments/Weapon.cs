using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Text;
using System.Linq;
using Sirenix.OdinInspector;

public class Weapon : MonoBehaviour
{

	[Title("Dependencies")]
	protected WeaponEquipmentData m_data;
	public WeaponEquipmentData Data => m_data;

	[SerializeField] protected List<ParticleSystem> m_onPerformPS;

	protected Entity m_user;
	protected string m_id;
	public string ID => m_id;
	protected Coroutine m_attackCR;
	protected AttackAction m_lastPerformedAction;
	protected Action m_onPerformAttackEnd;
	protected HashSet<Tile> m_targetedTiles = new();

	private WaitForSeconds m_singleAttackDuration;

	public class WeaponTarget
	{
		public Entity targetEntity;
		public Tile targetTile;
	}

	public virtual void Init ( Entity _user, WeaponEquipmentData _data, string _id, bool _isFirstSide )
	{
		m_user = _user;
		m_data = _data;
		m_id = _id;

		m_singleAttackDuration = new WaitForSeconds(_data.singleAttackAnimationDuration);
	}

	private void OnDestroy ()
	{
		if (m_attackCR != null && GameManager.Instance != null)
			GameManager.Instance.StopCoroutine(m_attackCR);
	}

	public virtual void PerformAttack ( AttackAction _attackAction, Action _onPerformEnd )
	{
		m_lastPerformedAction = _attackAction;
		m_onPerformAttackEnd = _onPerformEnd;

		if (m_attackCR != null)
			GameManager.Instance.StopCoroutine(m_attackCR);

		m_attackCR = GameManager.Instance.StartCoroutine(PerformAttackCR(_attackAction));
	}

	protected IEnumerator PerformAttackCR ( AttackAction _attackAction )
	{
		int lastSuccessfullAttackIndex = -1;
		for (int i = 0; i < _attackAction.attacksInfos.Length; i++)
		{
			if (_attackAction.attacksInfos[i].isAttackSuccessfull)
				lastSuccessfullAttackIndex = i;
		}
		OnStartAttacking(_attackAction);

		for (int attackIndex = 0; attackIndex < _attackAction.attacksInfos.Length; attackIndex++)
		{
			AttackAction.SingleAttackInfo attackInfo = _attackAction.attacksInfos[attackIndex];

			OnStartSingleAttack(_attackAction, attackIndex, attackInfo);

			yield return AimTargetCR(_attackAction, attackIndex, attackInfo);

			yield return PerformSingleAttackCR(_attackAction, attackIndex, attackInfo, lastSuccessfullAttackIndex);

			OnEndSingleAttack(_attackAction, attackIndex, attackInfo);
		}

		yield return OnEndAttackingCR(_attackAction);

		if (lastSuccessfullAttackIndex == -1)
			EndAttack(_attackAction);

		m_attackCR = null;
	}

	#region ATTACK FLOW

	protected virtual void OnStartAttacking ( AttackAction _attackAction )
	{

	}

	protected virtual void OnStartSingleAttack ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{

	}

	protected virtual IEnumerator AimTargetCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		yield return null;
	}

	protected virtual IEnumerator PerformSingleAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex )
	{
		yield return ApplyAttackCR(_attackAction, _attackIndex, _attackInfo);

		if (_attackIndex == _lastSuccessfullAttackIndex)
			EndAttack(m_lastPerformedAction);
	}

	protected virtual void OnEndSingleAttack ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{

	}

	protected virtual IEnumerator OnEndAttackingCR ( AttackAction _attackAction )
	{
		yield return null;
	}

	#endregion

	#region DAMAGE LOGIC

	protected virtual IEnumerator ApplyAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		if (_attackInfo.isAttackSuccessfull && !string.IsNullOrEmpty(m_data.attackAnimationSuccessId))
			m_user.Skin.OverrideAnimation(m_data.attackAnimationSuccessId);
		else if (!string.IsNullOrEmpty(m_data.attackAnimationFailureId))
			m_user.Skin.OverrideAnimation(m_data.attackAnimationFailureId);

		if (!_attackInfo.isAttackSuccessfull)
			yield break;

		List<WeaponTarget> targets = GetTargets(_attackAction, _attackIndex);
		Dictionary<WeaponEquipmentData.DamageType, int> damages = BuildDamageDictionary(_attackInfo);

		foreach (ParticleSystem ps in m_onPerformPS)
			ps.Play();
		SoundManager.Instance.Play(_attackAction.Data.onPerformSingleAttackSFXID);

		foreach (WeaponTarget target in targets)
		{
			Entity targetEntity = target.targetEntity != null ? target.targetEntity : target.targetTile.GetCurrentEntity();
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

		yield return m_singleAttackDuration;
	}

	protected List<WeaponTarget> GetTargets ( AttackAction _attackAction, int _attackIndex )
	{
		List<WeaponTarget> targets = new();
		EntityActionData attackData = GameAssets.current.game.entityActionsData[_attackAction.enumID];

		if (attackData.aoeType != EntityActionData.AOEType.Noone)
		{
			Tile aoeCenter = _attackAction.GetTargetTileAt(_attackIndex);
			if (aoeCenter == null)
				return targets;

			foreach (Tile tile in m_user.Equipment.GetTilesInAoERange(_attackAction, aoeCenter))
			{
				m_targetedTiles.Add(tile);
				tile.UI.SetOutlineColor(Color.red);
				if (tile.TryGetCurrentEntity(out Entity entity))
					targets.Add(new() { targetTile = tile, targetEntity = entity });
				else
					targets.Add(new() { targetTile = tile, targetEntity = null});
			}
		}
		else
		{
			Entity target = _attackAction.GetTargetEntityAt(_attackIndex);

			//No entity on the targeted tile: the attack still resolves on the tile the action recorded.
			Tile targetTile = target != null ? target.Displacement.Coordinates.GetTile() : _attackAction.GetTargetTileAt(_attackIndex);
			if (targetTile == null)
				return targets;

			targetTile.UI.SetOutlineColor(Color.red);
			m_targetedTiles.Add(targetTile);
			targets.Add(new() { targetTile = targetTile, targetEntity = target });
		}

		return targets;
	}

	protected virtual List<Entity> GetPassiveEffectTargets ( AttackAction _attackAction, AEntityPassiveEffect.PassiveEffectContainer _passiveEffect, int _attackIndex )
	{
		List<Entity> targets = new();

		if (_passiveEffect.aoeType == EntityActionData.AOEType.Noone)
		{
			Entity singleTarget = _attackAction.GetTargetEntityAt(_attackIndex);
			if (singleTarget != null)
				targets.Add(singleTarget);
		}
		else
		{
			Tile target = _attackAction.GetTargetTileAt(_attackIndex);
			if (target == null)
				return targets;

			Tile from = _passiveEffect.centerType == EntityActionData.AOECenterType.Self
				? m_user.Displacement.Coordinates.GetTile() : target;
			int minDistance = _passiveEffect.effectRange.x != -1 ? _passiveEffect.effectRange.x : _attackAction.Data.aoeMinEffectRange;
			int maxDistance = _passiveEffect.effectRange.y != -1 ? _passiveEffect.effectRange.y : _attackAction.Data.aoeMaxEffectRange;
			foreach (Tile tile in GridManager.Instance.GetTilesInAoERange(_passiveEffect.aoeType, m_user, from, target, minDistance, maxDistance, _attackAction.Data.maxChainedTarget, true))
			{
				if (tile.TryGetCurrentEntity(out Entity entity))
					targets.Add(entity);
			}
		}

		return targets;
	}

	protected virtual Dictionary<WeaponEquipmentData.DamageType, int> BuildDamageDictionary ( AttackAction.SingleAttackInfo _attackInfo )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();

		if (_attackInfo != null && _attackInfo.damageTypes != null)
		{
			for (int i = 0; i < _attackInfo.damageTypes.Length; i++)
				damages.Add((WeaponEquipmentData.DamageType)_attackInfo.damageTypes[i], _attackInfo.damages[i]);
		}

		return damages;
	}

	protected virtual void ApplyStatuses ( Entity _target, AttackAction.SingleAttackInfo _attackInfo )
	{
		if (_attackInfo != null && _attackInfo.areStatusesSuccess != null)
		{
			for (int i = 0; i < _attackInfo.areStatusesSuccess.Length; i++)
			{
				if (_attackInfo.areStatusesSuccess[i])
					_target.AddStatus((EntityStatusEnumID)_attackInfo.statusIds[i]);
			}
		}
	}

	protected virtual void ApplyEffects ( Entity _target, AEntityPassiveEffect.PassiveEffectContainer _passiveEffect )
	{
		if (m_lastPerformedAction == null || m_lastPerformedAction.effects == null)
			return;

		foreach (AEntityPassiveEffect.PassiveEffectContainer passiveEffectID in m_lastPerformedAction.effects)
			GameAssets.current.game.entityEffects[passiveEffectID.enumID].ApplyEffect(m_user, _target, passiveEffectID);

	}

	#endregion

	protected virtual void EndAttack ( AttackAction _attackAction )
	{
		m_onPerformAttackEnd?.Invoke();
		foreach (Tile tile in m_targetedTiles)
			tile.UI.ResetOutline();
		m_targetedTiles.Clear();
	}

	public virtual Dictionary<WeaponEquipmentData.DamageType, int> GetDamages ( Entity _user, Entity _target, AEntityAction _action, EntityActionData.PFCResultType _pfcResultType )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		bool didWinPFC = _pfcResultType == EntityActionData.PFCResultType.FirstWins;
		float flankMod = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(_user, _target, didWinPFC)];
		StringBuilder detailsBuilder = new();

		detailsBuilder.AppendLine($"<b>{_user.ID}</b> => <b>{_target.ID}</b>");
		detailsBuilder.AppendLine();

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _action.Data.baseDamages)
		{
			int baseDamage = pair.Value;

			float actionFactor = _action.Data.GetDamageFactorAmountForType(
					_action,
					_user,
					_target,
					pair.Key
				);

			EntityEquipmentData.SecondaryStat.StatType damageTypeStatType = GameConfig.current.game.statTypePerDamageType[pair.Key];
			EntityEquipmentData.SecondaryStat.StatType resistanceTypeStatType = GameConfig.current.game.statTypePerDamageResistanceType[pair.Key];

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
			EntityEquipmentData.SecondaryStat.StatType damageCategoryStatType = GameConfig.current.game.statTypePerDamageCategory[category];
			EntityEquipmentData.SecondaryStat.StatType resistanceCategoryStatType = GameConfig.current.game.statTypePerDamageCategory[category];

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
					_user.Equipment.GeneralDamageBuff + _user.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.GeneralDamageBonus, _action)
					- _target.Equipment.GeneralDamageResistance - _target.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.GeneralDamageResistance, null),
					-1f
				);

			float flankBonus =
				Mathf.Max(
					flankMod
					+ _user.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.FlankDamageBonus) + _user.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.FlankDamageBonus, _action)
					- _target.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.FlankResistance) - _target.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.FlankResistance, null),
					-1f
				);

			float finalBonus =
				Mathf.Max(
					_user.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.FinalDamageBonus) + _user.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.FinalDamageBonus, _action),
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
		LogConsole.AddLog(_target.ID + " takes damages from " + _user.ID, LogConsole.LogEventType.Damage, details);

		return damages;
	}

}
