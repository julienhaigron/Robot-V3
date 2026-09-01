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
	private Action<Entity> m_onHitEntity;
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

		m_didHitSomething = true;

		m_onHitEntity?.Invoke(_entity);

		PlayHitFeedbackAndDiscard();
	}

	public virtual void OnCollideWithOther ( int _collidedLayer, Collider _other )
	{
		if (_collidedLayer != 12 || !_other.transform.TryGetComponent(out WallSelector selector) || selector.LinkedWall == null)
			return;

		//Only a wall the attack deliberately aimed at takes the shot, see EntityEquipmentPlugin.AttackRoll.
		if (!IsIntendedTarget(selector.LinkedWall))
		{
			StopWithoutHit();
			return;
		}

		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		damages.Add(WeaponEquipmentData.DamageType.Bludgeoning, 1);

		selector.LinkedWall.TakeDamage(damages);

		PlayHitFeedbackAndDiscard();
	}

	private bool IsIntendedTarget ( Entity _entity )
	{
		return m_projectileData.isAttackSuccessful
			&& m_projectileData.targetType == ProjectileData.TargetType.Entity
			&& _entity == m_projectileData.targetEntity;
	}

	private bool IsIntendedTarget ( Wall _wall )
	{
		return m_projectileData.targetType == ProjectileData.TargetType.Wall
			&& _wall == m_projectileData.targetWall;
	}

	//Consumed on something it was not aiming at: no damage. m_didHitSomething stays false on purpose so that
	//Deactivate still fires m_onDespawnNoEntityHit and the attack ends normally.
	private void StopWithoutHit ()
	{
		PlayHitFeedbackAndDiscard();
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

	public void SetProjectileDataAndLaunch ( ProjectileData _projectileData, Action<Entity> _onHitEntity, Action _onProjectileDespawn, bool _hasTrajectoryControl )
	{
		SetProjectileData(_projectileData);

		if (_hasTrajectoryControl)
		{
			LaunchMortar(_onHitEntity, _onProjectileDespawn);
			return;
		}

		switch (_projectileData.attackData.trajectoryType)
		{
			case EntityActionData.TrajectoryType.Direct:
				Launch(_onHitEntity, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Mortar:
				LaunchMortar(_onHitEntity, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Grenade:
				LaunchGrenade(_onHitEntity, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Throw:
				LaunchThrow(_onHitEntity, _onProjectileDespawn);
				break;

			case EntityActionData.TrajectoryType.Underground:
				LaunchUnderground(_onHitEntity, _onProjectileDespawn);
				break;
		}

	}

	#region Launch

	public virtual void Launch ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;
		m_rb.AddForce((transform.forward * m_projectileData.speed.x) + (transform.up * m_projectileData.speed.y), ForceMode.VelocityChange);
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchMortar ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
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
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchGrenade ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
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
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchThrow ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 dir = (m_projectileData.Destination - transform.position).normalized;
		m_rb.linearVelocity = dir * m_projectileData.speed.x + Vector3.up * m_projectileData.speed.y;

		m_onHitEntity = _onHitEntity;
		m_onDespawnNoEntityHit = _onProjectileDespawn;
	}

	private void LaunchUnderground ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = true;
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoEntityHit = _onProjectileDespawn;

		Vector3 destination = m_projectileData.Destination;
		float duration = Vector3.Distance(transform.position, destination) / m_projectileData.speed.x;

		transform.LookAt(destination);
		transform.DOMove(destination, duration).SetEase(Ease.Linear).OnComplete(() =>
		{
			m_onDespawnNoEntityHit?.Invoke();
			Discard();
		});
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
		m_onHitEntity = null;

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
