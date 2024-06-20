using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    public GameObject _ghostPrefab;
    private GhostController _currentGhost;
    public GhostController CurrentGhost { get => _currentGhost; set => _currentGhost = value; }
    private List<Tuple<PlayerController, Queue<AIAction>>> _playerRobotsActions = new();
    private List<bool> _playerRobotsActionsFullyFinished = new(); //entier turn completion state
    private List<bool> _playerRobotsActionFinished = new(); //current action in turn

    private List<Tuple<EnemyController, Queue<AIAction>>> _enemyRobotsActions = new();
    private List<bool> _enemyRobotsActionsFullyFinished = new();
    private List<bool> _enemyRobotsActionFinished = new();

    public List<PlayerController> Players;
    private PlayerController _currentSelectedPlayer;
    public PlayerController CurrentSelectedPlayer { get => _currentSelectedPlayer; set => _currentSelectedPlayer = value; }

    public List<EnemyController> Enemys;

    private List<Tile> _currentPath = new();
    public List<Tile> CurrentPath { get => _currentPath; set => _currentPath = value; }

    private TurnState _currentTurnState;
    public TurnState CurrentTurnState { get => _currentTurnState; set => _currentTurnState = value; }
    public enum TurnState
    {
        RecordingPlayerActions,
        PerformingAllRobotsActions,
    }

    public void Init()
    {
        //init players action queue
        _playerRobotsActions = new List<Tuple<PlayerController, Queue<AIAction>>>();
        _playerRobotsActionFinished = new List<bool>();
        _playerRobotsActionsFullyFinished = new List<bool>();

        foreach (PlayerController playerRobot in Players)
        {
            _playerRobotsActions.Add(new Tuple<PlayerController, Queue<AIAction>>(playerRobot, new Queue<AIAction>()));
            _playerRobotsActionFinished.Add(false);
            _playerRobotsActionsFullyFinished.Add(false);
        }

        //init enemys action queue
        _enemyRobotsActions = new List<Tuple<EnemyController, Queue<AIAction>>>();
        _enemyRobotsActionFinished = new List<bool>();
        _enemyRobotsActionsFullyFinished = new List<bool>();

        foreach (EnemyController enemyRobot in Enemys)
        {
            _enemyRobotsActions.Add(new Tuple<EnemyController, Queue<AIAction>>(enemyRobot, new Queue<AIAction>()));
            _enemyRobotsActionFinished.Add(false);
            _enemyRobotsActionsFullyFinished.Add(false);
        }
    }

    public void AddPlayerAIActionToQueue(PlayerController robot, AIAction action)
    {
        Tuple<PlayerController, Queue<AIAction>> robotsActionQueue = null;
        for (int i = 0; i < _playerRobotsActions.Count; i++)
        {
            if (_playerRobotsActions[i].Item1 == robot)
                robotsActionQueue = _playerRobotsActions[i];
        }

        if (robotsActionQueue == null)
        {
            Debug.LogError("didnt find robot in playersRobotsActions tuple list");
            return;
        }

        robotsActionQueue.Item2.Enqueue(action);

        //pay action cost
        //Debug.Log("action cost : " + action._cost);
        CurrentSelectedPlayer.RemainingActionPoints -= action._cost;
        CurrentSelectedPlayer._actionPointText.SetText(CurrentSelectedPlayer.RemainingActionPoints.ToString());

        //start perform player turn if all action used
        if (CurrentSelectedPlayer.RemainingActionPoints <= 0)
        {
            bool allPlayersTurnActionDefined = true;
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].RemainingActionPoints > 0)
                    allPlayersTurnActionDefined = false;
            }

            if(allPlayersTurnActionDefined)
                StartPerformRobotsAIActions();
        }
    }

    public void AddEnemyAIActionToQueue(EnemyController robot, AIAction action)
    {
        Tuple<EnemyController, Queue<AIAction>> robotsActionQueue = null;
        for (int i = 0; i < _enemyRobotsActions.Count; i++)
        {
            if (_enemyRobotsActions[i].Item1 == robot)
                robotsActionQueue = _enemyRobotsActions[i];
        }

        if (robotsActionQueue == null)
        {
            Debug.LogError("didnt find robot in playersRobotsActions tuple list");
            return;
        }

        robotsActionQueue.Item2.Enqueue(action);

        //pay action cost
        //Debug.Log("action cost : " + action._cost);
        CurrentSelectedPlayer.RemainingActionPoints -= action._cost;
        CurrentSelectedPlayer._actionPointText.SetText(CurrentSelectedPlayer.RemainingActionPoints.ToString());
    }

    public void StartPerformRobotsAIActions()
    {
        if (_currentTurnState != TurnState.PerformingAllRobotsActions)
        {
            //depop ghost
            if (CurrentGhost != null)
                CurrentGhost.DepopGhost();

            _currentSelectedPlayer.DeactivateGhost();
            _currentSelectedPlayer.CurrentSelectionState = PlayerController.RobotSelectionState.Unselected;
            GameManager.Instance.GridManager.DeactivateMovementCellSprite();

            _currentTurnState = TurnState.PerformingAllRobotsActions;

            //start perform all players robots first actions
            for (int i = 0; i < _playerRobotsActions.Count; i++)
            {
                AIAction firstAction = _playerRobotsActions[i].Item2.Dequeue();
                firstAction.Perform();
            }

            //start perform all enemy robots first actions
            for (int i = 0; i < _enemyRobotsActions.Count; i++)
            {
                AIAction firstAction = _enemyRobotsActions[i].Item2.Dequeue();
                firstAction.Perform();
            }
        }
        else
        {
            Debug.LogError("Error : AIActionQueueManager is already active");
        }
    }

    public void PlayerRobotPerformedActionCallback(PlayerController robot)
    {
        //
        for (int i = 0; i < _playerRobotsActions.Count; i++)
        {
            if (_playerRobotsActions[i].Item1 == robot)
                _playerRobotsActionFinished[i] = true;
        }

        //check if all robots endend performing their actions
        bool allPlayerActionPerformed = true;
        for (int i = 0; i < _playerRobotsActionFinished.Count; i++)
        {
            if(_playerRobotsActionFinished[i] == false)
                allPlayerActionPerformed = false;
        }

        //check if all robots endend performing their actions
        bool allEnemyActionPerformed = true;
        for (int i = 0; i < _enemyRobotsActionFinished.Count; i++)
        {
            if (_enemyRobotsActionFinished[i] == false)
                allEnemyActionPerformed = false;
        }

        if (allPlayerActionPerformed && allEnemyActionPerformed)
            PerformNextRobotsAIAction();
    }

    public void EnemyRobotPerformedActionCallback(EnemyController robot)
    {
        //
        for (int i = 0; i < _enemyRobotsActions.Count; i++)
        {
            if (_enemyRobotsActions[i].Item1 == robot)
                _enemyRobotsActionFinished[i] = true;
        }

        //check if all robots endend performing their actions
        bool allPlayerActionPerformed = true;
        for (int i = 0; i < _playerRobotsActionFinished.Count; i++)
        {
            if (_playerRobotsActionFinished[i] == false)
                allPlayerActionPerformed = false;
        }

        //check if all robots endend performing their actions
        bool allEnemyActionPerformed = true;
        for (int i = 0; i < _enemyRobotsActionFinished.Count; i++)
        {
            if (_enemyRobotsActionFinished[i] == false)
                allEnemyActionPerformed = false;
        }

        if (allPlayerActionPerformed && allEnemyActionPerformed)
        {
            //cleanup
            for( int i = 0; i< _playerRobotsActionFinished.Count; i++)
            {
                _playerRobotsActionFinished[i] = false;
            }
            for (int i = 0; i < _enemyRobotsActionFinished.Count; i++)
            {
                _enemyRobotsActionFinished[i] = false;
            }

            //next action
            PerformNextRobotsAIAction();
        }
    }

    public void PerformNextRobotsAIAction()
    {
        for (int i = 0; i < _playerRobotsActions.Count; i++)
        {
            if (_playerRobotsActions[i].Item2.Count > 0)
            {
                AIAction action = _playerRobotsActions[i].Item2.Dequeue();
                action.Perform();
            }
            else
            {
                _playerRobotsActionsFullyFinished[i] = true;
            }
        }
        for (int i = 0; i < _enemyRobotsActions.Count; i++)
        {
            if (_enemyRobotsActions[i].Item2.Count > 0)
            {
                AIAction action = _enemyRobotsActions[i].Item2.Dequeue();
                action.Perform();
            }
            else
            {
                _enemyRobotsActionsFullyFinished[i] = true;
            }
        }

        //check if all actions perfomed
        bool allPlayerRobotsFiniedActions = true;
        for (int i = 0; i < _playerRobotsActionsFullyFinished.Count; i++)
        {
            if (_playerRobotsActionsFullyFinished[i] == false)
                allPlayerRobotsFiniedActions = false;
        }
        bool allEnemyRobotsFiniedActions = true;
        for (int i = 0; i < _enemyRobotsActionsFullyFinished.Count; i++)
        {
            if (_enemyRobotsActionsFullyFinished[i] == false)
                allEnemyRobotsFiniedActions = false;
        }

        if (allPlayerRobotsFiniedActions && allEnemyRobotsFiniedActions)
        {
            //cleanup
            for (int i = 0; i < _playerRobotsActionsFullyFinished.Count; i++)
            {
                _playerRobotsActionsFullyFinished[i] = false;
                Players[i].NewTurn();
            }
            for (int i = 0; i < _enemyRobotsActionsFullyFinished.Count; i++)
            {
                _enemyRobotsActionsFullyFinished[i] = false;
            }

            //no more actions. back to action selection state
            _currentTurnState = TurnState.RecordingPlayerActions;
            GameManager.Instance.GridManager.DeactivateMovementCellSprite();
            GameManager.Instance.TurnManager.CurrentSelectedPlayer.CurrentSelectionState = PlayerController.RobotSelectionState.Unselected;
        }
    }

    #region Actions

    public void AddMovementAction(PlayerController robot, Tile destination)
    {
        GameManager.Instance.GridManager.DeactivateMovementCellSprite();

        if (CurrentGhost == null)
            PopGhost(destination);
        else
        {
            //move ghost to destination
            CurrentGhost.transform.position = destination.transform.position + new Vector3(0, 1 / 2f, 0);
        }

        foreach (Tile tile in GameManager.Instance.TurnManager._currentPath)
        {
            if (tile != null)
            {
                MoveAction moveAction = new MoveAction(tile, CurrentSelectedPlayer);
                AddPlayerAIActionToQueue(robot, moveAction);
            }
        }
    }

    public void PopGhost(Tile ghostTile)
    {
        GameManager.Instance.TurnManager.CurrentSelectedPlayer.ActivateChost();

        Vector3 position = ghostTile.transform.position + new Vector3(0, 1 / 2f, 0);
        GameObject currentGhostGO = Instantiate(_ghostPrefab, position, Quaternion.identity);

        CurrentGhost = currentGhostGO.GetComponent<GhostController>();
        CurrentGhost._connectedPlayer = CurrentSelectedPlayer;
        CurrentGhost.CurrentTile = ghostTile;
        CurrentGhost.InitWeapons();
    }

    public void AddAttackIfPossibleAction(PlayerController robot, int weaponId)
    {
        GameManager.Instance.GridManager.DeactivateAttackCellSprite();
        GameManager.Instance.TurnManager.CurrentSelectedPlayer.ResetWeaponCones();
        GameManager.Instance.TurnManager.CurrentSelectedPlayer.CurrentRobotAction = PlayerController.RobotActions.Move;
        //GameManager.Instance.TurnManager.CurrentSelectedPlayer.InitWeapons();

        AttackIfPossibleAction action = new AttackIfPossibleAction(robot, weaponId);
        AddPlayerAIActionToQueue(robot, action);
    }

    public void AddRotateWeaponAction(Tile aimedTile, PlayerController robot, int weaponId)
    {
        if(GameManager.Instance.TurnManager.CurrentSelectedPlayer)
        //GameManager.Instance.TurnManager.CurrentSelectedPlayer._weaponsTarget[weaponId] = aimedTile;

        GameManager.Instance.GridManager.DeactivateAttackCellSprite();
        GameManager.Instance.GridManager.DeactivateDeadAttackCellSprite();
        GameManager.Instance.TurnManager.CurrentSelectedPlayer.ResetWeaponCones();
        GameManager.Instance.TurnManager.CurrentSelectedPlayer.CurrentRobotAction = PlayerController.RobotActions.Move;
        //GameManager.Instance.TurnManager.CurrentSelectedPlayer.InitWeapons();

        RotateWeaponAction action = new RotateWeaponAction(aimedTile, robot, weaponId);
        AddPlayerAIActionToQueue(robot, action);
    }

    #endregion

}
