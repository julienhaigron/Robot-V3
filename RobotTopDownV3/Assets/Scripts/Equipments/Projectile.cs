using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class Projectile : PoolElement
{
	[SerializeField] protected Rigidbody m_rb;
	[SerializeField] protected TrailRenderer m_trail;
	[SerializeField] private ParticleSystem m_onHitPS;

	protected ProjectileData m_projectileData;
	private bool m_isInit;
	private Action<Entity> m_onHitEntity;
	private Action m_onDespawnNoHit;
	private bool m_didHitSomething = false;

	private void Reset ()
	{
		m_rb = GetComponent<Rigidbody>();
	}

	public override void Init ( PoolData _pool )
	{
		base.Init(_pool);

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

		_other.transform.parent.parent.TryGetComponent(out Entity entity);

		if (entity != null)
			OnCollideWithEntity(entity);
	}

	public virtual void OnCollideWithEntity ( Entity _entity )
	{
		if (_entity == m_projectileData.owner)
			return;

		m_didHitSomething = true;
		
		m_onHitEntity?.Invoke(_entity);

		SoundManager.Instance.Play(m_projectileData.onHitSFXID);
		if (m_onHitPS != null)
		{
			m_onHitPS.Play();
			DiscardIn(m_onHitPS.main.duration);
		}
		else
			Discard();
	}

	public virtual void OnCollideWithOther ( int _collidedLayer, Collider _other )
	{
		if(_collidedLayer == 12 
			&& _other.transform.TryGetComponent(out WallSelector selector) && selector.LinkedWall != null)
		{
			Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
			damages.Add(WeaponEquipmentData.DamageType.Contendant, 1);

			selector.LinkedWall.TakeDamage(damages);

			m_didHitSomething = true;

			SoundManager.Instance.Play(m_projectileData.onHitSFXID);
			if (m_onHitPS != null)
			{
				m_onHitPS.Play();
				DiscardIn(m_onHitPS.main.duration);
			}
			else
				Discard();
		}
	}

	public virtual void SetProjectileData ( ProjectileData _projectileData )
	{
		m_projectileData = _projectileData;

		m_isInit = true;
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
		m_onDespawnNoHit = _onProjectileDespawn;
	}

	private void LaunchMortar ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 start = transform.position;
		Vector3 target = m_projectileData.destination;

		float gravity = Mathf.Abs(Physics.gravity.y);

		Vector3 planarTarget = new Vector3(target.x, 0f, target.z);
		Vector3 planarPosition = new Vector3(start.x, 0f, start.z);

		float distance = Vector3.Distance(planarPosition, planarTarget);
		float maxHeight = Mathf.Max(start.y, target.y) + m_projectileData.speed.y;
		float verticalVelocity = Mathf.Sqrt(2f * gravity * (maxHeight - start.y));
		float timeUp = verticalVelocity / gravity;
		float timeDown = Mathf.Sqrt(2f * (maxHeight - target.y) / gravity);
		float totalTime = timeUp + timeDown;

		Vector3 planarVelocity = (planarTarget - planarPosition) / totalTime;
		Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;

		m_rb.linearVelocity = velocity;
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoHit = _onProjectileDespawn;
	}

	private void LaunchGrenade ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 start = transform.position;
		Vector3 target = m_projectileData.destination;

		float gravity = Mathf.Abs(Physics.gravity.y);
		Vector3 displacement = target - start;
		Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
		float time = displacementXZ.magnitude / Mathf.Max(1f, m_projectileData.speed.x);
		Vector3 velocityXZ = displacementXZ / time;
		float velocityY = (displacement.y + 0.5f * gravity * time * time) / time;

		m_rb.linearVelocity = velocityXZ + Vector3.up * velocityY;
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoHit = _onProjectileDespawn;
	}

	private void LaunchThrow ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = false;

		Vector3 dir = (m_projectileData.destination - transform.position).normalized;
		m_rb.linearVelocity = dir * m_projectileData.speed.x + Vector3.up * m_projectileData.speed.y;

		m_onHitEntity = _onHitEntity;
		m_onDespawnNoHit = _onProjectileDespawn;
	}

	private void LaunchUnderground ( Action<Entity> _onHitEntity, Action _onProjectileDespawn )
	{
		m_didHitSomething = false;
		m_rb.isKinematic = true;
		m_onHitEntity = _onHitEntity;
		m_onDespawnNoHit = _onProjectileDespawn;

		Vector3 destination = m_projectileData.destination;
		float duration = Vector3.Distance(transform.position, destination) / m_projectileData.speed.x;

		transform.LookAt(destination);
		transform.DOMove(destination, duration).SetEase(Ease.Linear).OnComplete(() =>
		{
			m_onDespawnNoHit?.Invoke();
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
			m_onDespawnNoHit?.Invoke();
		m_onDespawnNoHit = null;
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
	public Vector3 destination;
	public EntityActionData attackData;
	public WeaponEquipmentData weapon;
	public SfxId onHitSFXID;
}
