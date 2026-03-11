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
	[SerializeField] private Transform m_weaponConesParent;

	private Dictionary<string, WeaponCone> m_weaponConeDictionary = new();

	private Dictionary<string, Tool> m_tools = new();
	public Dictionary<string, Tool> Tools => m_tools;

	private int m_currentHealth;
	public int CurrentHealth => m_currentHealth;

	private int m_maxHealth;
	public int MaxHealth => m_maxHealth;

	private bool m_isDead = false;
	public bool IsDead => m_isDead;

	private SerializableDictionary<EntityActionEnumID, int> m_actionsInCooldown = new();
	public SerializableDictionary<EntityActionEnumID, int> ActionInCooldown => m_actionsInCooldown;

	[Title("Stats")]
	private float m_generalDamageBuff = 0f;
	public float GeneralDamageBuff => m_generalDamageBuff;

	private float m_generalDamageResistance = 0f;
	public float GeneralDamageResistance => m_generalDamageResistance;

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
		if (_entityData.armsIds != null && _entityData.armsIds.Length > 0)
		{
			foreach (StringContainer stringContainer in _entityData.armsIds)
			{
				if (GameAssets.current.equipments[stringContainer.value] is WeaponEquipmentData weaponData)
					AddWeapon(weaponData, m_linkedEntity.Displacement.Spawn.isFirstSide);
				else if (GameAssets.current.equipments[stringContainer.value] is ToolEquipmentData toolData)
					AddTool(toolData, m_linkedEntity.Displacement.Spawn.isFirstSide);
			}
		}

		//init health
		m_maxHealth = m_linkedEntity.Data.GetMaxHealth();
		m_currentHealth = m_maxHealth;
		m_isDead = false;

		//resistance
		m_generalDamageBuff = m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.GeneralDamageBonus);
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Tranchant, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.SlashDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Contendant, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.BludgeoningDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Perforant, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.PiercingDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Electrique, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.ElectricDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Feu, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FireDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Laser, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.LaserDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Magnetique, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.MagneticDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Plasma, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.PlasmaDamageBonus));
		m_applyedDamageTypeBuffs.Add(WeaponEquipmentData.DamageType.Radiation, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.RadiationDamageBonus));

		m_generalDamageResistance = m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.GeneralDamageResistance);
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Tranchant, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.SlashResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Contendant, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.BludgeoningResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Perforant, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.PiercingResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Electrique, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.ElectricResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Feu, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.FireResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Laser, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.LaserResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Magnetique, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.MagneticResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Plasma, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.PlasmaResitance));
		m_applyedDamageTypeResitance.Add(WeaponEquipmentData.DamageType.Radiation, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.RadiationResitance));

		m_applyedDamageCategoryBuffs.Add(WeaponEquipmentData.DamageCategory.Physic, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.PhysicalDamageBonus));
		m_applyedDamageCategoryBuffs.Add(WeaponEquipmentData.DamageCategory.Elemental, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.ElementalDamageBonus));

		m_applyedDamageCategoryResitance.Add(WeaponEquipmentData.DamageCategory.Physic, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.PhysicalDamageResistance));
		m_applyedDamageCategoryResitance.Add(WeaponEquipmentData.DamageCategory.Elemental, m_linkedEntity.Data.GetStatBonusFromAll(EntityEquipmentData.StatBonus.StatType.ElementalDamageResistance));

		base.Init(_entityData);
	}

	#region Callbacks

	private void OnEntitySelected ()
	{
		TurnManager.Instance.SetCurrentActionSelected(EntityActionEnumID.TargetTileMove);

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
		foreach (EntityActionEnumID action in m_actionsInCooldown.Keys.ToList())
		{
			m_actionsInCooldown[action]--;
			if (m_actionsInCooldown[action] <= 0)
				m_actionsInCooldown.Remove(action);
		}
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
		m_weapons.Add(_data.name, newWeapon);

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

	public List<Tile> GetTilesInWeaponRange (AEntityAction _action, string _weaponID, bool _isThisTurn = false )
	{
		List<Tile> tilesInRange = new();
		Weapon usedWeapon = m_weapons[_weaponID];

		float angle = GridManager.Instance.FromOrientationToAngle(m_linkedEntity.Displacement.CurrentOrientation);

		bool ignoreObstacles = _action.effects.Contains(EntityPassiveEffectEnumID.TrajectoryControl);

		int nbOfRayPerAngle = 1;
		int totalNbOfRay = usedWeapon.Data.visionConeRange * nbOfRayPerAngle;
		for (int i = 0; i < totalNbOfRay; i++)
		{
			//calculate angle
			float rayAngle = Mathf.LerpAngle(angle - (usedWeapon.Data.visionConeRange / 2), angle + (usedWeapon.Data.visionConeRange / 2), (float)i / (float)totalNbOfRay);
			rayAngle += 90f;
			//get position in at angle Y at distance X from linkedEntity
			if (rayAngle < 0)
				rayAngle += 360;

			float radians = rayAngle * Mathf.Deg2Rad;
			Vector3 aimedPosition = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
			RaycastHit[] hits = Physics.RaycastAll(m_linkedEntity.Displacement.Coordinates.GetTile().transform.position, aimedPosition * usedWeapon.Data.range, usedWeapon.Data.range * (2 * Tile.innerRadius), GameConfig.current.input.tileInternRayCastLayer);
			foreach (RaycastHit hitInfo in hits)
			{
				if (hitInfo.transform.TryGetComponent(out Tile tile) && !tilesInRange.Contains(tile)
					&& (ignoreObstacles || GridManager.Instance.IsVisionLineClear(m_linkedEntity.Displacement.Coordinates.GetTile(), tile, _isThisTurn)))
				{
					tilesInRange.Add(tile);
				}
			}
		}

		return tilesInRange;
	}


	public List<Tile> GetTilesInAoERange ( AttackAction _action, bool _isThisTurn = false )
	{
		List<Tile> tilesInRange = new();
		EntityActionData attackData = GameAssets.current.game.entityActionsData[_action.enumID];

		Weapon usedWeapon = null;
		foreach (string weaponID in m_weapons.Keys)
		{
			if (m_weapons[weaponID].Data.knownedActions.Contains(attackData.enumID))
			{
				usedWeapon = m_weapons[weaponID];
				break;
			}
		}
		if (usedWeapon == null)
		{
			Debug.LogError("Error : trying to use an attack not in a weapon. No weapon found for action " + _action.enumID.ToString());
			return tilesInRange;
		}

		if (!attackData.isAoe)
			return tilesInRange;

		switch (attackData.aoeType)
		{
			case EntityActionData.AOEType.Circle:

				tilesInRange.AddRange(GridManager.Instance.GetTilesInVisionRange(_action.TargetTile, attackData.circleRange, _isThisTurn));
				break;
			case EntityActionData.AOEType.Ray:

				tilesInRange.AddRange(GridManager.Instance.GetTilesInRay(_action.PerformingEntity.Displacement.Coordinates.GetTile(), _action.TargetTile, _isThisTurn));
				break;
			case EntityActionData.AOEType.Cone:

				tilesInRange.AddRange(GridManager.Instance.GetTilesInCone(m_linkedEntity.Displacement.Coordinates.GetTile()
						, usedWeapon.Data.range, m_linkedEntity.Displacement.CurrentOrientation, attackData.coneType, _isThisTurn));
				break;
			case EntityActionData.AOEType.Arc:

				float angle = GridManager.Instance.FromOrientationToAngle(m_linkedEntity.Displacement.CurrentOrientation);

				int nbOfRayPerAngle = 1;
				int totalNbOfRay = usedWeapon.Data.visionConeRange * nbOfRayPerAngle;
				for (int i = 0; i < totalNbOfRay; i++)
				{
					//calculate angle
					float rayAngle = Mathf.LerpAngle(angle - (usedWeapon.Data.visionConeRange / 2), angle + (usedWeapon.Data.visionConeRange / 2), (float)i / (float)totalNbOfRay);
					rayAngle += 90f;
					//get position in at angle Y at distance X from linkedEntity
					if (rayAngle < 0)
						rayAngle += 360;

					float radians = rayAngle * Mathf.Deg2Rad;
					Vector3 aimedPosition = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
					RaycastHit[] hits = Physics.RaycastAll(m_linkedEntity.Displacement.Coordinates.GetTile().transform.position, aimedPosition * usedWeapon.Data.range, usedWeapon.Data.range * (2 * Tile.innerRadius), GameConfig.current.input.tileInternRayCastLayer);
					foreach (RaycastHit hitInfo in hits)
					{
						if (hitInfo.transform.TryGetComponent(out Tile tile) && !tilesInRange.Contains(tile)
							&& GridManager.Instance.IsVisionLineClear(m_linkedEntity.Displacement.Coordinates.GetTile(), tile, _isThisTurn))
						{
							tilesInRange.Add(tile);
						}
					}
				}

				break;
		}


		return tilesInRange;
	}

	public bool AttackRoll ( AttackAction _attackAction )
	{
		Entity targetEntity = _attackAction.TargetEntity;
		WeaponEquipmentData usedWeapon = m_weapons[_attackAction.attackingWeaponId].Data;
		bool doesWinPFC = _attackAction.pfcResult == (int)EntityActionData.PFCResultType.FirstWins;

		float targetCamo = targetEntity.Data.GetStaticStealthBonus(true);
		float evationRatio = _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack ? targetEntity.Data.BrainData.distanceEvasion : targetEntity.Data.BrainData.meleeEvasion;
		float coverRatio = GridManager.Instance.IsThereCoverBeween(_attackAction.PerformingEntity, targetEntity, doesWinPFC) ? GameConfig.current.game.entityCoverBonus : 0;
		float distanceRatio = m_weapons[_attackAction.attackingWeaponId].Data.distanceAccuracyBonus[GetWeaponDistanceTypeFrom(targetEntity, usedWeapon, doesWinPFC)];
		float targetEvasionScore = targetCamo + evationRatio + coverRatio + distanceRatio;

		float userPerception = m_linkedEntity.Data.GetStaticPerceptionBonus(true);
		float userAim = _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack ? m_linkedEntity.Data.BrainData.distanceAccuracy : m_linkedEntity.Data.BrainData.agility;
		float flankBonus = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(m_linkedEntity, targetEntity, doesWinPFC)];
		float modAction = m_linkedEntity.LastActionPerformedData.previousActionAttackModificator;
		float userHitScore = userPerception + userAim + flankBonus + modAction;

		float finalScore = userHitScore - targetEvasionScore;

		if (finalScore >= 1)
		{
			LogConsole.AddLog("Attack Roll [AUTOMATIC SUCESS] : targetEvasionScore = " + targetEvasionScore + " and userHitScore = " + userHitScore, LogConsole.LogEventType.PlayPhase);
			return true;
		}
		else
		{
			float roll = Random.Range(0f, 1f);
			bool isAttackSuccessful = finalScore >= roll;
			//bool isAttackSuccessful = roll + finalScore > 1;
			LogConsole.AddLog("Attack Roll " + (isAttackSuccessful ? "[SUCESS]" : "[FAILURE]") + " : targetEvasionScore = " + targetEvasionScore + ", roll = " + roll + " and userHitScore = " + userHitScore, LogConsole.LogEventType.PlayPhase);
			return isAttackSuccessful;
		}
	}

	public WeaponEquipmentData.DistanceType GetWeaponDistanceTypeFrom ( Entity _target, WeaponEquipmentData _weaponData, bool _didAttackerWinPFC )
	{
		int attackerPosition = _didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID);
		int defenderPosition = !_didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID);
		float actualDistanceFromTarget = Vector3.Distance(GridManager.Instance.Tiles[attackerPosition].transform.position, GridManager.Instance.Tiles[defenderPosition].transform.position) / (Tile.outerRadius * 2f);
		float distanceRelativeToWeaponRangePercentage = actualDistanceFromTarget / (float)_weaponData.range;

		float currentTotal = 0;
		for (int i = 0; i < GameConfig.current.game.distanceTypeSpreadEvaluation.Keys.Count; i++)
		{
			if (distanceRelativeToWeaponRangePercentage < currentTotal + GameConfig.current.game.distanceTypeSpreadEvaluation[(WeaponEquipmentData.DistanceType)i])
				return (WeaponEquipmentData.DistanceType)i;

			currentTotal += GameConfig.current.game.distanceTypeSpreadEvaluation[(WeaponEquipmentData.DistanceType)i];
		}
		return WeaponEquipmentData.DistanceType.Long;
	}

	public bool StatusRoll ( Entity _entity, AEntityStatus _effect )
	{
		bool isAttackSuccessful = Random.Range(0, 100) > _effect.hitProbability;

		//TODO
		//take possible build buff into acount

		return isAttackSuccessful;
	}

	#endregion

	#region Tool

	private Tool AddTool ( ToolEquipmentData _data, bool _isFirstSide )
	{
		Tool newTool = Instantiate(_data.prefab, m_linkedEntity.Skin.IK.handGrabSocket);
		newTool.Init(m_linkedEntity, _data, _isFirstSide);
		m_tools.Add(_data.name, newTool);

		return newTool;
	}

	#endregion

	#region Heatlh

	public void TakeDamage ( TakeDamageCallback _damageInfo )
	{
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

	private void Death ()
	{
		m_linkedEntity.Displacement.Coordinates.GetTile().SetEntity(null, true);

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
