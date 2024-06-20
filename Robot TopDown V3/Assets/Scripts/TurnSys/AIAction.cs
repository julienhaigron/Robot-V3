using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIAction
{
    public int _cost;
    public abstract void Perform();

    public void EndPerform()
    {
        //GameManager.Instance.TurnManager.PerformNextRobotsAIAction();
    }

}
