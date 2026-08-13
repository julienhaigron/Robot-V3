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
			foreach (GameDatas.PlayerSave.Equipment stringContainer in _entityData.arms)
			{
				if (stringContainer == null || !stringContainer.TryGetData(out EntityEquipmentData data))
					continue;

				if (data is WeaponEquipmentData weaponData)
					AddWeapon(weaponData, m_linkedEntity.Displacement.Spawn.isFirstSide);
				else if (data is ToolEquipmentData toolData)
					AddTool(toolData, m_linkedEntity.Displacement.Spawn.isFirstSide);
			}
		}

		//init health
		m_maxHealth = m_linkedEntity.Data.GetMaxHealth();
		m_currentHealth = _entityData.currentHp;
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
		foreach (WeaponCone weaponCone in m_weaponConeDictionary.Values)
		{
			weaponCone.ActivateActiveCone();
		}
	}

	private void OnEntityDeselected ()
	{
		foreach (WeaponCone weaponCone in m_weaponConeDictionary.Values)
		{
			weaponCone.ActivateUnactiveCone();
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

	private Weapon AddWeapon ( WeaponEquipmentData _data, bool _isFirstSide )
	{
		Weapon newWeapon = Instantiate(_data.prefab, m_linkedEntity.Skin.IK.handGrabSocket);
		newWeapon.Init(m_linkedEntity, _data, _isFirstSide);
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

	public List<Tile> GetTilesInWeaponRange( AEntityAction _action, bool _isThisTurn, Tile _from, int _orientation )
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
				tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, ignoreObstacles, _isThisTurn));
				break;
			case EntityActionData.AOEType.LargeCone:
			case EntityActionData.AOEType.ThinCone:
				if (_action.Data.aoECenterType == EntityActionData.AOECenterType.Self)
					tilesInRange.AddRange(GridManager.Instance.GetTilesInCone(_from, minDistance, maxDistance, _orientation, _action.Data.aoeType, _isThisTurn));
				else
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, ignoreObstacles, _isThisTurn));
				break;
			case EntityActionData.AOEType.Circle:
				if (_action.Data.aoECenterType == EntityActionData.AOECenterType.Self)
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionRange(_from, minDistance, maxDistance, ignoreObstacles, _isThisTurn, false));
				else
					tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionCone(_from, minDistance, maxDistance, _orientation, ignoreObstacles, _isThisTurn));
				break;
		}


		return tilesInRange;
	}

	public List<Tile> GetTilesInAoERange ( AttackAction _action, Tile _targetTile, bool _isThisTurn = false )
	{
		int maxDistance = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self ? _action.Data.GetMaxRange(_action, m_linkedEntity, null) : _action.Data.GetAoEMaxRange(_action, m_linkedEntity, null);
		int minDistance = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self ? _action.Data.minDistance : _action.Data.aoeMinEffectRange;
		Tile from = _action.Data.aoECenterType == EntityActionData.AOECenterType.Self ? _action.PerformingEntity.Displacement.Coordinates.GetTile() : _targetTile;
		int extraValue = _action.Data.maxChainedTarget;
		return GridManager.Instance.GetTilesInAoERange(_action.Data.aoeType, m_linkedEntity, from, _targetTile, minDistance, maxDistance, extraValue, _isThisTurn);
	}

	public bool AttackRoll ( AttackAction _attackAction, AttackAction.SingleAttackInfo _singleAttackInfo, Entity _targetEntity, out Tile _coverTile )
	{
		WeaponEquipmentData usedWeapon = m_weapons[_attackAction.linkedEquipmentId].Data;
		bool doesWinPFC = _singleAttackInfo.pfcResult == (int)EntityActionData.PFCResultType.FirstWins;
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

		if (isAttackSuccessful || !isThereCoverBetween || roll > GameConfig.current.game.entityCoverBonus)
			_coverTile = null;

		StringBuilder detailsBuilder = new();
		detailsBuilder.AppendLine($"<b>{m_linkedEntity.ID}</b> attacks <b>{_targetEntity.ID}</b>");
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine("<b>Attacker Hit Score</b>");
		//detailsBuilder.AppendLine($"Perception: {userPerception:+0.##;-0.##;0}");
		detailsBuilder.AppendLine($"Aim: {userAim:+0.##;-0.##;0}");

		if (flankBonus != 0)
			detailsBuilder.AppendLine($"Flank Bonus: {flankBonus:+0.##;-0.##;0}");

		if (modAction != 0)
			detailsBuilder.AppendLine($"Action Modifier: {modAction:+0.##;-0.##;0}");

		detailsBuilder.AppendLine($"<b>Total Hit Score: {userHitScore:F2}</b>");
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine("<b>Target Evasion Score</b>");
		detailsBuilder.AppendLine($"Camouflage: {targetCamo:+0.##;-0.##;0}");
		detailsBuilder.AppendLine($"Evasion: {evationRatio:+0.##;-0.##;0}");

		if (coverRatio > 0)
			detailsBuilder.AppendLine($"Cover Bonus: +{coverRatio:0.##}");

		/*if (distanceRatio != 0)
			detailsBuilder.AppendLine($"Distance Modifier: {distanceRatio:+0.##;-0.##;0}");*/

		detailsBuilder.AppendLine($"<b>Total Evasion: {targetEvasionScore:F2}</b>");
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine($"Final Score = {userHitScore:F2} - {targetEvasionScore:F2}");

		if (finalScore >= 1f)
			detailsBuilder.AppendLine($"<color=green><b>Guaranteed Hit ({finalScore:F2})</b></color>");
		else
		{
			detailsBuilder.AppendLine($"Hit Chance: {(finalScore * 100f):F0}%");
			detailsBuilder.AppendLine($"Roll: {roll:F2}");
			detailsBuilder.AppendLine(isAttackSuccessful ? "<color=green><b>Hit Success</b></color>" : "<color=red><b>Hit Failed</b></color>");
			if(_coverTile != null)
				detailsBuilder.AppendLine($"Will hit tile : {_coverTile.coordinates.ID}");
		}

		string detailsDescription = detailsBuilder.ToString();
		LogConsole.LogDetails details = new("attack_" + LogConsole.Instance.LogsDetails.Keys.Count, "Attack Details", detailsDescription);
		LogConsole.AddLog(
			m_linkedEntity.ID
			+ (isAttackSuccessful ? " succeeds " : " fails ")
			+ _attackAction.ToString()
			+ " against "
			+ _targetEntity.ID,
			LogConsole.LogEventType.AttackRoll,
			details
		);
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

		StringBuilder detailsBuilder = new();
		detailsBuilder.AppendLine($"<b>{m_linkedEntity.ID}</b> tries to apply <b>{_effect.GetType().Name}</b> on <b>{_target.ID}</b>");
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine("<b>Status Chance Calculation</b>");
		detailsBuilder.AppendLine($"Base Chance: {actionProbability:+0.##%;-0.##%;0%}");
		detailsBuilder.AppendLine($"Equipment Bonus: {equipmentProbability:+0.##%;-0.##%;0%}");
		detailsBuilder.AppendLine($"Status Chance Bonus: {userStatusChance:+0.##%;-0.##%;0%}");
		detailsBuilder.AppendLine($"Target Resistance: -{targetResistance:0.##%}");
		detailsBuilder.AppendLine();
		detailsBuilder.AppendLine($"<b>Final Chance: {Mathf.Clamp01(hitProba):P0}</b>");
		if (hitProba >= 1f)
			detailsBuilder.AppendLine("<color=green><b>Guaranteed Apply</b></color>");
		else
		{
			detailsBuilder.AppendLine($"Roll: {roll:F2}");
			detailsBuilder.AppendLine(isAttackSuccessful ? "<color=green><b>Status Applied</b></color>" : "<color=red><b>Status Resisted</b></color>");
		}

		string detailsDescription = detailsBuilder.ToString();
		LogConsole.LogDetails details = new("status_" + LogConsole.Instance.LogsDetails.Keys.Count, "Status Details", detailsDescription);
		LogConsole.AddLog(m_linkedEntity.ID + (isAttackSuccessful ? " applies " : " fails to apply ")
			+ _effect.GetType().Name + " on " + _target.ID, LogConsole.LogEventType.Status, details);

		return isAttackSuccessful;
	}

	#endregion

	#region Tool

	private Tool AddTool ( ToolEquipmentData _data, bool _isFirstSide )
	{
		Tool newTool = Instantiate(_data.prefab, m_linkedEntity.Skin.IK.handGrabSocket);
		newTool.Init(m_linkedEntity, _data, _isFirstSide);
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
			Death();

		onHealthChangeDamage?.Invoke(_damageInfo);
	}

	public void InstantDeath ()
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		damages.Add(WeaponEquipmentData.DamageType.Bludgeoning, 999999);
		m_currentHealth = 0;
		onHealthChangeDamage?.Invoke(new TakeDamageCallback()
		{
			critical = false,
			damages = damages,
			entityAttacker = m_linkedEntity,
			entityTargeted = m_linkedEntity,
			hitNormal = Vector3.zero,
			hitPos = Vector3.zero
		});

		Death();
	}

	private void Death ()
	{
		if (m_linkedEntity.LastPerformedAction != null && m_linkedEntity.LastPerformedAction.IsPerforming)
			m_linkedEntity.LastPerformedAction.CancelAction();
		
		m_linkedEntity.Displacement.Coordinates.GetTile().ClearEntityAnyTurn(m_linkedEntity);

		string detailsDescription = m_linkedEntity.ID + " died";
		//LogConsole.LogDetails details = new("death_" + LogConsole.Instance.LogsDetails.Keys.Count, "Damage Details", detailsDescription);
		LogConsole.AddLog(detailsDescription, LogConsole.LogEventType.Damage/*, details*/);

		m_isDead = true;
		onDeath?.Invoke(m_linkedEntity.ID);
		onAnyEntityDeath?.Invoke(m_linkedEntity);
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
