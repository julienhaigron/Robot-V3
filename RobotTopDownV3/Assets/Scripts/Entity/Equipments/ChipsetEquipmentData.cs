using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "ChipsetData", menuName = "ScriptableObject/Equipment/ChipsetData", order = 1)]
public class ChipsetEquipmentData : EntityEquipmentData
{
	[BoxGroup(GroupID = "Stat"), Parsing("ConditionalStat")]
	public ConditionalStatBonus[] statBonuses;


	[System.Serializable, ShowOdinSerializedPropertiesInInspector]
	public class ConditionalStatBonus
	{
		public AEntityPassiveEffect.ConditionType conditionType;

		public SecondaryStat bonus;

		public bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity )
		{
			if (_action == null || _entity == null)
				return false;

			switch (conditionType)
			{
				default:
				case AEntityPassiveEffect.ConditionType.Noone:
					return true;
				case AEntityPassiveEffect.ConditionType.DidNotMoveThisTurn:
					bool recordedCheck = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved == -1
						|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved >= _action.timeAtStart;
					bool liveCheck = !_entity.Displacement.DidMoveThisTurn;
					return liveCheck && recordedCheck;
				case AEntityPassiveEffect.ConditionType.DidNotAttackThisTurn:
					bool recordedCheck2 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked == -1
						|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked >= _action.timeAtStart;
					bool liveCheck2 = !_entity.Equipment.DidAttackThisTurn;
					return recordedCheck2 && liveCheck2;
				case AEntityPassiveEffect.ConditionType.IsTargetMarked:
					return _targetEntity != null && _targetEntity.Status.Contains(EntityStatusEnumID.Marked);
			}
		}

	}

	public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		foreach (ConditionalStatBonus bonus in statBonuses)
		{
			description.Add(bonus.bonus.GetDescription());
		}

		return description.ToArray();
	}
}
