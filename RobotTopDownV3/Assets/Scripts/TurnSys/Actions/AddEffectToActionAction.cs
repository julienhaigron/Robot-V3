using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class AddEffectToAction : SpecialAction
{

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?
		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);
		EndTick();
	}

	public override void OnModActionAdded ( AEntityAction _mainAction )
	{
		base.OnModActionAdded(_mainAction);
		List<AEntityPassiveEffect.PassiveEffectContainer> newEffectList = _mainAction.effects.ToList();
		newEffectList.AddRange(Data.passiveEffects);
		_mainAction.effects = newEffectList.ToArray();
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
