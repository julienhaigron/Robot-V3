using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitAction : AEntityAction
{
	public override bool TileInteractPredicate ( Tile _tile )
	{
		return _tile == PlayerController.Instance.SelectedEntity.Displacement.Coordinates.GetTile();
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		//nothing to do
	}
	
	public override void Prepare ( Entity.EntityState _state )
	{

	}

	public override bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		return false;
	}


	public override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		EndPerform();
	}

	public override void EndPerform ()
	{
		base.EndPerform();

	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}


}
