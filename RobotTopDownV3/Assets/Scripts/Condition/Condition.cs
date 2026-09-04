using UnityEngine;

public class Condition
{
	public enum ConditionType 
	{ 
		Noone, 
		DidNotMoveThisTurn, 
		DidNotAttackThisTurn, 
		IsTargetMarked, 
		NoEnnemy8CellDistance, 
		Ennemy2Cell3Distance, 
		Traveled6Tiles, 
		IsInPreaparation, 
		IsInCooldown, 
		Cells12FromStart,
		DidNotUseThisGame
	}

	public static bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity, ConditionType _conditionType )
	{
		if (_action == null || _entity == null)
			return _conditionType == ConditionType.Noone;

		bool isLive = TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording;

		switch (_conditionType)
		{
			default:
			case ConditionType.Noone:
				return true;
			case ConditionType.DidNotMoveThisTurn:
				bool recordedCheck = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved >= _action.timeAtStart;
				bool liveCheck = !_entity.Displacement.DidMoveThisTurn;
				return isLive ? liveCheck : recordedCheck;
			case ConditionType.DidNotAttackThisTurn:
				bool recordedCheck2 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked >= _action.timeAtStart;
				bool liveCheck2 = !_entity.Equipment.DidAttackThisTurn;
				return isLive ? liveCheck2 : recordedCheck2;
			case ConditionType.IsTargetMarked:
				return _targetEntity != null && _targetEntity.Status.Contains(EntityStatusEnumID.Marked);
			/*case ConditionType.NoEnnemy8CellDistance:
				return _entity
			case ConditionType.Ennemy2Cell3Distance:*/
			case ConditionType.Traveled6Tiles:
				bool recordCheck3 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].traveledTileCountThisTurn >= 6;
				bool liveCheck3 = _entity.Displacement.TraveledTileCountThisTurn >= 6;
				return isLive ? liveCheck3 : recordCheck3;
			case ConditionType.IsInPreaparation:
				return _action.lifetime < _action.preparationDuration;
			case ConditionType.Cells12FromStart:
				bool recordCheck4 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].traveledTileCountThisTurn >= 12;
				bool liveCheck4 = _entity.Displacement.TraveledTileTotalCount >= 12;
				return isLive ? liveCheck4 : recordCheck4;
			case ConditionType.DidNotUseThisGame:
				bool liveCheck5 = !TurnManager.Instance.DidUseActionThisGame(_entity.ID, _action.enumID);
				bool recordCheck5 = liveCheck5 && !TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].usedActionThisTurn.Contains(_action.enumID);
				return isLive ? liveCheck5 : recordCheck5;
		}
	}
}
