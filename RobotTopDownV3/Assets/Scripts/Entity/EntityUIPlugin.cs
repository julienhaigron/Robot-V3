using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityUIPlugin : EntityPlugin
{
    [SerializeField] private HealthBar m_healthBar;


	private void Awake ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage += OnTakeDamage;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage -= OnTakeDamage;
	}

	public override void Init ()
	{
		base.Init();

		m_healthBar.SetHealth(m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);
	}

	private void OnTakeDamage(int _amount )
	{
		m_healthBar.SetHealth(m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);
	}
}
