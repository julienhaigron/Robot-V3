using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EntityUIPlugin : EntityPlugin
{
	[SerializeField] private Billboard m_billboard;
	[Title("InGame")]
    [SerializeField] private HealthBar m_healthBar;
	[SerializeField] private FlyingNumberManager m_flyingNumberManagerDamage;
	[SerializeField] private Transform m_statusDisplayParent;
	[SerializeField] private int m_statusPrefabSpawnAtInitCount;

	[Title("Hangar")]
	[SerializeField] private BaseButton m_modifyBtn;

	private List<EntityStatusDisplay> m_statusDisplays = new();

	private void Awake ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage += OnTakeDamage;
		m_linkedEntity.onStatusAdded += OnStatusAdded;
		m_linkedEntity.onStatusRemoved += OnStatusRemoved;
		m_modifyBtn.onClick += OnClickModifyBtn;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage -= OnTakeDamage;
		m_linkedEntity.onStatusAdded -= OnStatusAdded;
		m_linkedEntity.onStatusRemoved -= OnStatusRemoved;
		m_modifyBtn.onClick -= OnClickModifyBtn;
	}

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_modifyBtn.gameObject.SetActive(false);
		m_healthBar.gameObject.SetActive(true);
		m_healthBar.SetHealth(m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);

		for(int i = 0; i < m_statusPrefabSpawnAtInitCount; i++)
			AddStatusDisplay();

		m_billboard.SetRot();
	}

	public void InitHangarMode ( EntitySavedData _entityData )
	{
		m_healthBar.gameObject.SetActive(false);
		m_modifyBtn.gameObject.SetActive(true);

		m_billboard.SetRot();
	}

	public void HideUI ()
	{
		gameObject.SetActive(false);
	}

	private void OnTakeDamage( EntityEquipmentPlugin.TakeDamageCallback _damageInfo )
	{
		foreach(KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _damageInfo.damages)
		{
			if (_damageInfo.critical)
			{
				m_flyingNumberManagerDamage.config.fontAsset = GameAssets.current.ui.flyingDamageCritFontAsset;
				m_flyingNumberManagerDamage.ShowNumber(pair.Value, GameAssets.current.ui.critIcon, _iconScale: 0.4f);
			}
			else
			{
				m_flyingNumberManagerDamage.config.fontAsset = GameAssets.current.ui.flyingDamageFontAsset;
				m_flyingNumberManagerDamage.ShowNumber(pair.Value, false, false);
			}
		}

		m_healthBar.SetHealth(m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);

		if (m_linkedEntity.Equipment.CurrentHealth <= 0)
			HideUI();
	}

	private void OnClickModifyBtn ()
	{
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_linkedEntity.Data, false);
	}

	#region Status

	private void OnStatusAdded ( EntityStatusEnumID _statusID )
	{
		EntityStatusDisplay newStatusDisplay = GetUnactiveStatusDisplay();
		newStatusDisplay.SetStatus(_statusID);
	}

	private void OnStatusRemoved ( EntityStatusEnumID _statusID )
	{
		foreach(EntityStatusDisplay display in m_statusDisplays)
		{
			if (display.IsActive && display.StatusID == _statusID)
			{
				display.Hide();
				return;
			}
		}

		Debug.LogError("ERROR : no status display with ID " + _statusID + " found");
	}

	private EntityStatusDisplay AddStatusDisplay ()
	{
		EntityStatusDisplay statusDisplay = Instantiate(GameAssets.current.ui.statusDisplayPrefab);
		m_statusDisplays.Add(statusDisplay);
		statusDisplay.Hide();

		return statusDisplay;
	}

	private EntityStatusDisplay GetUnactiveStatusDisplay ()
	{
		foreach(EntityStatusDisplay display in m_statusDisplays)
		{
			if (!display.IsActive)
				return display;
		}

		return AddStatusDisplay();
	}

	#endregion

}
