using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotEnnemiPlayer : MonoBehaviour
{

	private void Awake ()
	{
		TurnManager.onEndInputPhase += InputPhase;
	}

	private void OnDestroy ()
	{
		TurnManager.onEndInputPhase -= InputPhase;
	}

	public void InputPhase ()
	{
		if (!GameManager.Instance.IsOnline)
			return;

		foreach(Entity entity in GameManager.Instance.PlayersEntityAnchor[1].Entities)
		{
			DetermineEntityActions(entity);
		}
	}

	private void DetermineEntityActions (Entity entity)
	{
		for(int i = 0; i < entity.Data.actionTokenAmount; i++)
		{
			TurnManager.Instance.AddAction(entity.ID, EntityActionType.Wait, Entity.EntityState.Guarding);
		}

	}


}
