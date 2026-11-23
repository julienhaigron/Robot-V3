using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PushOrPull", menuName = "ScriptableObject/PassiveEffect/PushOrPull")]
public class PushOrPullPassiveEffect : AEntityPassiveEffect
{
	private const int HexDirectionCount = 6;

	public int movementStrength = 0;

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		ApplyEffect(_entity, _targetEntity, _effectContainer, null);
	}

	public struct PushPrediction
	{
		public Tile destination;
		public Entity blockingEntity;
		public Tile blockingTile;
		public bool didCollide;
		public bool fallsIntoVoid;
	}

	public PushPrediction PredictPush ( Entity _targetEntity, Tile _originTile, Tile _casterTile )
	{
		Tile origin = _targetEntity.Displacement.Coordinates.GetTile();
		PushPrediction prediction = new() { destination = origin };

		Tile from = _originTile == null || _originTile == origin ? _casterTile : _originTile;
		if (origin == null || from == null || from == origin)
			return prediction;

		int direction = GridManager.Instance.GetClosestOrientation(origin, from);
		if (movementStrength > 0)
			direction = (direction + 3) % HexDirectionCount;

		int stepAmount = Mathf.Abs(movementStrength);
		for (int i = 0; i < stepAmount; i++)
		{
			Tile nextTile = prediction.destination.Neighbors[direction];
			bool isVoid = nextTile != null && nextTile.GroundType == TileGroundType.Void;

			if (nextTile == null || (!isVoid && nextTile.IsObstacle(false)))
			{
				prediction.blockingTile = nextTile;
				prediction.didCollide = true;
				break;
			}

			Entity occupant = nextTile.GetBlockingEntityFor(_targetEntity);
			if (occupant != null)
			{
				prediction.blockingEntity = occupant;
				prediction.didCollide = true;
				break;
			}

			prediction.destination = nextTile;

			if (isVoid && !_targetEntity.Status.Contains(EntityStatusEnumID.Flying))
			{
				prediction.fallsIntoVoid = true;
				break;
			}
		}

		return prediction;
	}

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer, Tile _originTile )
	{
		Tile origin = _targetEntity.Displacement.Coordinates.GetTile();
		PushPrediction push = PredictPush(_targetEntity, _originTile, _entity.Displacement.Coordinates.GetTile());
		Tile destination = push.destination;

		if (push.didCollide)
			ApplyCollisionDamage(_targetEntity, push.blockingEntity, push.blockingTile);

		if (destination != origin && !_targetEntity.Equipment.IsDead)
		{
			TurnManager.RecordedEvent movementEvent = new((movementStrength > 0 ? "Push " : "Pull ") + _targetEntity.Data.name
				+ " (" + this.GetLocalizedName() + ")");
			TurnManager.Instance.AddGameEvent(movementEvent);
			_targetEntity.Displacement.MoveToTile(destination.coordinates.ID, movementEvent.EndEvent, false, 0f, true);
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

		Dictionary<WeaponEquipmentData.DamageType, int> damages = _entity.Equipment.GetDamagesAfterResistances(BuildCollisionDamages(_damage));
		LogConsole.AddLog(_collisionDescription + " and takes " + damages[WeaponEquipmentData.DamageType.Bludgeoning] + " collision damage", LogConsole.LogEventType.Damage);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			entityAttacker = null,
			entityTargeted = _entity,
			damages = damages
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
