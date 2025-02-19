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
		for(int i = 0; i < entity.Data.actionTokenAmount; i++)
		{
			TurnManager.Instance.AddAction(entity, EntityActionType.Wait, Entity.EntityState.Guarding);
		}

	}


}
