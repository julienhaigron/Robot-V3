using UnityEngine;
using System;
using System.Collections.Generic;

public class Projectile : PoolElement
{
	[SerializeField] protected Rigidbody m_rb;
	[SerializeField] protected TrailRenderer m_trail;

	protected ProjectileData m_projectileData;
	private bool m_isInit;

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

		if (_other.gameObject.layer != GameConfig.current.input.entityLayer.value)
		{
			OnCollideWithOther(_other.gameObject.layer, _other);
			return;
		}

		_other.TryGetComponent(out Entity entity);

		if (entity != null)
			OnCollideWithEntity(entity);
	}

	public virtual void OnCollideWithEntity ( Entity _entity )
	{
		if (_entity == m_projectileData.owner)
			return;

		Discard();
		/*if (_entity.TryGetModule(out DestructibleEM destructibleEM) && destructibleEM.DestroyBulletOnHit)
		{
			Discard();
		}*/
	}

	public virtual void OnCollideWithOther ( int _collidedLayer, Collider _other )
	{
		//spawn bullet impact
		//GameAssets.current.effects.punchLightFx.Get(transform.position).transform.localScale = Vector3.one * .5f;
		if(_collidedLayer == GameConfig.current.ui.wallLayerMask)
		{
			//TODO : not flat damage
			Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
			damages.Add(WeaponEquipmentData.DamageType.Contendant, 1);

			_other.GetComponent<WallSelector>().LinkedWall.TakeDamage(damages);
		}

	}

	public virtual void SetProjectileData ( ProjectileData _projectileData )
	{
		m_projectileData = _projectileData;

		m_isInit = true;
	}

	public virtual void Launch ()
	{
		m_rb.isKinematic = false;
		m_rb.AddForce((transform.forward * m_projectileData.speed.x) + (transform.up * m_projectileData.speed.y), ForceMode.VelocityChange);
	}

	public void SetProjectileDataAndLaunch ( ProjectileData _projectileData )
	{
		SetProjectileData(_projectileData);
		Launch();
	}

	public override void Discard ()
	{
		if (!m_rb.isKinematic)
		{
			m_rb.linearVelocity = Vector3.zero;
			m_rb.angularVelocity = Vector3.zero;
			m_rb.isKinematic = true;
		}
		if (m_trail != null)
			m_trail.emitting = false;

		m_isInit = false;
		base.Discard();
	}
}

[Serializable]
public struct ProjectileData
{
	public Entity owner;
	public Vector2 speed;
	public WeaponEquipmentData weapon;
	//public float gravityMultiplier;
}
