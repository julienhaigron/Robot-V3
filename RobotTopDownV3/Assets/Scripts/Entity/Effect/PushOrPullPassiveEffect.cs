using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PushOrPull", menuName = "ScriptableObject/PassiveEffect/PushOrPull")]
public class PushOrPullPassiveEffect : AEntityPassiveEffect
{
	private const int HexDirectionCount = 6;

	public int movementStrength = 0;

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		Tile origin = _targetEntity.Displacement.Coordinates.GetTile();
		int direction = GridManager.Instance.GetClosestOrientation(origin, _entity.Displacement.Coordinates.GetTile());
		if (movementStrength > 0)
			direction = (direction + 3) % HexDirectionCount;

		Tile destination = origin;
		Entity blockingEntity = null;
		Tile blockingTile = null;
		bool didCollide = false;
		int stepAmount = Mathf.Abs(movementStrength);

		for (int i = 0; i < stepAmount; i++)
		{
			Tile nextTile = destination.Neighbors[direction];

			if (nextTile == null || nextTile.IsObstacle(false))
			{
				blockingTile = nextTile;
				didCollide = true;
				break;
			}

			Entity occupant = nextTile.GetEntityAtEndOfTick();
			if (occupant != null && occupant != _targetEntity)
			{
				blockingEntity = occupant;
				didCollide = true;
				break;
			}

			destination = nextTile;
		}

		if (didCollide)
			ApplyCollisionDamage(_targetEntity, blockingEntity, blockingTile);

		if (destination != origin && !_targetEntity.Equipment.IsDead)
		{
			TurnManager.RecordedEvent movementEvent = new();
			TurnManager.Instance.AddGameEvent(movementEvent);
			_targetEntity.Displacement.MoveToTile(destination.coordinates.ID, movementEvent.EndEvent, false);
		}

		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}

	private void ApplyCollisionDamage ( Entity _pushedEntity, Entity _blockingEntity, Tile _blockingTile )
	{
		int collisionDamage = GameConfig.current.game.pushCollisionDamage;
		if (collisionDamage <= 0)
			return;

		TakeCollisionDamage(_pushedEntity, collisionDamage, _pushedEntity.Data.name
			+ (movementStrength > 0 ? " is pushed into " : " is pulled into ")
			+ GetObstacleName(_blockingEntity, _blockingTile));

		if (_blockingEntity != null)
			TakeCollisionDamage(_blockingEntity, collisionDamage, _blockingEntity.Data.name + " is rammed by " + _pushedEntity.Data.name);

		if (_blockingTile != null && _blockingTile.Wall != null && _blockingTile.Wall.Health > 0)
			_blockingTile.Wall.TakeDamage(BuildCollisionDamages(collisionDamage));
	}

	private string GetObstacleName ( Entity _blockingEntity, Tile _blockingTile )
	{
		if (_blockingEntity != null)
			return _blockingEntity.Data.name;

		if (_blockingTile == null)
			return "the edge of the grid";

		if (_blockingTile.Wall != null && _blockingTile.Wall.Health > 0)
			return "the wall on tile " + _blockingTile.coordinates.ID;

		return "tile " + _blockingTile.coordinates.ID;
	}

	private void TakeCollisionDamage ( Entity _entity, int _damage, string _collisionDescription )
	{
		if (_entity.Equipment.IsDead)
			return;

		LogConsole.AddLog(_collisionDescription + " and takes " + _damage + " collision damage", LogConsole.LogEventType.Damage);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			entityAttacker = null,
			entityTargeted = _entity,
			damages = BuildCollisionDamages(_damage)
		});
	}

	private Dictionary<WeaponEquipmentData.DamageType, int> BuildCollisionDamages ( int _damage )
	{
		return new Dictionary<WeaponEquipmentData.DamageType, int>()
		{
			{ WeaponEquipmentData.DamageType.Bludgeoning, _damage }
		};
	}
}
