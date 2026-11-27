using UnityEngine;

[CreateAssetMenu(fileName = "Horde", menuName = "ScriptableObject/PassiveEffect/Horde")]
public class HordePassiveEffect : AEntityPassiveEffect
{
	[Range(0f, 1f)] public float damageBonusPerDrone = .05f;

	public float GetDamageBonus ( Entity _entity )
	{
		int drones = 0;
		foreach (Entity ally in GameManager.Instance.PlayersEntityAnchor[_entity.OwnerID].Entities)
		{
			if (ally.IsInvocation && !ally.Equipment.IsDead && ally.IsAdjacentTo(_entity))
				drones++;
		}

		return drones * damageBonusPerDrone;
	}
}
