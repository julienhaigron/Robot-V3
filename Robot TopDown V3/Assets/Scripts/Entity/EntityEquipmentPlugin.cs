using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEquipmentPlugin : RobotPlugin
{
    private Dictionary<string, Weapon> m_weapons = new();
	[SerializeField] private Transform m_weaponParent;

	private void Awake ()
	{
		m_linkedEntity.onSelect += OnEntitySelected;
		m_linkedEntity.onDeselect += OnEntityDeselected;

		AddWeapon(GameAssets.current.game.defaultWeapon);
	}

	private void OnDestroy ()
	{
		m_linkedEntity.onSelect -= OnEntitySelected;
		m_linkedEntity.onDeselect -= OnEntityDeselected;
	}

	#region Callbacks

	private void OnEntitySelected ()
	{
		foreach(Weapon weapon in m_weapons.Values)
		{
			weapon.ActivateUnactiveCone();
		}
	}

	private void OnEntityDeselected ()
	{
		foreach (Weapon weapon in m_weapons.Values)
		{
			weapon.DisableAllCones();
		}
	}

	#endregion

	private Weapon AddWeapon(WeaponData _data )
	{
		Weapon newWeapon = Instantiate(GameAssets.current.game.weapons[_data.saveKey], m_weaponParent);
		m_weapons.Add(_data.saveKey, newWeapon);
		newWeapon.Init(_data);

		return newWeapon;
	}


	public void AimAtTile(string _weaponID, Tile _tile )
	{
		//OLD : get angle and apply to cone
		Weapon selectedWeapon = m_weapons[_weaponID];
		Vector3 oldRotation = selectedWeapon.transform.localRotation.eulerAngles;
		Vector2Int currentLocation = new Vector2Int((int) m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.x, (int)m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.z);
		Vector2Int destination = new Vector2Int((int)_tile.transform.position.x, (int)_tile.transform.position.z);

		float angle = GridManager.Instance.GetAngleFrom(currentLocation, destination);
		selectedWeapon.aimedRotation = angle;
		//Debug.Log("Rot : " + angle);
		selectedWeapon.transform.localRotation = Quaternion.Euler(0, angle, 0);

		//new: look at tile
	}

	public List<Tile> GetTilesInRange(string _weaponID)
	{
		List<Tile> tilesInRange = new();

		Weapon selectedWeapon = m_weapons[_weaponID];
		//shoot ray from tile to other tiles in range
		float angle = selectedWeapon.aimedRotation;

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

	private void OnDrawGizmos ()
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
	}
}
