using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : AIAction
{
    public Tile _nextTile;
    public PlayerController _playerRobot;
    public EnemyController _enemyRobot;
    
    public MoveAction(Tile nextTile, PlayerController robot)
    {
        _nextTile = nextTile;
        _playerRobot = robot;
        _cost = 1;
    }


    public MoveAction(Tile nextTile, EnemyController robot)
    {
        _nextTile = nextTile;
        _enemyRobot = robot;
        _cost = 1;
    }

    public override void Perform()
    {
        SendPathToRobot();
    }

    public void SendPathToRobot()
    {
        if (_playerRobot != null)
            _playerRobot.SetDestination(_nextTile);
        else
            _enemyRobot.SetDestination(_nextTile);
    }
}
