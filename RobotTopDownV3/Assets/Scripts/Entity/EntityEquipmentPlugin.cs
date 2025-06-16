using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEquipmentPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityDeath;
	public System.Action<int> onDeath;
	public System.Action<int> onHealthChangeDamage;

    private Dictionary<string, Weapon> m_weapons = new();
	public Dictionary<string, Weapon> Weapons => m_weapons;
	[SerializeField] private Transform m_weaponParent;

	private int m_currentHealth;
	public int CurrentHealth => m_currentHealth;

	public int MaxHealth => m_linkedEntity.Data.maxHealth;

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

	public override void Init ()
	{
		//init weapon
		AddWeapon(GameAssets.current.game.defaultWeapon);

		//init health
		m_currentHealth = MaxHealth;
		m_isDead = false;
		base.Init();
	}

	#region Callbacks

	private void OnEntitySelected ()
	{
		foreach(Weapon weapon in m_weapons.Values)
		{
			weapon.ActivateActiveCone();
		}
	}

	private void OnEntityDeselected ()
	{
		foreach (Weapon weapon in m_weapons.Values)
		{
			weapon.ActivateUnactiveCone();
		}
	}

	#endregion

	#region Weapon

	private Weapon AddWeapon(WeaponData _data )
	{
		Weapon newWeapon = Instantiate(GameAssets.current.game.weapons[_data.saveKey], m_weaponParent);
		newWeapon.Init(_data);
		m_weapons.Add(_data.saveKey, newWeapon);

		return newWeapon;
	}


	public void AimAtTile(string _weaponID, Tile _tile, System.Action _onEndMovement = null )
	{
		//OLD : get angle and apply to cone
		Weapon selectedWeapon = m_weapons[_weaponID];
		Vector2Int currentLocation = new Vector2Int((int) m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.x, (int)m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.z);
		Vector2Int destination = new Vector2Int((int)_tile.transform.position.x, (int)_tile.transform.position.z);

		float angle = GridManager.Instance.GetAngleFrom(currentLocation, destination);
		selectedWeapon.AimAtAngle(angle);

		_onEndMovement?.Invoke();
	}

	public List<Tile> GetTilesInRange(string _weaponID)
	{
		List<Tile> tilesInRange = new();

		Weapon selectedWeapon = m_weapons[_weaponID];
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
			RaycastHit[] hits = Physics.RaycastAll(m_linkedEntity.Displacement.Coordinates.GetTile().transform.position, aimedPosition * selectedWeapon.Data.range, selectedWeapon.Data.range, GameConfig.current.input.tileInternRayCastLayer);
			foreach(RaycastHit hitInfo in hits)
			{
				if (hitInfo.transform.TryGetComponent(out Tile tile) && !tilesInRange.Contains(tile))
				{
					tilesInRange.Add(tile);
				}
			}
		}

		return tilesInRange;
	}

	public bool AttackRoll( Entity _targetEntity )
	{
		bool isAttackSuccessful = true;
		 

		//TODO
		//isAttackSuccessful = (_weaponAccuracy * _currentMoventAccuracyRatio) - _targetEntity.evasionPercent
		
		return isAttackSuccessful;
	}

	#endregion

	#region Heatlh

	public void TakeDamage(int _amount )
	{
		m_currentHealth -= _amount;

		if (m_currentHealth <= 0)
			Death();

		onHealthChangeDamage?.Invoke(_amount);
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
