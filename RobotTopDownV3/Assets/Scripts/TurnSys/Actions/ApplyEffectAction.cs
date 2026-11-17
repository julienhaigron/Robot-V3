using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class ApplyEffectAction : SpecialAction
{

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?
		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
