using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Rooted", menuName = "ScriptableObject/Status/Rooted")]
public class RootedStatus : AEntityStatus
{

	public override void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
	{
		base.ApplyStatusEffect(_remainingDuration, _entity);
	}

	public override void PerformStatusEffectAtBeginingOfRound ( Tile _tile )
	{
		base.PerformStatusEffectAtBeginingOfRound(_tile);
		/*if (_tile.currentContent.entity != null)
			_tile.currentContent.entity.AddStatus(enumID);*/
	}
}
