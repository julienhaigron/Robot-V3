using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using System.Text;
public class EntityEquipmentPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityDeath;
	public System.Action<int> onDeath;
	public System.Action<TakeDamageCallback> onHealthChangeDamage;

	private Dictionary<string, Weapon> m_weapons = new();
	public Dictionary<string, Weapon> Weapons => m_weapons;
	[SerializeField] private Transform m_weaponConesParent;

	private Dictionary<string, WeaponCone> m_weaponConeDictionary = new();

	private Dictionary<string, Tool> m_tools = new();
	public Dictionary<string, Tool> Tools => m_tools;

	private Dictionary<string, AItemLinkedData> m_itemsLinkedDataDictionary = new();
	public Dictionary<string, AItemLinkedData> ItemsLinkedDataDictionary => m_itemsLinkedDataDictionary;

	private int m_currentHealth;
	public int CurrentHealth => m_currentHealth;

	private int m_maxHealth;
	public int MaxHealth => m_maxHealth;

	private bool m_isDead = false;
	public bool IsDead => m_isDead;

	/*private SerializableDictionary<string, int> m_equipmentInCooldown = new();
	public SerializableDictionary<string, int> EquipmentInCooldown => m_equipmentInCooldown;*/

	[Title("Stats")]
	private float m_generalDamageBuff = 0f;
	public float GeneralDamageBuff
	{

		get
		{
			return m_generalDamageBuff;
		}
		set
		{
			m_generalDamageBuff = value;
		}
	}

	private float m_generalDamageResistance = 0f;
	public float GeneralDamageResistance
	{

		get
		{
			return m_generalDamageResistance;
		}
		set
		{
			m_generalDamageResistance = value;
		}
	}

	private SerializableDictionary<WeaponEquipmentData.DamageType, float> m_applyedDamageTypeBuffs = new();
	public SerializableDictionary<WeaponEquipmentData.DamageType, float> ApplyedDamageTypeBuffs => m_applyedDamageTypeBuffs;

	private SerializableDictionary<WeaponEquipmentData.DamageType, float> m_applyedDamageTypeResitance = new();
	public SerializableDictionary<WeaponEquipmentData.DamageType, float> ApplyedDamageTypeResistance => m_applyedDamageTypeResitance;

	private SerializableDictionary<WeaponEquipmentData.DamageCategory, float> m_applyedDamageCategoryBuffs = new();
	public SerializableDictionary<WeaponEquipmentData.DamageCategory, float> ApplyedDamageCategoryBuffs => m_applyedDamageCategoryBuffs;

	private SerializableDictionary<WeaponEquipmentData.DamageCategory, float> m_applyedDamageCategoryResitance = new();
	public SerializableDictionary<WeaponEquipmentData.DamageCategory, float> ApplyedDamageTypeCategoryResitance => m_applyedDamageCategoryResitance;

	private bool m_didAttackThisTurn = false;
	public bool DidAttackThisTurn => m_didAttackThisTurn;

	private void Awake ()
	{
		m_linkedEntity.onSelect += OnEntitySelected;
		m_linkedEntity.onDeselect += OnEntityDeselected;
		m_linkedEntity.onNewRoundBegin += OnNewPhaseStart;
		m_linkedEntity.onStartPerformAction += OnStartPerformAction;
		TurnManager.onStartInputPhase += OnNewTurnBegin;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.onSelect -= OnEntitySelected;
		m_linkedEntity.onDeselect -= OnEntityDeselected;
		m_linkedEntity.onNewRoundBegin -= OnNewPhaseStart;
		m_linkedEntity.onStartPerformAction -= OnStartPerformAction;
		TurnManager.onStartInputPhase -= OnNewTurnBegin;
	}

	public override void Init ( EntitySavedData _entityData )
	{
		//init weapon
		if (_entityData.arms != null && _entityData.arms.Length > 0)
		{
			foreach (GameDatas.PlayerSave.Component stringContainer in _entityData.arms)
			{
				if (stringContainer == null || !stringContainer.TryGetData(out EntityEquipmentData data))
					continue;

				if (data is WeaponEquipmentData weaponData)
					AddWeapon(weaponData, stringContainer.ID, m_linkedEntity.Displacement.Spawn.isFirstSide);
				else if (data is ToolEquipmentData toolData)
					AddTool(toolData, stringContainer.ID, m_linkedEntity.Displacement.Spawn.isFirstSide);
			}
		}

		//init health
		m_maxHealth = m_linkedEntity.Data.GetMaxHealth();
		m_currentHealth = _entityData.currentHp <= 0 ? m_maxHealth : Mathf.Min(_entityData.currentHp, m_maxHealth);
		m_isDead = false;

		//resistance
		m_generalDamageBuff = m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.GeneralDamageBonus);
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Slash, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.SlashDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Bludgeoning, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.BludgeoningDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Piercing, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.PiercingDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Electric, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.ElectricDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Fire, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.FireDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Laser, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.LaserDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Magnetic, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.MagneticDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Plasma, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.PlasmaDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Radiation, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.RadiationDamageBonus));

		m_generalDamageResistance = m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.GeneralDamageResistance);
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Slash, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.SlashResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Bludgeoning, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.BludgeoningResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Piercing, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.PiercingResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Electric, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.ElectricResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Fire, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.FireResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Laser, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.LaserResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Magnetic, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.MagneticResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Plasma, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.PlasmaResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Radiation, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.RadiationResitance));

		m_applyedDamageCategoryBuffs.Add(WeaponEquipmentData.DamageCategory.Physic, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.PhysicalDamageBonus));
		m_applyedDamageCategoryBuffs.Add(WeaponEquipmentData.DamageCategory.Elemental, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.ElementalDamageBonus));

		m_applyedDamageCategoryResitance.Add(WeaponEquipmentData.DamageCategory.Physic, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.PhysicalDamageResistance));
		m_applyedDamageCategoryResitance.Add(WeaponEquipmentData.DamageCategory.Elemental, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.ElementalDamageResistance));

		base.Init(_entityData);
	}

	#region Callbacks

	private void OnEntitySelected ()
	{
		if (m_isDead)
			return;

		foreach (WeaponCone weaponCone in m_weaponConeDictionary.Values)
		{
			weaponCone.ActivateActiveCone();
		}
	}

	private void OnEntityDeselected ()
	{
		if (m_isDead)
		{
			DisableWeaponCones();
			return;
		}

		foreach (WeaponCone weaponCone in m_weaponConeDictionary.Values)
		{
			weaponCone.ActivateUnactiveCone();
		}
	}

	public void SetWeaponConesHidden ( bool _hidden )
	{
		if (_hidden || m_isDead)
		{
			DisableWeaponCones();
			return;
		}

		if (PlayerController.Instance != null && PlayerController.Instance.SelectedEntity == m_linkedEntity)
			OnEntitySelected();
		else
			OnEntityDeselected();
	}

	private void DisableWeaponCones ()
	{
		foreach (WeaponCone weaponCone in m_weaponConeDictionary.Values)
		{
			weaponCone.DisableAllCones();
		}
	}

	private void OnNewPhaseStart ()
	{
		/*foreach (string equipment in m_equipmentInCooldown.Keys.ToList())
		{
			m_equipmentInCooldown[equipment]--;
			if (m_equipmentInCooldown[equipment] <= 0)
				m_equipmentInCooldown.Remove(equipment);
		}*/
	}

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
		public bool critical;
		public Vector3 hitPos;
		public Vector3 hitNormal;
	}

	private Weapon AddWeapon ( WeaponEquipmentData _data, string _id, bool _isFirstSide )
	{
		Weapon newWeapon = Instantiate(_data.prefab, m_linkedEntity.Skin.IK.handGrabSocket);
		newWeapon.Init(m_linkedEntity, _data, _id, _isFirstSide);
		m_weapons.Add(newWeapon.ID, newWeapon);

		WeaponCone weaponCone = Instantiate(GameAssets.current.game.weaponCone, m_weaponConesParent);
		m_weaponConeDictionary.Add(_data.name, weaponCone);
		weaponCone.Init(m_linkedEntity, _data, m_linkedEntity.Displacement.Spawn.isFirstSide);

		return newWeapon;
	}


	/*public void AimAtTile(string _weaponID, Tile _tile, System.Action _onEndMovement = null )
	{
		//OLD : get angle and apply to cone
		WeaponCone selectedWeaponCone = m_weaponConeDictionary[_weaponID];
		Vector2 currentLocation = new Vector2( m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.x, m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.z);
		Vector2 destination = new Vector2(_tile.transform.position.x, _tile.transform.position.z);

		float angle = GridManager.Instance.GetAngleFrom(currentLocation, destination);
		selectedWeaponCone.AimAtAngle(angle, false, _onEndMovement);

		m_linkedEntity.Displacement.Rotate(_tile, false);
	}*/

	public List<Tile> GetTilesInWeaponRange ( AEntityAction _action, bool _isThisTurn = false )
	{
		Tile from = _isThisTurn ? m_linkedEntity.Displacement.Coordinates.GetTile() : GridManager.Instance.Tiles[_action.supposedPositionAtActionStartID];
		int orientation = m_linkedEntity.Displacement.CurrentOrientation;
		return GetTilesInWeaponRange(_action, _isThisTurn, from, orientation);
	}

	public List<Tile> GetTilesInWeaponRange( AEntityAction _action, bool _isThisTurn, Tile _from, int _orientation, bool _ignoreVisibility = false )
	{
		List<Tile> tilesInRange = new();
		int maxDistance = _action.Data.GetMaxRange(_action, m_linkedEntity, null);
		int minDistance = _action.Data.minDistance;

		bool ignoreObstacles = false;
		foreach (AEntityPassiveEffect.PassiveEffectContainer passiveContainer in _action.effects)
		{
			ignoreObstacles = passiveContainer.enumID == EntityPassiveEffectEnumID.TrajectoryControl;
			break;
		}

		switch (_action.Data.aoeType)
		{
			case EntityActionData.AOEType.Noone:
			case EntityActionData.AOEType.LargeArc:
			case EntityActionData.AOEType.ThinArc:
			case EntityActionData.AOEType.Chain:
			case EntityActionData.AOEType.Ray:
				tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, ignoreObstacles, _isThisTurn, _ignoreVisibility));
				break;
			case EntityActionData.AOEType.LargeCone:
			case EntityActionData.AOEType.ThinCone:
				if (_action.Data.aoECenterType == EntityActionData.AOECenterType.Self)
					tilesInRange.AddRange(GridManager.Instance.GetTilesInCone(_from, minDistance, maxDistance, _orientation, _action.Data.aoeType, _isThisTurn));
				else
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, ignoreObstacles, _isThisTurn, _ignoreVisibility));
				break;
			case EntityActionData.AOEType.Circle:
				if (_action.Data.aoECenterType == EntityActionData.AOECenterType.Self)
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionRange(_from, minDistance, maxDistance, ignoreObstacles, _isThisTurn, _ignoreVisibility));
				else
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, ignoreObstacles, _isThisTurn, _ignoreVisibility));
				break;
		}


		return tilesInRange;
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

	public List<Tile> GetTilesInAoERange ( AEntityAction _action, Tile _targetTile, bool _isThisTurn = false )
	{
		int maxDistance = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self ? _action.Data.GetMaxRange(_action, m_linkedEntity, null) : _action.Data.GetAoEMaxRange(_action, m_linkedEntity, null);
		int minDistance = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self ? _action.Data.minDistance : _action.Data.aoeMinEffectRange;
		Tile from = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self ? _action.PerformingEntity.Displacement.Coordinates.GetTile() : _targetTile;
		int extraValue = _action.Data.maxChainedTarget;
		return GridManager.Instance.GetTilesInAoERange(_action.Data.aoeType, m_linkedEntity, from, _targetTile, minDistance, maxDistance, extraValue, _isThisTurn);
	}

	public bool AttackRoll ( AttackAction _attackAction, AttackAction.SingleAttackInfo _singleAttackInfo, Entity _targetEntity, out Tile _coverTile )
	{
		//WeaponEquipmentData usedWeapon = m_weapons[_attackAction.linkedEquipmentId].Data;
		bool doesWinPFC = _attackAction.DoesWinExchangeAgainst(_targetEntity);

		if (GridManager.Instance.IsThereBlockingWallBetween(_attackAction.PerformingEntity, _targetEntity, doesWinPFC, out _coverTile))
		{
			LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_wall_blocks)
				, m_linkedEntity.Data.name, _targetEntity.Data.name), LogConsole.LogEventType.AttackRoll);
			return false;
		}

		bool isThereCoverBetween = GridManager.Instance.IsThereCoverBeween(_attackAction.PerformingEntity, _targetEntity, doesWinPFC, out _coverTile);

		float targetCamo = _targetEntity.Data.GetStaticStealthBonus(true)
			+ (_targetEntity.HowIsUnitVisible == NeuronalMembraneEquipmentData.VisionTypes.Optic ? _targetEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.VisualCamo, null)
			: _targetEntity.HowIsUnitVisible == NeuronalMembraneEquipmentData.VisionTypes.Radar ? _targetEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.RadarCamo, null)
			: _targetEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.ThermicCamo, null));

		float evationRatio = _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack
				? _targetEntity.Data.BrainData.distanceEvasion + _targetEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.DistanceEvasion, null)
				: _targetEntity.Data.BrainData.meleeEvasion + _targetEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.MeleeEvasion, null);
		float coverRatio = isThereCoverBetween
				? GameConfig.current.game.entityCoverBonus
				: 0f;
		//float distanceRatio = GameConfig.current.game.distanceTypeSpreadEvaluation[GetWeaponDistanceTypeFrom(_targetEntity, _attackAction, doesWinPFC)];

		float targetEvasionScore =
			targetCamo
			+ evationRatio
			+ coverRatio;
		//+ distanceRatio;

		//float userPerception = m_linkedEntity.Data.GetStaticPerceptionBonus(true) + m_linkedEntity.GetAdditionaryStatBonus(EntityEquipmentData.StatBonus.StatType.VisualPerception, _attackAction);
		float userAim = _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack
				? m_linkedEntity.Data.BrainData.distanceAccuracy + m_linkedEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.DistanceAccuracy, _attackAction)
				: m_linkedEntity.Data.BrainData.agility + m_linkedEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.MeleeAccuracy, _attackAction);
		float flankBonus = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(m_linkedEntity, _targetEntity, doesWinPFC)]
			+ m_linkedEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.FlankBonus, _attackAction);
		float modAction = m_linkedEntity.LastActionPerformedData.previousActionAttackModificator;

		float userHitScore =
			//userPerception
			userAim
			+ flankBonus
			+ modAction;

		float finalScore = userHitScore - targetEvasionScore;

		float roll = Random.Range(0f, 1f);
		bool isAttackSuccessful = finalScore >= 1f || finalScore >= roll;

		if (isAttackSuccessful || !isThereCoverBetween || roll < (1-GameConfig.current.game.entityCoverBonus))
			_coverTile = null;

		LocalizationManager localization = LocalizationManager.Instance;
		StringBuilder detailsBuilder = new();
		detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.roll_header), m_linkedEntity.Data.name, _targetEntity.Data.name));
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.roll_attacker_section), userHitScore.ToPercent()));
		detailsBuilder.AppendPercentLine(LocalizationKey.roll_aim, userAim);
		detailsBuilder.AppendPercentLine(LocalizationKey.roll_flank_bonus, flankBonus);
		detailsBuilder.AppendPercentLine(LocalizationKey.roll_action_modifier, modAction);
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.roll_target_section), targetEvasionScore.ToPercent()));
		detailsBuilder.AppendPercentLine(LocalizationKey.roll_camouflage, targetCamo);
		detailsBuilder.AppendPercentLine(LocalizationKey.roll_evasion, evationRatio);
		detailsBuilder.AppendPercentLine(LocalizationKey.roll_cover_bonus, coverRatio);
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.roll_final_score)
			, userHitScore.ToPercent(), targetEvasionScore.ToPercent(), finalScore.ToPercent()));

		if (finalScore >= 1f)
			detailsBuilder.AppendLine(localization.Get(LocalizationKey.roll_guaranteed_hit));
		else
		{
			detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.roll_roll), roll.ToPercent()));
			detailsBuilder.AppendLine(localization.Get(isAttackSuccessful ? LocalizationKey.roll_hit_success : LocalizationKey.roll_hit_failed));
		}

		string detailsDescription = detailsBuilder.ToString();
		LogConsole.LogDetails details = new("attack_" + LogConsole.Instance.Counter, localization.Get(LocalizationKey.roll_title), detailsDescription);

		LogConsole.AddLog(localization.Get(isAttackSuccessful ? LocalizationKey.log_attack_success : LocalizationKey.log_attack_failure), LogConsole.LogEventType.AttackRoll, details);
		return isAttackSuccessful;
	}

	public WeaponEquipmentData.DistanceType GetWeaponDistanceTypeFrom ( Entity _target, AEntityAction _action, bool _didAttackerWinPFC )
	{
		int attackerPosition = _didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID);
		int defenderPosition = !_didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(m_linkedEntity.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(m_linkedEntity.ID);
		float actualDistanceFromTarget = Vector3.Distance(GridManager.Instance.Tiles[attackerPosition].transform.position, GridManager.Instance.Tiles[defenderPosition].transform.position) / (Tile.outerRadius * 2f);

		int maxDistance = _action.Data.GetMaxRange(_action, m_linkedEntity, _target);
		float distanceRelativeToWeaponRangePercentage = actualDistanceFromTarget / (float)maxDistance;

		float currentTotal = 0;
		for (int i = 0; i < GameConfig.current.game.distanceTypeSpreadEvaluation.Keys.Count; i++)
		{
			if (distanceRelativeToWeaponRangePercentage < currentTotal + GameConfig.current.game.distanceTypeSpreadEvaluation[(WeaponEquipmentData.DistanceType)i])
				return (WeaponEquipmentData.DistanceType)i;

			currentTotal += GameConfig.current.game.distanceTypeSpreadEvaluation[(WeaponEquipmentData.DistanceType)i];
		}
		return WeaponEquipmentData.DistanceType.Long;
	}

	public bool StatusRoll ( Entity _target, AEntityStatus _effect, AEntityAction _action, EntityEquipmentData _equipmentData )
	{
		float actionProbability = _action.Data.statusHitProbability;
		float equipmentProbability = _equipmentData.statusHitProbability;
		float userStatusChance = m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.StatusChance) + m_linkedEntity.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusChance, _action);
		float targetResistance = _target.Data.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.StatusResistance) + _target.GetAdditionaryStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusResistance, null);
		float hitProba = actionProbability + equipmentProbability + userStatusChance - targetResistance;
		float roll = Random.Range(0f, 1f);
		bool isAttackSuccessful = hitProba >= 1f || roll <= hitProba;

		LocalizationManager localization = LocalizationManager.Instance;
		StringBuilder detailsBuilder = new();
		detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.status_roll_header), m_linkedEntity.Data.name, _effect.GetLocalizedName(), _target.Data.name));
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.status_roll_section), Mathf.Clamp01(hitProba).ToPercent()));
		detailsBuilder.AppendPercentLine(LocalizationKey.status_roll_base_chance, actionProbability);
		detailsBuilder.AppendPercentLine(LocalizationKey.status_roll_equipment_bonus, equipmentProbability);
		detailsBuilder.AppendPercentLine(LocalizationKey.status_roll_chance_bonus, userStatusChance);
		detailsBuilder.AppendPercentLine(LocalizationKey.status_roll_target_resistance, -targetResistance);
		detailsBuilder.AppendLine();
		if (hitProba >= 1f)
			detailsBuilder.AppendLine(localization.Get(LocalizationKey.status_roll_guaranteed));
		else
		{
			detailsBuilder.AppendLine(string.Format(localization.Get(LocalizationKey.roll_roll), roll.ToPercent()));
			detailsBuilder.AppendLine(localization.Get(isAttackSuccessful ? LocalizationKey.status_roll_applied : LocalizationKey.status_roll_resisted));
		}

		string detailsDescription = detailsBuilder.ToString();
		LogConsole.LogDetails details = new("status_roll_" + LogConsole.Instance.Counter, localization.Get(LocalizationKey.status_roll_title), detailsDescription);
		LogConsole.AddLog(string.Format(localization.Get(isAttackSuccessful ? LocalizationKey.status_roll_log_applied : LocalizationKey.status_roll_log_failed)
			, m_linkedEntity.Data.name, _effect.GetLocalizedName(), _target.Data.name), LogConsole.LogEventType.Status, details);


		return isAttackSuccessful;
	}

	#endregion

	#region Tool

	private Tool AddTool ( ToolEquipmentData _data, string _id, bool _isFirstSide )
	{
		Tool newTool = Instantiate(_data.prefab, m_linkedEntity.Skin.IK.handGrabSocket);
		newTool.Init(m_linkedEntity, _data, _id, _isFirstSide);
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
		//apply flat damage reduction (ex: Shield)
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new(_damageInfo.damages);
		if (_damageInfo.entityAttacker != null)
		{
			foreach (Tool tool in m_tools.Values)
			{
				if (tool is Shield shield
					&& shield.orientation == GridManager.Instance.GetClosestOrientation(m_linkedEntity.Displacement.Coordinates.GetTile(), _damageInfo.entityAttacker.Displacement.Coordinates.GetTile()))
				{
					foreach (WeaponEquipmentData.DamageType damageType in damages.Keys)
					{
						damages[damageType] -= shield.RemoveDamage(damages[damageType]);
					}
				}
			}
		}

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in damages)
		{
			m_currentHealth -= pair.Value;
		}

		if (m_currentHealth <= 0)
			Death(_damageInfo);

		onHealthChangeDamage?.Invoke(_damageInfo);
	}

	public void InstantDeath ()
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
		Death(deathInfo);
	}

	private static readonly HashSet<EntityEquipmentData.SecondaryStat.StatType> m_statsHiddenInDeathTooltip = new()
	{
		EntityEquipmentData.SecondaryStat.StatType.BaseHp,
		EntityEquipmentData.SecondaryStat.StatType.EnergyCost,
		EntityEquipmentData.SecondaryStat.StatType.EnergyProduced,
		EntityEquipmentData.SecondaryStat.StatType.Action,
		EntityEquipmentData.SecondaryStat.StatType.ChipsetSlot,
		EntityEquipmentData.SecondaryStat.StatType.EquipmentSlot,
		EntityEquipmentData.SecondaryStat.StatType.ArmourySlot,
		EntityEquipmentData.SecondaryStat.StatType.OccultorSlot,
	};

	private string BuildDeathTooltip ( TakeDamageCallback _damageInfo )
	{
		LocalizationManager localization = LocalizationManager.Instance;
		StringBuilder builder = new();
		builder.AppendLine("<b>" + localization.Get(LocalizationKey.death_title) + "</b>");
		builder.AppendLine(string.Format(localization.Get(LocalizationKey.death_tick), TurnManager.currentTick));

		if (_damageInfo.entityAttacker != null && _damageInfo.entityAttacker != m_linkedEntity)
		{
			builder.AppendLine(string.Format(localization.Get(LocalizationKey.death_killed_by), _damageInfo.entityAttacker.Data.name));
			if (_damageInfo.entityAttacker.LastPerformedAction != null)
				builder.AppendLine(string.Format(localization.Get(LocalizationKey.death_action), _damageInfo.entityAttacker.LastPerformedAction.Data.GetLocalizedName()));
		}

		if (_damageInfo.damages != null)
		{
			int total = 0;
			foreach (int value in _damageInfo.damages.Values)
				total += value;
			builder.AppendLine(string.Format(localization.Get(LocalizationKey.death_killing_blow), total)
				+ (_damageInfo.critical ? localization.Get(LocalizationKey.death_critical) : ""));
		}

		builder.AppendLine();
		builder.AppendLine($"<b>{m_linkedEntity.Data.name}</b>");
		builder.AppendLine(string.Format(localization.Get(LocalizationKey.death_health), Mathf.Max(0, m_currentHealth), m_maxHealth));

		foreach (KeyValuePair<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> stat in m_linkedEntity.Data.GetStatsDesciptions())
		{
			if (m_statsHiddenInDeathTooltip.Contains(stat.Key))
				continue;

			builder.AppendLine($"{stat.Value.title}: {stat.Value.stringValue}");
		}

		return builder.ToString();
	}


	private void Death ( TakeDamageCallback _damageInfo )
	{
		if (m_isDead)
			return;

		if (m_linkedEntity.LastPerformedAction != null && m_linkedEntity.LastPerformedAction.IsPerforming)
			m_linkedEntity.LastPerformedAction.CancelAction();
		
		//Every tile, not just the one under it: CancelAction above may also have re-registered it, and a booked
		//destination lives on a tile the entity never reached.
		GridManager.Instance.ClearEntityFromAllTiles(m_linkedEntity);

		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_death), m_linkedEntity.Data.name), LogConsole.LogEventType.Death
			, new LogConsole.LogDetails("death_" + LogConsole.Instance.Counter, m_linkedEntity.Data.name, BuildDeathTooltip(_damageInfo)));

		m_isDead = true;
		onDeath?.Invoke(m_linkedEntity.ID);
		onAnyEntityDeath?.Invoke(m_linkedEntity);

		DisableWeaponCones();
	}

	#endregion

	/*private void OnDrawGizmos ()
	{
		foreach(string weapongID in m_weapons.Keys)
		{
			Weapon selectedWeapon = m_weapons[weapongID];
			//shoot ray from tile to other tiles in range
			float angle = selectedWeapon.aimedRotation;

			int nbOfRayPerAngle = 1;
			int totalNbOfRay = selectedWeapon.Data.visionConeRange * nbOfRayPerAngle;
			for (int i = 0; i < totalNbOfRay; i++)
			{
				//calculate angle
				float rayAngle = Mathf.Lerp(angle + (selectedWeapon.Data.visionConeRange / 2), angle - (selectedWeapon.Data.visionConeRange / 2), (float)i / (float)totalNbOfRay);
				rayAngle += 90f;
				//get position in at angle Y at distance X from linkedEntity
				if (rayAngle < 0)
					rayAngle += 360;

				float radians = rayAngle * Mathf.Deg2Rad;
				Vector3 aimedPosition = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));

				Gizmos.color = Color.red;
				Gizmos.DrawRay(m_linkedEntity.Displacement.Coordinates.GetTile().transform.position, aimedPosition * selectedWeapon.Data.range);
			}
		}
	}*/
}
