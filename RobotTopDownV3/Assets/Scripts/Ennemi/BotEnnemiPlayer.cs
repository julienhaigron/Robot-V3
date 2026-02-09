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
		for(int i = 0; i < GameConfig.current.game.actionTokenPerRound; i++)
		{
			TurnManager.Instance.AddAction(entity.ID, EntityActionEnumID.Wait, Entity.EntityState.Guarding);
		}

	}


}
