using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEquipmentPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityDeath;
	public System.Action<int> onDeath;
	public System.Action<TakeDamageCallback> onHealthChangeDamage;

    private Dictionary<string, Weapon> m_weapons = new();
	public Dictionary<string, Weapon> Weapons => m_weapons;
	[SerializeField] private Transform m_weaponConesParent;

	private Dictionary<string, WeaponCone> m_weaponConeDictionary = new();

	private int m_currentHealth;
	public int CurrentHealth => m_currentHealth;

	public int MaxHealth => m_linkedEntity.Data.FrameData.maxHealth;

	private bool m_isDead = false;
	public bool IsDead => m_isDead;

	private void Awake ()
	{
		m_linkedEntity.onSelect += OnEntitySelected;
		m_linkedEntity.onDeselect += OnEntityDeselected;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.onSelect -= OnEntitySelected;
		m_linkedEntity.onDeselect -= OnEntityDeselected;
	}

	public override void Init ( EntitySavedData _entityData )
	{
		//init weapon
		if(_entityData.armsIds != null && _entityData.armsIds.Length > 0)
		{
			foreach(StringContainer stringContainer in _entityData.armsIds)
			{
				AddWeapon(GameAssets.current.equipments[stringContainer.value] as WeaponEquipmentData, m_linkedEntity.Displacement.Spawn.isFirstSide);
			}
		}

		//init health
		m_currentHealth = MaxHealth;
		m_isDead = false;
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

	#endregion

	#region Weapon

	public struct TakeDamageCallback
	{
		public Entity entityAttacker;
		public Entity entityTargeted;
		public int damage;
		public bool critical;
		public Vector3 hitPos;
		public Vector3 hitNormal;
	}

	private Weapon AddWeapon(WeaponEquipmentData _data, bool _isFirstSide)
	{
		Weapon newWeapon = Instantiate(GameAssets.current.game.weapons[_data.ID], m_linkedEntity.Skin.IK.handGrabSocket);
		newWeapon.Init(m_linkedEntity, _data, _isFirstSide);
		m_weapons.Add(_data.ID, newWeapon);

		WeaponCone weaponCone = Instantiate(GameAssets.current.game.weaponCone, m_weaponConesParent);
		m_weaponConeDictionary.Add(_data.ID, weaponCone);
		weaponCone.Init(m_linkedEntity, _data, m_linkedEntity.Displacement.Spawn.isFirstSide);

		return newWeapon;
	}


	public void AimAtTile(string _weaponID, Tile _tile, System.Action _onEndMovement = null )
	{
		//OLD : get angle and apply to cone
		WeaponCone selectedWeaponCone = m_weaponConeDictionary[_weaponID];
		Vector2 currentLocation = new Vector2( m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.x, m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.z);
		Vector2 destination = new Vector2(_tile.transform.position.x, _tile.transform.position.z);

		float angle = GridManager.Instance.GetAngleFrom(currentLocation, destination);
		selectedWeaponCone.AimAtAngle(angle, false, _onEndMovement);

		m_linkedEntity.Displacement.Rotate(_tile, false);
	}

	public List<Tile> GetTilesInRange(string _weaponID, bool _isThisTurn = false)
	{
		List<Tile> tilesInRange = new();

		WeaponCone selectedWeapon = m_weaponConeDictionary[_weaponID];
		//shoot ray from tile to other tiles in range
		float angle = selectedWeapon.AimedRotation;

		int nbOfRayPerAngle = 1;
		int totalNbOfRay = selectedWeapon.Data.visionConeRange * nbOfRayPerAngle;
		for(int i = 0; i< totalNbOfRay; i++)
		{
			//calculate angle
			float rayAngle = Mathf.LerpAngle(angle - (selectedWeapon.Data.visionConeRange / 2), angle + (selectedWeapon.Data.visionConeRange / 2), (float)i / (float)totalNbOfRay);
			rayAngle += 90f;
			//get position in at angle Y at distance X from linkedEntity
			if (rayAngle < 0)
				rayAngle += 360;

			float radians = rayAngle * Mathf.Deg2Rad;
			Vector3 aimedPosition = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
			RaycastHit[] hits = Physics.RaycastAll(m_linkedEntity.Displacement.Coordinates.GetTile().transform.position, aimedPosition * selectedWeapon.Data.range, selectedWeapon.Data.range * (2*Tile.innerRadius), GameConfig.current.input.tileInternRayCastLayer);
			foreach(RaycastHit hitInfo in hits)
			{
				if (hitInfo.transform.TryGetComponent(out Tile tile) && !tilesInRange.Contains(tile)
					&& GridManager.Instance.IsVisionLineClear(m_linkedEntity.Displacement.Coordinates.GetTile(), tile, _isThisTurn))
				{
					tilesInRange.Add(tile);
				}
			}
		}

		return tilesInRange;
	}

	public bool AttackRoll ( AttackAction _attackAction )
	{
		Entity targetEntity = _attackAction.TargetEntity;
		WeaponEquipmentData usedWeapon = m_weapons[_attackAction.attackingWeaponId].Data;

		int targetCamo = targetEntity.Data.FrameData.camo;
		int evationRatio = targetEntity.Data.FrameData.evasion;
		int coverRatio = GridManager.Instance.IsThereCoverBeween(_attackAction.PerformingEntity, targetEntity) ? GameConfig.current.game.entityCoverBonus : 0;
		int distanceRatio = m_weapons[_attackAction.attackingWeaponId].Data.distanceAccuracyBonus[GetWeaponDistanceTypeFrom(targetEntity, usedWeapon)];

		int targetEvasionScore = targetCamo + evationRatio + coverRatio + distanceRatio;

		int userPerception = m_linkedEntity.Data.BrainData.perception;
		int userAim = _attackAction.Data.type == EntityActionData.ActionType.DistanceAttack ? m_linkedEntity.Data.BrainData.accuracy : m_linkedEntity.Data.BrainData.agility;
		int flankBonus = GameConfig.current.game.entityFlankRatio[GridManager.Instance.GetHitTileSide(m_linkedEntity, targetEntity)];
		int modAction = m_linkedEntity.LastActionPerformedData.previousActionAttackModificator;

		int userHitScore = userPerception + userAim + flankBonus + modAction;
		bool isAttackSuccessful = targetEvasionScore < userHitScore;
		LogConsole.AddLog("Attack Roll " + (isAttackSuccessful ? "[SUCESS]" : "[FAILURE]") + " : targetEvasionScore = " + targetEvasionScore + " and userHitScore = " + userHitScore, LogConsole.LogEventType.PlayPhase);

		return targetEvasionScore < userHitScore;
	}

	public WeaponEquipmentData.DistanceType GetWeaponDistanceTypeFrom(Entity _target, WeaponEquipmentData _weaponData )
	{
		float actualDistanceFromTarget = Vector3.Distance(m_linkedEntity.transform.position, _target.transform.position) / (Tile.outerRadius*2f);
		float distanceRelativeToWeaponRangePercentage = actualDistanceFromTarget / (float)_weaponData.range;

		float currentTotal = 0;
		for(int i = 0; i < GameConfig.current.game.distanceTypeSpreadEvaluation.Keys.Count; i++)
		{
			if(distanceRelativeToWeaponRangePercentage < currentTotal + GameConfig.current.game.distanceTypeSpreadEvaluation[(WeaponEquipmentData.DistanceType)i])
				return (WeaponEquipmentData.DistanceType)i;

			currentTotal += GameConfig.current.game.distanceTypeSpreadEvaluation[(WeaponEquipmentData.DistanceType)i];
		}
		return WeaponEquipmentData.DistanceType.Long;
	}

	public bool EffectRoll(Entity _entity, AEntityEffect _effect )
	{
		bool isAttackSuccessful = Random.Range(0, 100) > _effect.hitProbability;

		//TODO
		//take possible build buff into acount

		return isAttackSuccessful;
	}

	#endregion

	#region Heatlh

	public void TakeDamage( TakeDamageCallback _damageInfo )
	{
		m_currentHealth -= _damageInfo.damage;

		if (m_currentHealth <= 0)
			Death();

		onHealthChangeDamage?.Invoke(_damageInfo);
	}

	private void Death ()
	{
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
