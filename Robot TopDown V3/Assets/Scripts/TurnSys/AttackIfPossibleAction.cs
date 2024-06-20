using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackIfPossibleAction : AIAction
{
    public int _weaponId;
    public PlayerController _robot;
    
    public AttackIfPossibleAction(PlayerController robot, int weaponId)
    {
        _robot = robot;
        _weaponId = weaponId;
        _cost = 0;
    }

    public override void Perform()
    {
        _robot.AttackIfPossible(_weaponId);
    }
}
