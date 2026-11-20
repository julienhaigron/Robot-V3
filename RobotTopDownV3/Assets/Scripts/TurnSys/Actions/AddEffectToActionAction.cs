using System;
using System.Collections.Generic;

public class AddEffectToAction : SpecialAction
{
	protected override bool DoesOwnItsCompletion => true;

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);
		EndTick();
	}

	public override void OnModActionAdded ( AEntityAction _mainAction )
	{
		base.OnModActionAdded(_mainAction);
		List<AEntityPassiveEffect.PassiveEffectContainer> newEffectList = new(_mainAction.effects ?? Array.Empty<AEntityPassiveEffect.PassiveEffectContainer>());
		newEffectList.AddRange(Data.passiveEffects);
		_mainAction.effects = newEffectList.ToArray();
	}
}
