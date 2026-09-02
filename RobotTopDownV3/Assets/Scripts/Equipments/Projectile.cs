using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class Projectile : PoolElement
{
	[SerializeField] protected Rigidbody m_rb;
	[SerializeField] protected TrailRenderer m_trail;
	[SerializeField] private ParticleSystem m_onHitPS;

	private Renderer[] m_renderers;

	protected ProjectileData m_projectileData;
	private bool m_isInit;
	private Action<Tile> m_onImpact;
	private float m_lastPlanarDistanceToDestination = float.MaxValue;
	private Action m_onDespawnNoEntityHit;
	private bool m_didHitSomething = false;

	private void Reset ()
	{
		m_rb = GetComponent<Rigidbody>();
	}

	public override void Init ( PoolData _pool )
	{
		base.Init(_pool);

		//cached once per pooled element, the visibility toggle then costs nothing per shot
		m_renderers = GetComponentsInChildren<Renderer>(true);

		if (m_trail != null)
			m_trail.emitting = false;
	}

	public override void OnStartUse ()
	{
		base.OnStartUse();

		if (m_trail != null)
			m_trail.emitting = true;
	}

	protected virtual void OnTriggerEnter ( Collider _other )
	{
		if (m_isInit == false || m_rb.isKinematic)
			return;

		if (_other.gameObject.layer != 6)
		{
			OnCollideWithOther(_other.gameObject.layer, _other);
			return;
		}

		Transform entityRoot = _other.transform.parent != null ? _other.transform.parent.parent : null;

		if (entityRoot != null && entityRoot.TryGetComponent(out Entity entity))
			OnCollideWithEntity(entity);
	}

	public virtual void OnCollideWithEntity ( Entity _entity )
	{
		if (_entity == m_projectileData.owner)
			return;

		//Whatever is not the target the attack was rolled against stops the projectile without any effect: a
		//missed shot must never hurt whoever happens to stand on its trajectory.
		if (!IsIntendedTarget(_entity))
		{
			StopWithoutHit();
			return;
		}

		Impact(_entity.Displacement.Coordinates.GetTile());
	}

	public virtual void OnCollideWithOther ( int _collidedLayer, Collider _other )
	{
		if (_collidedLayer != 12 || !_other.transform.TryGetComponent(out WallSelector selector) || selector.LinkedWall == null)
			return;

		//A wall only ever takes a projectile when EntityEquipmentPlugin.AttackRoll deliberately routed the shot
		//into the cover standing between shooter and target. Any other wall simply stops the bullet.
		if (!IsIntendedTarget(selector.LinkedWall))
		{
			StopWithoutHit();
			return;
		}

		Impact(selector.LinkedWall.LinkedTile);
	}

	//Universal fallback for every target type: a shot aimed at a place resolves on that place occupied or not,
	//and an entity hidden by the fog of war has its colliders disabled, so no collision can ever happen against
	//it. Whichever comes first, collision or closest approach, discards the projectile and the other never runs.
	private void FixedUpdate ()
	{
		if (!m_isInit || m_rb.isKinematic)
			return;

		Vector3 destination = m_projectileData.Destination;
		float planarDistance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z)
			, new Vector2(destination.x, destination.z));

		if (planarDistance > m_lastPlanarDistanceToDestination)
		{
			Impact(GetTargetTile());
			return;
		}

		m_lastPlanarDistanceToDestination = planarDistance;
	}

	private Tile GetTargetTile ()
	{
		switch (m_projectileData.targetType)
		{
			case ProjectileData.TargetType.Entity:
				return m_projectileData.targetEntity == null ? null : m_projectileData.targetEntity.Displacement.Coordinates.GetTile();

			case ProjectileData.TargetType.Wall:
				return m_projectileData.targetWall == null ? null : m_projectileData.targetWall.LinkedTile;

			default:
				return m_projectileData.targetTile;
		}
	}

	//Where the round actually landed. The attack resolves from there, so an area blast goes off around it.
	private void Impact ( Tile _impactTile )
	{
		//The wall AttackRoll designated takes the round, whichever collider actually stopped the projectile and
		//wherever the impact was detected: the hex ray the roll walks and the bullet trajectory do not always
		//meet the same wall first, and Wall.LinkedTile is not guaranteed to be wired on every prefab. A wall
		//merely standing on a tile an area attack was recentred onto is not concerned: there the blast resolves.
		if (m_projectileData.targetType == ProjectileData.TargetType.Wall && m_projectileData.targetWall != null
			&& m_projectileData.damages != null && m_projectileData.damages.Count > 0)
			m_projectileData.targetWall.TakeDamage(m_projectileData.damages);

		if (_impactTile != null && m_projectileData.isAttackSuccessful)
		{
			m_didHitSomething = true;
			m_onImpact?.Invoke(_impactTile);
		}

		PlayHitFeedbackAndDiscard();
	}

	private bool IsIntendedTarget ( Wall _wall )
	{
		if (_wall == null)
			return false;

		//an area attack recentred onto a wall tile goes off against that wall
		if (m_projectileData.targetType == ProjectileData.TargetType.Tile)
			return IsSameTile(_wall.LinkedTile, m_projectileData.targetTile);

		if (m_projectileData.targetType != ProjectileData.TargetType.Wall || m_projectileData.targetWall == null)
			return false;

		if (_wall == m_projectileData.targetWall)
			return true;

		//Also compared through the tile: the wall linked to the collider and the Tile.Wall the attack recorded
		//are not guaranteed to be the same instance, but a cover always sits on one known tile.
		return IsSameTile(_wall.LinkedTile, m_projectileData.targetWall.LinkedTile);
	}

	private static bool IsSameTile ( Tile _a, Tile _b )
	{
		return _a != null && _b != null && _a.coordinates.ID == _b.coordinates.ID;
	}

	private bool IsIntendedTarget ( Entity _entity )
	{
		if (!m_projectileData.isAttackSuccessful)
			return false;

		if (m_projectileData.targetType == ProjectileData.TargetType.Entity)
			return _entity == m_projectileData.targetEntity;

		//A shot aimed at a tile, which is how area attacks are fired, resolves on whoever stands on that tile.
		//Without this the whole Tile targeted path silently stopped dealing any damage at all.
		if (m_projectileData.targetType == ProjectileData.TargetType.Tile && m_projectileData.targetTile != null)
			return _entity.Displacement.Coordinates.ID == m_projectileData.targetTile.coordinates.ID;

		return false;
	}

	//Consumed on something it was not aiming at: the attack resolves on nobody, hence the null tile, and
	//m_didHitSomething stays false so Deactivate still fires m_onDespawnNoEntityHit and the attack ends. A cover
	//designated by AttackRoll still takes its round: whatever stopped the projectile, that shot was for it.
	private void StopWithoutHit ()
	{
		Impact(null);
	}

	private void PlayHitFeedbackAndDiscard ()
	{
		SoundManager.Instance.Play(m_projectileData.onHitSFXID);

		if (m_onHitPS != null)
		{
			m_onHitPS.Play();
			DiscardIn(m_onHitPS.main.duration);
		}
		else
			Discard();
	}

	protected virtual void SetProjectileData ( ProjectileData _projectileData )
	{
		m_projectileData = _projectileData;
		m_lastPlanarDistanceToDestination = float.MaxValue;

		//A shot fired by an enemy the player cannot see must not give its position away. Owner visibility is the
		//cheap proxy here: following the bullet tile by tile through the fog would cost far more per frame.
		//Entity.IsVisible only ever gets updated for non allied entities, hence the ownership check.
		bool isOwnerHidden = m_projectileData.owner != null
			&& !m_projectileData.owner.IsAlliedTo(GameManager.Instance.PlayerID)
			&& !m_projectileData.owner.IsVisible;

		SetVisualsVisible(!isOwnerHidden);

		m_isInit = true;
	}

	private void SetVisualsVisible ( bool _isVisible )
	{
		if (m_renderers == null)
			return;

		foreach (Renderer renderer in m_renderers)
		{
			if (renderer != null)
				renderer.enabled = _isVisible;
		}
	}

	public void SetProjectileDataAndLaunch ( ProjectileData _projectileData, Action<Tile> _onImpact, Action _onProjectileDespawn, bool _hasTrajectoryControl )
	{
		SetProjectileData(_projectileData);

		if (_hasTrajectoryControl)
		{
			LaunchMortar(_onImpact, _onProjectileDespawn);
			return;
		}

		switch (_projectileData.attackData.trajectoryType)
		{
			case EntityActionData.TrajectoryType.Direct:
				Launch(_onImpact, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Mortar:
				LaunchMortar(_onImpact, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Grenade:
				LaunchGrenade(_onImpact, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Throw:
				LaunchThrow(_onImpact, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Underground:
				LaunchUnderground(_onImpact, _onProjectileDespawn);
				break;
		}

	}

	#region Launch

	public virtual void Launch ( Action<Tile> _onImpact, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;
		m_rb.AddForce((transform.forward * m_projectileData.speed.x) + (transform.up * m_projectileData.speed.y), ForceMode.VelocityChange);
		m_onImpact = _onImpact;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchMortar ( Action<Tile> _onImpact, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 start = transform.position;
		Vector3 target = m_projectileData.Destination;

		float gravity = Mathf.Abs(Physics.gravity.y);
		float verticalVelocity = m_projectileData.speed.y;
		Vector3 planarDisplacement = new Vector3( target.x - start.x, 0, target.z - start.z);
		float time = (verticalVelocity + Mathf.Sqrt( verticalVelocity * verticalVelocity + 2 * gravity * (start.y - target.y))) / gravity;
		Vector3 planarVelocity = planarDisplacement / time;

		m_rb.linearVelocity = planarVelocity + Vector3.up * verticalVelocity;
		m_onImpact = _onImpact;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchGrenade ( Action<Tile> _onImpact, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 start = transform.position;
		Vector3 target = m_projectileData.Destination;

		float gravity = Mathf.Abs(Physics.gravity.y);
		Vector3 displacement = target - start;
		Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
		float time = displacementXZ.magnitude / Mathf.Max(1f, m_projectileData.speed.x);
		Vector3 velocityXZ = displacementXZ / time;
		float velocityY = (displacement.y + 0.5f * gravity * time * time) / time;

		m_rb.linearVelocity = velocityXZ + Vector3.up * velocityY;
		m_onImpact = _onImpact;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchThrow ( Action<Tile> _onImpact, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 dir = (m_projectileData.Destination - transform.position).normalized;
		m_rb.linearVelocity = dir * m_projectileData.speed.x + Vector3.up * m_projectileData.speed.y;

		m_onImpact = _onImpact;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchUnderground ( Action<Tile> _onImpact, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = true;
		m_onImpact = _onImpact;
		m_onDespawnNoEntityHit = _onProjectileDespawn;

		Vector3 destination = m_projectileData.Destination;
		float duration = Vector3.Distance(transform.position, destination) / m_projectileData.speed.x;

		transform.LookAt(destination);
		//Impact rather than a plain discard: an underground round resolves where it surfaces, like any other.
		//Deactivate still fires m_onDespawnNoEntityHit when nothing was actually hit.
		transform.DOMove(destination, duration).SetEase(Ease.Linear).OnComplete(() => Impact(GetTargetTile()));
	}

	#endregion

	public void DiscardIn(float _sec )
	{
		Deactivate();

		DOVirtual.DelayedCall(_sec, Discard);
	}

	public override void Discard ()
	{
		Deactivate();

		base.Discard();
	}

	private void Deactivate ()
	{
		if (!m_didHitSomething)
			m_onDespawnNoEntityHit?.Invoke();
		m_onDespawnNoEntityHit = null;
		m_onImpact = null;

		if (!m_rb.isKinematic)
		{
			m_rb.linearVelocity = Vector3.zero;
			m_rb.angularVelocity = Vector3.zero;
			m_rb.isKinematic = true;
		}
		if (m_trail != null)
			m_trail.emitting = false;

		m_isInit = false;
	}
}

[Serializable]
public struct ProjectileData
{
	public Entity owner;
	public Vector2 speed;

	//A missed shot must not damage the target it was rolled against, even if the stray trajectory clips it.
	public bool isAttackSuccessful;

	//The weapon damage this round carries, used when it lands in a cover instead of its target.
	public Dictionary<WeaponEquipmentData.DamageType, int> damages;

	public EntityActionData attackData;
	public WeaponEquipmentData weapon;
	public SfxId onHitSFXID;
	
	public enum TargetType { Tile, Entity, Wall}
	public TargetType targetType;
	public Tile targetTile;
	public Entity targetEntity;
	public Wall targetWall;
	//public Vector3 destination;

	public Vector3 Destination
	{
		get
		{
			switch (targetType)
			{
				case TargetType.Tile:
					return targetTile.coordinates.GetTile().transform.position;
				case TargetType.Entity:
					return targetEntity.Skin.Center.position;
				case TargetType.Wall:
					return targetWall.Center;
			}

			Debug.LogError("Projectile target error, projectile wasnt initialized");
			return Vector3.zero;
		}
	}

	public void SetTarget( TargetType _targetType, Tile _targetTile = null, Entity _targetEntity = null, Wall _targetWall = null )
	{
		targetType = _targetType;
		targetTile = _targetTile;
		targetEntity = _targetEntity;
		targetWall = _targetWall;
	}
}
