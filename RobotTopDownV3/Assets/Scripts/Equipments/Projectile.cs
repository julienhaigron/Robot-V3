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

		if (!IsIntendedTarget(selector.LinkedWall))
		{
			StopWithoutHit();
			return;
		}

		Impact(selector.LinkedWall.LinkedTile);
	}
	private void FixedUpdate ()
	{
		if (!m_isInit || m_rb.isKinematic || !m_projectileData.HasValidTarget)
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

	private void Impact ( Tile _impactTile )
	{
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

		if (m_projectileData.targetType == ProjectileData.TargetType.Tile)
			return IsSameTile(_wall.LinkedTile, m_projectileData.targetTile);

		if (m_projectileData.targetType != ProjectileData.TargetType.Wall || m_projectileData.targetWall == null)
			return false;

		if (_wall == m_projectileData.targetWall)
			return true;

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

		if (m_projectileData.targetType == ProjectileData.TargetType.Tile && m_projectileData.targetTile != null)
			return _entity.Displacement.Coordinates.ID == m_projectileData.targetTile.coordinates.ID;

		return false;
	}

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

	public bool isAttackSuccessful;
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

	public bool HasValidTarget
	{
		get
		{
			switch (targetType)
			{
				case TargetType.Tile:
					return targetTile != null;
				case TargetType.Entity:
					return targetEntity != null && targetEntity.Skin != null;
				case TargetType.Wall:
					return targetWall != null;
			}

			return false;
		}
	}

	public Vector3 Destination
	{
		get
		{
			switch (targetType)
			{
				case TargetType.Tile:
					if (targetTile != null)
						return targetTile.coordinates.GetTile().transform.position;
					break;
				case TargetType.Entity:
					if (targetEntity != null && targetEntity.Skin != null)
						return targetEntity.Skin.Center.position;
					break;
				case TargetType.Wall:
					if (targetWall != null)
						return targetWall.Center;
					break;
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
