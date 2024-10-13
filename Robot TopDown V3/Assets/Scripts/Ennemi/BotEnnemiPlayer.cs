using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotEnnemiPlayer : Singleton<BotEnnemiPlayer>
{
    
    public void InputPhase ()
	{
		foreach(Entity entity in GameManager.Instance.EnnemiEntityAnchor.Entities)
		{
			DetermineEntityActions(entity);
		}
	}

	private void DetermineEntityActions (Entity entity)
	{
		TurnManager.Instance.AddAction(entity, EntityActionType.Wait);
	}


}
