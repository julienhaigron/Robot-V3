using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WaitAction : AEntityAction
{

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return _tile == GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		TurnManager.Instance.RegisterAction(performingEntityID, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.CurrentStateTypeSelected);
		TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
	}
	
	public override void Prepare ( Entity.EntityState _state )
	{

	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}


	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndTick());
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}


}
