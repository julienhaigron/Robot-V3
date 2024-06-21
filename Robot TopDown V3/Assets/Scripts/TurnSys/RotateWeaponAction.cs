using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateWeaponAction : AIAction
{
    public float _from;
    public Tile _aimedTile;
    public int _weaponId;
    public PlayerController _playerRobot;
    //public EnemyController _enemyRobot; 
    
    public RotateWeaponAction(Tile tileAimed, PlayerController robot, int weaponId)
    {
        _aimedTile = tileAimed;
        _playerRobot = robot;
        _weaponId = weaponId;
        _cost = 0;
    }

    public RotateWeaponAction(Tile tileAimed, /*EnemyController robot,*/ int weaponId)
    {
        _aimedTile = tileAimed;
        //_enemyRobot = robot;
        _weaponId = weaponId;
        _cost = 0;
    }

    public override void Perform()
    {
        SendAimToRobot();
    }

    public void SendAimToRobot()
    {
        /*if (_playerRobot != null)
            _playerRobot.SetWeaponAim(_aimedTile, _weaponId);
        else
            _enemyRobot.SetWeaponAim(_aimedTile, _weaponId);*/
    }
}
