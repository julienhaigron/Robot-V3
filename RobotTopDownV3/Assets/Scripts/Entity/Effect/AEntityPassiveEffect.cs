using UnityEngine;
using Unity.Netcode;
using System;
using Sirenix.OdinInspector;

[Serializable]
public abstract class AEntityPassiveEffect : ScriptableEnum<EntityPassiveEffectEnumID>
{
	[Serializable]
	public struct PassiveEffectContainer : INetworkSerializable
	{
		public EntityPassiveEffectEnumID enumID;
		public ConditionType conditionType;
		public TargetType targetType;
		[ShowIf("@targetType == TargetType.ConeOnSelf || targetType == TargetType.ConeOnTarget")]
		public int effectRange;

		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref enumID);
			serializer.SerializeValue(ref conditionType);
			serializer.SerializeValue(ref targetType);
			serializer.SerializeValue(ref effectRange);
		}
	}

	public enum TargetType { OtherEntity, Tile, ConeOnSelf, Self, ConeOnTarget}
	public enum ConditionType { Noone, DidNotMoveThisTurn, DidNotAttackThisTurn, IsTargetMarked }

	public virtual bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity, ConditionType _conditionType )
	{
		if (_action == null || _entity == null )
			return false;

		switch (_conditionType)
		{
			default:
			case ConditionType.Noone:
				return true;
			case ConditionType.DidNotMoveThisTurn:
				bool recordedCheck = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved >= _action.timeAtStart;
				bool liveCheck = !_entity.Displacement.DidMoveThisTurn;
				return liveCheck && recordedCheck;
			case ConditionType.DidNotAttackThisTurn:
				bool recordedCheck2 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked >= _action.timeAtStart;
				bool liveCheck2 = !_entity.Equipment.DidAttackThisTurn;
				return recordedCheck2 && liveCheck2;
			case ConditionType.IsTargetMarked:
				return _targetEntity != null && _targetEntity.Status.Contains(EntityStatusEnumID.Marked);
		}
	}

	public virtual void ApplyEffect ( Entity _performingEntity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
    {

    }

	public virtual void ApplyEffect ( Tile _tile )
	{

    }

	public virtual void OnDeathTrigger(Entity _deadEntity )
	{

	}
}