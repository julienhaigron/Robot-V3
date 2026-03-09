using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Flying", menuName = "ScriptableObject/Status/Flying")]
public class FlyingStatus : AEntityStatus
{

	public override void ApplyStatusEffect ( Entity _entity )
	{
		base.ApplyStatusEffect(_entity);
	}

	public override void PerformStatusEffectAtBeginingOfRound ( Tile _tile )
	{
		base.PerformStatusEffectAtBeginingOfRound(_tile);
		/*if (_tile.currentContent.entity != null)
			_tile.currentContent.entity.AddStatus(enumID);*/
	}
}
