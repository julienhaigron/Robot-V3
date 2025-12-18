using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityUIPlugin : EntityPlugin
{
    [SerializeField] private HealthBar m_healthBar;
	[SerializeField] private FlyingNumberManager m_flyingNumberManagerDamage;


	private void Awake ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage += OnTakeDamage;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage -= OnTakeDamage;
	}

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_healthBar.SetHealth(m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);
	}

	private void OnTakeDamage( EntityEquipmentPlugin.TakeDamageCallback _damageInfo )
	{
		if (_damageInfo.critical)
		{
			m_flyingNumberManagerDamage.config.fontAsset = GameAssets.current.ui.flyingDamageCritFontAsset;
			m_flyingNumberManagerDamage.ShowNumber(_damageInfo.damage, GameAssets.current.ui.critIcon, _iconScale: 0.4f);
		}
		else
		{
			m_flyingNumberManagerDamage.config.fontAsset = GameAssets.current.ui.flyingDamageFontAsset;
			m_flyingNumberManagerDamage.ShowNumber(_damageInfo.damage, false, false);
		}

		m_healthBar.SetHealth(m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);
	}
}
