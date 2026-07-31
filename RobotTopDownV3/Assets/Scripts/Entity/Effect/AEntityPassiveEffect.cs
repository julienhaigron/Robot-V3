using UnityEngine;
using Unity.Netcode;
using System;
using Sirenix.OdinInspector;

[Serializable]
public abstract class AEntityPassiveEffect : ScriptableEnum<EntityPassiveEffectEnumID>
{
	public string displayName;

	[Serializable]
	public struct PassiveEffectContainer : INetworkSerializable
	{
		public EntityPassiveEffectEnumID enumID;
		public Condition.ConditionType conditionType;
		public EntityActionData.TargetType targetType;
		public EntityActionData.AOEType aoeType;
		public EntityActionData.AOECenterType centerType;
		public Vector2Int effectRange;

		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref enumID);
			serializer.SerializeValue(ref conditionType);
			serializer.SerializeValue(ref targetType);
			serializer.SerializeValue(ref aoeType);
			serializer.SerializeValue(ref centerType);
			serializer.SerializeValue(ref effectRange);
		}

		public override string ToString ()
		{
			return enumID + (conditionType != Condition.ConditionType.Noone ? "if " + conditionType : "");
		}
	}

	public virtual void ApplyEffect ( Entity _performingEntity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
    {
		if (_effectContainer.conditionType == Condition.ConditionType.IsTargetMarked && _targetEntity.Status.Contains(EntityStatusEnumID.Marked))
			_targetEntity.RemoveStatus(EntityStatusEnumID.Marked);
    }

	public virtual void ApplyEffect ( Tile _tile )
	{

    }

	public virtual void OnDeathTrigger(Entity _deadEntity )
	{

	}
}