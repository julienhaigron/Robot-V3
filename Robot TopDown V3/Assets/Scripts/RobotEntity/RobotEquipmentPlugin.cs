using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotEquipmentPlugin : RobotPlugin
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

		return newWeapon;
	}


	public void AimAtTile(string _weaponID, Tile _tile )
	{
		Weapon selectedWeapon = m_weapons[_weaponID];
		Vector3 oldRotation = selectedWeapon.transform.localRotation.eulerAngles;

		//Vector2Int currentLocation = _currentTile._location;
		/*Vector2Int currentLocation = new Vector2Int(m_linkedEntity.Displacement.Coordinates.X, m_linkedEntity.Displacement.Coordinates.Z);
		Vector2Int destination = new Vector2Int(_tile.coordinates.X, _tile.coordinates.Z);*/
		Vector2Int currentLocation = new Vector2Int((int) m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.x, (int)m_linkedEntity.Displacement.Coordinates.GetTile().transform.position.z);
		Vector2Int destination = new Vector2Int((int)_tile.transform.position.x, (int)_tile.transform.position.z);

		float angle = GridManager.Instance.GetAngleFrom(currentLocation, destination);
		Debug.Log("Rot : " + angle);
		//selectedWeapon.AimAtAngle(angle);
		
		/*Vector3 newRotationV3 = new Vector3(0, angle, 0);
		Quaternion newRotationQUAT = new Quaternion();
		newRotationQUAT.eulerAngles = newRotationV3;
		selectedWeapon.transform.localRotation = newRotationQUAT;*/
		//selectedWeapon.transform.localRotation = Quaternion.Euler(0, angle, 0);
		selectedWeapon.transform.localRotation = Quaternion.LookRotation(_tile.transform.position);

		//Debug.Log("new rot: " + selectedWeapon.transform.localRotation.eulerAngles);
	}
}
