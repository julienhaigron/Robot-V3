using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
public class EntityEquipmentPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityDeath;
	public System.Action<int> onDeath;
	public System.Action<TakeDamageCallback> onHealthChangeDamage;

	private Dictionary<string, Weapon> m_weapons = new();
	public Dictionary<string, Weapon> Weapons => m_weapons;

	private Dictionary<string, ToolEquipment> m_tools = new();
	public Dictionary<string, ToolEquipment> Tools => m_tools;

	private Dictionary<string, AItemLinkedData> m_itemsLinkedDataDictionary = new();
	public Dictionary<string, AItemLinkedData> ItemsLinkedDataDictionary => m_itemsLinkedDataDictionary;

	private int m_currentHealth;
	public int CurrentHealth => m_currentHealth;

	private int m_maxHealth;
	public int MaxHealth => m_maxHealth;

	private bool m_isDead = false;
	public bool IsDead => m_isDead;

	private bool m_didAttackThisTurn = false;
	public bool DidAttackThisTurn => m_didAttackThisTurn;

	private void Awake ()
	{
		m_linkedEntity.onStartPerformAction += OnStartPerformAction;
		TurnManager.onStartInputPhase += OnNewTurnBegin;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.onStartPerformAction -= OnStartPerformAction;
		TurnManager.onStartInputPhase -= OnNewTurnBegin;
	}

	public override void Init ( EntitySavedData _entityData )
	{
		if (_entityData.arms != null && _entityData.arms.Length > 0)
		{
			for (int armIndex = 0; armIndex < _entityData.arms.Length; armIndex++)
			{
				GameDatas.PlayerSave.Component stringContainer = _entityData.arms[armIndex];
				if (stringContainer == null || !stringContainer.TryGetData(out EntityEquipmentData data))
					continue;

				Transform socket = m_linkedEntity.Skin.IK.GetGrabSocket(armIndex);
				if (data is WeaponEquipmentData weaponData)
					AddWeapon(weaponData, stringContainer.ID, socket);
				else if (data is ToolEquipmentData toolData)
					AddTool(toolData, stringContainer.ID, socket);
			}
		}

		m_maxHealth = m_linkedEntity.Data.GetMaxHealth();
		m_currentHealth = _entityData.currentHp <= 0 ? m_maxHealth : Mathf.Min(_entityData.currentHp, m_maxHealth);
		m_isDead = false;

		base.Init(_entityData);
	}

	#region Callbacks

	private void OnStartPerformAction ( AEntityAction _actionPerformed )
	{
		if (_actionPerformed.Data.type == EntityActionData.ActionType.MeleeAttack || _actionPerformed.Data.type == EntityActionData.ActionType.DistanceAttack)
			m_didAttackThisTurn = true;
	}

	private void OnNewTurnBegin ()
	{
		m_didAttackThisTurn = false;
	}

	#endregion

	#region Weapon

	public struct TakeDamageCallback
	{
		public Entity entityAttacker;
		public Entity entityTargeted;
		public Dictionary<WeaponEquipmentData.DamageType, int> damages;
		public Tile shotOrigin;
		public Tile hitTile;
		public int absorbedDamage;
		public bool critical;
		public Vector3 hitPos;
		public Vector3 hitNormal;
	}

	private Weapon AddWeapon ( WeaponEquipmentData _data, string _id, Transform _socket )
	{
		Weapon newWeapon = Instantiate(_data.prefab, _socket);
		if (m_linkedEntity.Skin.IK != null)
			m_linkedEntity.Skin.IK.FitInHand(newWeapon.transform);
		newWeapon.Init(m_linkedEntity, _data, _id);
		newWeapon.transform.localScale = Vector3.one;
		m_weapons.Add(newWeapon.ID, newWeapon);

		return newWeapon;
	}


	public List<Tile> GetTilesInWeaponRange ( AEntityAction _action, bool _isThisTurn = false, bool _ignoreVisibility = false )
	{
		Tile from = _isThisTurn ? m_linkedEntity.Displacement.Coordinates.GetTile() : GridManager.Instance.Tiles[_action.supposedPositionAtActionStartID];
		int orientation = m_linkedEntity.Displacement.CurrentOrientation;
		return GetTilesInWeaponRange(_action, _isThisTurn, from, orientation, _ignoreVisibility);
	}

	public List<Tile> GetTilesInWeaponRange( AEntityAction _action, bool _isThisTurn, Tile _from, int _orientation, bool _ignoreVisibility = false )
	{
		List<Tile> tilesInRange = new();
		int maxDistance = _action.Data.GetMaxRange(_action, m_linkedEntity, null);
		int minDistance = _action.Data.minDistance;

		if (_ignoreVisibility && CanCurveShot(_action))
		{
			foreach (Tile tile in GridManager.Instance.GetTilesInCurvedShotReach(_from, minDistance, maxDistance))
			{
				if (GridManager.Instance.IsInVisionCone(_from, tile, (HexDirection)_orientation))
					tilesInRange.Add(tile);
			}

			return tilesInRange;
		}

		switch (_action.Data.aoeType)
		{
			case EntityActionData.AOEType.Noone:
			case EntityActionData.AOEType.LargeArc:
			case EntityActionData.AOEType.ThinArc:
			case EntityActionData.AOEType.Chain:
			case EntityActionData.AOEType.Ray:
				tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, false, _isThisTurn, _ignoreVisibility));
				break;
			case EntityActionData.AOEType.LargeCone:
			case EntityActionData.AOEType.ThinCone:
				if (_action.Data.aoECenterType == EntityActionData.AOECenterType.Self)
					tilesInRange.AddRange(GridManager.Instance.GetTilesInCone(_from, minDistance, maxDistance, _orientation, _action.Data.aoeType, _isThisTurn));
				else
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, false, _isThisTurn, _ignoreVisibility));
				break;
			case EntityActionData.AOEType.Circle:
				if (_action.Data.aoECenterType == EntityActionData.AOECenterType.Self)
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionRange(_from, minDistance, maxDistance, false, _isThisTurn, _ignoreVisibility));
				else
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, false, _isThisTurn, _ignoreVisibility));
				break;
		}


		return tilesInRange;
	}

	public bool IsTileInWeaponRange ( AEntityAction _action, bool _isThisTurn, Tile _from, int _orientation, Tile _tile, bool _ignoreVisibility = false )
	{
		if (_from == null || _tile == null)
			return false;

		if (!_ignoreVisibility || !CanCurveShot(_action))
			return GetTilesInWeaponRange(_action, _isThisTurn, _from, _orientation, _ignoreVisibility).Contains(_tile);

		return GridManager.Instance.IsInVisionCone(_from, _tile, (HexDirection)_orientation)
			&& GridManager.Instance.IsInCurvedShotReach(_from, _tile, _action.Data.minDistance, _action.Data.GetMaxRange(_action, m_linkedEntity, null));
	}

	public bool CanCurveShot ( AEntityAction _action )
	{
		if (_action == null || _action.effects == null || !_action.Data.FiresProjectileAtTarget())
			return false;

		foreach (AEntityPassiveEffect.PassiveEffectContainer passiveContainer in _action.effects)
		{
			if (passiveContainer.enumID == EntityPassiveEffectEnumID.TrajectoryControl
				&& (passiveContainer.conditionType == Condition.ConditionType.Noone || Condition.UseConditionPredicate(_action, m_linkedEntity, null, passiveContainer.conditionType)))
				return true;
		}

		return false;
	}

	public bool TryCurveShot ( AEntityAction _action, Tile _from, Tile _to, out Tile _curveTile )
	{
		_curveTile = null;

		return CanCurveShot(_action)
			&& GridManager.Instance.TryGetCurvedShotTile(_from, _to, _action.Data.GetMaxRange(_action, m_linkedEntity, null), out _curveTile);
	}

	public Tile GetShotOrigin ( AEntityAction _action, Tile _from, Tile _to )
	{
		Tile strikeTile = _action is AttackAction attackAction ? attackAction.GetPlannedStrikeTile(_from, _to) : _from;
		return GridManager.Instance.IsThereBlockingWallBetween(strikeTile, _to, true, out _) && TryCurveShot(_action, strikeTile, _to, out Tile curveTile) ? curveTile : strikeTile;
	}

	public HashSet<Tile> GetWeaponConeTilesAt ( Tile _from, int _orientation )
	{
		HashSet<Tile> coneTiles = new();
		if (_from == null || _orientation < 0 || m_linkedEntity.ComponentLinkedToAction == null)
			return coneTiles;

		foreach (KeyValuePair<EntityActionEnumID, List<string>> pair in m_linkedEntity.ComponentLinkedToAction)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData.TryGetValue(pair.Key, out EntityActionData actionData) ? actionData : null;
			if (data == null || pair.Value == null || pair.Value.Count == 0
				|| (data.type != EntityActionData.ActionType.DistanceAttack && data.type != EntityActionData.ActionType.MeleeAttack))
				continue;

			AEntityAction action = TurnManager.Instance.GetAction(data, m_linkedEntity.ID, pair.Value[0], TurnManager.currentTick);
			if (action != null)
				coneTiles.UnionWith(GetTilesInWeaponRange(action, false, _from, _orientation, true));
		}

		return coneTiles;
	}

	public HashSet<Tile> GetTilesInReach ( AEntityAction _action, Tile _from, bool _isThisTurn = true, bool _ignoreVisibility = false )
	{
		HashSet<Tile> tilesInReach = new();
		if (_action == null || _from == null)
			return tilesInReach;

		if (_action.Data.GetMainActionType() != EntityActionData.MainActionType.Attack)
		{
			foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(_from, _action.Data.minDistance
				, _action.Data.GetMaxRange(_action, m_linkedEntity, null), false, _isThisTurn, _ignoreVisibility))
				tilesInReach.Add(tile);

			return tilesInReach;
		}

		for (int orientation = 0; orientation < 6; orientation++)
		{
			foreach (Tile tile in GetTilesInWeaponRange(_action, _isThisTurn, _from, orientation, _ignoreVisibility))
				tilesInReach.Add(tile);
		}

		return tilesInReach;
	}

	public List<Tile> GetTilesInAoERange ( AEntityAction _action, Tile _targetTile, bool _isThisTurn = false, bool _usePlannedPose = false, System.Func<Tile, Entity> _entityOnTile = null )
	{
		bool isCenteredOnCaster = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self;
		int maxDistance = isCenteredOnCaster ? _action.Data.GetMaxRange(_action, m_linkedEntity, null) : _action.Data.GetAoEMaxRange(_action, m_linkedEntity, null);
		int minDistance = isCenteredOnCaster ? _action.Data.minDistance : _action.Data.aoeMinEffectRange;
		Tile casterTile = _usePlannedPose ? GridManager.Instance.Tiles[_action.supposedPositionAtActionStartID]
			: _action.PerformingEntity.Displacement.Coordinates.GetTile();
		Tile from = !isCenteredOnCaster ? _targetTile : casterTile;
		int orientation = _usePlannedPose && _targetTile != null && casterTile != null && _targetTile != casterTile
			? GridManager.Instance.GetClosestOrientation(casterTile, _targetTile)
			: m_linkedEntity.Displacement.CurrentOrientation;
		int extraValue = _action.Data.maxChainedTarget;
		return GridManager.Instance.GetTilesInAoERange(_action.Data.aoeType, m_linkedEntity, from, _targetTile, minDistance, maxDistance, extraValue, _isThisTurn, orientation, _entityOnTile);
	}

	public bool AttackRoll ( AttackAction _attackAction, AttackAction.SingleAttackInfo _singleAttackInfo, Entity _targetEntity, LogCard _entry, out Tile _coverTile )
	{
		bool doesWinPFC = _attackAction.DoesWinExchangeAgainst(_targetEntity);
		Tile strikeTile = _singleAttackInfo.StrikeTile;
		Tile targetTile = _attackAction.GetExchangeTileOf(_targetEntity);
		Tile curveTile = null;

		if (GridManager.Instance.IsThereBlockingWallBetween(strikeTile, targetTile, true, out _coverTile)
			&& !TryCurveShot(_attackAction, strikeTile, targetTile, out curveTile))
		{
			_entry.steps.Add(LogCard.Note(LocalizationKey.log_card_wall, _targetEntity.Data.name));
			return false;
		}

		_singleAttackInfo.curveTileID = curveTile == null ? -1 : curveTile.coordinates.ID;

		bool isThereCoverBetween = GridManager.Instance.IsThereCoverBeween(strikeTile, targetTile, out _coverTile, curveTile);
		string coverName = isThereCoverBetween ? _coverTile.GroundType.GetLocalizedCoverName() : null;

		NeuronalMembraneEquipmentData.VisionTypes seenWith = GridManager.Instance.GetBestVisionOf(m_linkedEntity.OwnerID, targetTile);
		float targetCamo = _targetEntity.Data.GetOccultorCamo(seenWith)
			+ _targetEntity.GetStatBonus(NeuronalMembraneEquipmentData.GetCamoStat(seenWith));
		EntityEquipmentData.SecondaryStat.StatType perceptionStat = NeuronalMembraneEquipmentData.GetPerceptionStat(seenWith);
		float perception = Mathf.Clamp(m_linkedEntity.GetStatBonus(perceptionStat, _attackAction), 0f, Mathf.Max(targetCamo, 0f));

		float evationRatio = _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack
				? _targetEntity.Data.BrainData.distanceEvasion + _targetEntity.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.DistanceEvasion)
				: _targetEntity.Data.BrainData.meleeEvasion + _targetEntity.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.MeleeEvasion);
		float coverRatio = isThereCoverBetween ? GameConfig.current.game.entityCoverBonus : 0f;
		float targetEvasionScore = targetCamo + evationRatio + coverRatio;

		float userAim = GetAimScore(_attackAction);
		Tile.TileDirectionType side = GridManager.Instance.GetHitTileSide(m_linkedEntity, _targetEntity, doesWinPFC, _singleAttackInfo.ShotOrigin);
		float flankBonus = GameConfig.current.game.entityFlankRatio[side]
			+ (side == Tile.TileDirectionType.Front ? 0f : m_linkedEntity.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.FlankBonus, _attackAction));
		float modAction = m_linkedEntity.PreviousActionAttackModificator;

		float userHitScore = userAim + flankBonus + modAction + perception;

		float finalScore = userHitScore - targetEvasionScore;

		float roll = Random.Range(0f, 1f);
		bool isAttackSuccessful = finalScore >= 1f || finalScore >= roll;

		if (isAttackSuccessful || !isThereCoverBetween || roll < (1-GameConfig.current.game.entityCoverBonus))
			_coverTile = null;

		LocalizationManager localization = LocalizationManager.Instance;
		LogCard card = LogCard.Roll(LogCard.Kind.HitRoll, LocalizationKey.log_card_hit_roll, m_linkedEntity.Data.name, _targetEntity.Data.name, finalScore, roll, isAttackSuccessful
			, LocalizationKey.log_card_hit, LocalizationKey.log_card_miss);
		card.equation = string.Format(localization.Get(LocalizationKey.log_card_hit_equation), userHitScore.ToPercentText(), targetEvasionScore.ToPercentText(), Mathf.Clamp01(finalScore).ToPercentText());

		LogCard.Column attack = card.AddColumn(LogCard.ColumnKind.Attack, LocalizationKey.log_card_attack, userHitScore);
		attack.Add(LocalizationKey.log_card_accuracy, userAim);
		attack.Add(string.Format(localization.Get(LocalizationKey.log_card_flank), side.GetLocalizedTitle()), flankBonus);
		attack.Add(LocalizationKey.log_card_previous_action, modAction);
		if (perception > 0f)
			attack.Add(perceptionStat.GetLocalizedTitle(), perception);

		LogCard.Column defense = card.AddColumn(LogCard.ColumnKind.Defense, LocalizationKey.log_card_defense, targetEvasionScore);
		defense.Add(string.Format(localization.Get(LocalizationKey.log_card_stealth), seenWith.GetLocalizedTitle().ToLower()), -targetCamo);
		defense.Add(LocalizationKey.log_card_evasion, -evationRatio);
		defense.Add(string.IsNullOrEmpty(coverName) ? localization.Get(LocalizationKey.log_card_cover) : string.Format(localization.Get(LocalizationKey.log_card_cover_named), coverName), -coverRatio);

		_entry.steps.Add(card);
		return isAttackSuccessful;
	}

	private float GetAimScore ( AttackAction _attackAction )
	{
		return _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack
			? m_linkedEntity.Data.BrainData.distanceAccuracy + m_linkedEntity.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.DistanceAccuracy, _attackAction)
			: m_linkedEntity.Data.BrainData.agility + m_linkedEntity.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.MeleeAccuracy, _attackAction);
	}

	public bool StructureAttackRoll ( AttackAction _attackAction, AttackAction.SingleAttackInfo _singleAttackInfo, Tile _pointTile, LogCard _entry, out Tile _coverTile )
	{
		bool isSabotagePoint = SabotagePoints.TryGetLivingPoint(_pointTile, out _);
		string pointName = AttackAction.GetStructureName(_pointTile) ?? "?";
		Tile strikeTile = _singleAttackInfo.StrikeTile;

		if (GridManager.Instance.IsThereBlockingWallBetween(strikeTile, _pointTile, true, out _coverTile))
		{
			if (!TryCurveShot(_attackAction, strikeTile, _pointTile, out Tile curveTile))
			{
				_entry.steps.Add(LogCard.Note(LocalizationKey.log_card_wall, pointName));
				return false;
			}

			_singleAttackInfo.curveTileID = curveTile.coordinates.ID;
			_coverTile = null;
		}

		float userAim = GetAimScore(_attackAction);
		float modAction = m_linkedEntity.PreviousActionAttackModificator;
		float demolisherBonus = isSabotagePoint && m_linkedEntity.MatchRole == Entity.MatchRoleType.Demolisher ? GameConfig.current.game.demolisherSabotageHitBonus : 0f;
		float finalScore = userAim + modAction + demolisherBonus;

		float roll = Random.Range(0f, 1f);
		bool isAttackSuccessful = finalScore >= 1f || finalScore >= roll;

		LogCard card = LogCard.Roll(LogCard.Kind.HitRoll, LocalizationKey.log_card_hit_roll, m_linkedEntity.Data.name, pointName, finalScore, roll, isAttackSuccessful
			, LocalizationKey.log_card_hit, LocalizationKey.log_card_miss);
		LogCard.Column attack = card.AddColumn(LogCard.ColumnKind.Attack, LocalizationKey.log_card_attack, finalScore);
		attack.Add(LocalizationKey.log_card_accuracy, userAim);
		attack.Add(LocalizationKey.log_card_previous_action, modAction);
		attack.Add(LocalizationKey.log_card_demolisher, demolisherBonus);

		_entry.steps.Add(card);
		return isAttackSuccessful;
	}

	public bool StatusRoll ( Entity _target, AEntityStatus _effect, AEntityAction _action, LogCard _entry )
	{
		float actionProbability = _action.Data.statusHitProbability;
		float userStatusChance = m_linkedEntity.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusChance, _action);
		float targetResistance = _target.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusResistance);
		float hitProba = actionProbability + userStatusChance - targetResistance;
		float roll = Random.Range(0f, 1f);
		bool isAttackSuccessful = hitProba >= 1f || roll <= hitProba;

		LogCard card = LogCard.Roll(LogCard.Kind.StatusRoll, LocalizationKey.log_card_status_roll, m_linkedEntity.Data.name, _target.Data.name, hitProba, roll, isAttackSuccessful
			, LocalizationKey.log_card_applied, LocalizationKey.log_card_resisted);
		card.status = _effect;
		card.description = _effect.GetLocalizedDescription();

		LogCard.Column chance = card.AddColumn(LogCard.ColumnKind.Neutral, LocalizationKey.log_card_status_chance, Mathf.Clamp01(hitProba));
		chance.Add(LocalizationKey.log_card_base_chance, actionProbability);
		chance.Add(LocalizationKey.log_card_chance_bonus, userStatusChance);
		chance.Add(LocalizationKey.log_card_target_resistance, -targetResistance);

		_entry.steps.Add(card);
		return isAttackSuccessful;
	}

	#endregion

	#region Tool

	private ToolEquipment AddTool ( ToolEquipmentData _data, string _id, Transform _socket )
	{
		ToolEquipment newTool = Instantiate(_data.prefab, _socket);
		if (m_linkedEntity.Skin.IK != null)
			m_linkedEntity.Skin.IK.FitInHand(newTool.transform);
		newTool.Init(m_linkedEntity, _data, _id);
		newTool.transform.localScale = Vector3.one;
		m_tools.Add(newTool.ID, newTool);

		return newTool;
	}

	#endregion

	#region Heatlh

	public void TakeDamage ( TakeDamageCallback _damageInfo )
	{
		if (m_linkedEntity.Data.FrameData.isImmortal)
		{
			onHealthChangeDamage?.Invoke(_damageInfo);
			return;
		}
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new(_damageInfo.damages);
		int incomingSide = GetIncomingSide(_damageInfo);
		foreach (ToolEquipment tool in m_tools.Values)
		{
			if (tool is not Shield shield || !shield.Covers(incomingSide))
				continue;

			foreach (WeaponEquipmentData.DamageType damageType in _damageInfo.damages.Keys)
			{
				int absorbed = shield.Absorb(damages[damageType]);
				damages[damageType] -= absorbed;
				_damageInfo.absorbedDamage += absorbed;
			}
		}

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in damages)
		{
			m_currentHealth -= pair.Value;
		}

		_damageInfo.damages = damages;
		if (m_currentHealth <= 0)
			Death(_damageInfo);

		onHealthChangeDamage?.Invoke(_damageInfo);
	}

	public Dictionary<WeaponEquipmentData.DamageType, int> GetDamagesAfterResistances ( Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> resisted = new();
		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _damages)
			resisted.Add(pair.Key, Mathf.RoundToInt(pair.Value * new DamageModifiers(null, m_linkedEntity, null, pair.Key, Tile.TileDirectionType.Front).Multiplier));

		return resisted;
	}

	private int GetIncomingSide ( TakeDamageCallback _damageInfo )
	{
		if (_damageInfo.entityAttacker == null)
			return -1;

		Tile shotOrigin = _damageInfo.shotOrigin != null ? _damageInfo.shotOrigin : _damageInfo.entityAttacker.Displacement.Coordinates.GetTile();
		Tile hitTile = _damageInfo.hitTile != null ? _damageInfo.hitTile : m_linkedEntity.Displacement.Coordinates.GetTile();
		return GridManager.Instance.GetIncomingSide(shotOrigin, hitTile);
	}

	public enum DeathCause { Default, Doom }

	public void InstantDeath ( DeathCause _cause = DeathCause.Default )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		damages.Add(WeaponEquipmentData.DamageType.Bludgeoning, 999999);
		m_currentHealth = 0;
		TakeDamageCallback deathInfo = new TakeDamageCallback()
		{
			critical = false,
			damages = damages,
			entityAttacker = m_linkedEntity,
			entityTargeted = m_linkedEntity,
			hitPos = Vector3.zero,
			hitNormal = Vector3.zero
		};

		onHealthChangeDamage?.Invoke(deathInfo);
		Death(deathInfo, _cause);
	}

	private LogCard BuildDeathCard ( TakeDamageCallback _damageInfo )
	{
		LocalizationManager localization = LocalizationManager.Instance;
		LogCard card = new(LogCard.Kind.Death, localization.Get(LocalizationKey.log_card_destroyed))
		{
			actor = m_linkedEntity.Data.name,
			verdict = string.Format(localization.Get(LocalizationKey.log_card_tick), TurnManager.currentTick)
		};

		Entity attacker = _damageInfo.entityAttacker;
		if (attacker != null && attacker != m_linkedEntity)
			card.AddFact(LocalizationKey.log_card_by, attacker.LastPerformedAction == null ? attacker.Data.name
				: attacker.Data.name + " · " + attacker.LastPerformedAction.Data.GetLocalizedName());

		if (_damageInfo.damages != null && _damageInfo.damages.Count > 0)
		{
			int total = LogCard.Sum(_damageInfo.damages, out WeaponEquipmentData.DamageType? main);
			card.AddFact(LocalizationKey.log_card_killing_blow, total + " " + main.Value.GetLocalizedTitle().ToLower()
				+ (_damageInfo.critical ? localization.Get(LocalizationKey.log_card_critical) : ""));
		}

		card.SetHealth(Mathf.Max(0, m_currentHealth), m_maxHealth);
		return card;
	}


	private void Death ( TakeDamageCallback _damageInfo, DeathCause _cause = DeathCause.Default )
	{
		if (m_isDead)
			return;

		PlayDeathVFX(_cause);

		if (m_linkedEntity.LastPerformedAction != null && m_linkedEntity.LastPerformedAction.IsPerforming)
			m_linkedEntity.LastPerformedAction.CancelAction();
		
		//Every tile, not just the one under it: CancelAction above may also have re-registered it, and a booked
		//destination lives on a tile the entity never reached.
		GridManager.Instance.ClearEntityFromAllTiles(m_linkedEntity);

		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_death), m_linkedEntity.Data.name), LogConsole.LogEventType.Death
			, new LogConsole.LogDetails(BuildDeathCard(_damageInfo)));

		m_isDead = true;
		onDeath?.Invoke(m_linkedEntity.ID);
		onAnyEntityDeath?.Invoke(m_linkedEntity);
	}

	private void PlayDeathVFX ( DeathCause _cause )
	{
		PoolData deathVFXPool = _cause == DeathCause.Doom
			? GameAssets.current.game.doomDeathVFXPoolData
			: GameAssets.current.game.deathVFXPoolData;

		if (deathVFXPool == null)
			return;

		Transform center = m_linkedEntity.Skin == null ? null : m_linkedEntity.Skin.Center;
		deathVFXPool.Get(center == null ? m_linkedEntity.transform.position : center.position, Quaternion.identity);
	}

	#endregion
}
