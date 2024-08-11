using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitAction : EntityAction
{

	

	public override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		EndPerform();
	}

	public override void EndPerform ()
	{
		base.EndPerform();

	}
}
