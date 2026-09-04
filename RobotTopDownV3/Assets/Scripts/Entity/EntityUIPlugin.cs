using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

public class EntityUIPlugin : EntityPlugin
{
	[SerializeField] private Billboard m_billboard;
	[Title("InGame")]
    [SerializeField] private HealthBar m_healthBar;
	[SerializeField, FormerlySerializedAs("m_flyingNumberManagerDamage")] private FlyingTextManager m_flyingTextManagerDamage;
	[SerializeField] private Color m_damageTextColor = Color.white;
	[SerializeField] private string m_missText = "Miss";
	[SerializeField] private Color m_missTextColor = Color.red;
	[SerializeField, FormerlySerializedAs("m_immortalText")] private string m_indestructibleText = "Indestructible";
	[SerializeField] private Color m_indestructibleTextColor = new(.65f, .2f, .95f);
	[SerializeField] private RectTransform m_statusDisplayParent;
	[SerializeField] private int m_statusPrefabSpawnAtInitCount;
	[SerializeField] private List<EntityStatusDisplay> m_statusDisplays;

	[Title("Hangar")]
	[SerializeField] private BaseButton m_modifyBtn;


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

		foreach (EntityStatusDisplay display in m_statusDisplays)
			display.Hide();

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

	public void ShowText ( string _text, Color? _colorOverride = null )
	{
		m_flyingTextManagerDamage.ShowText(_text, _colorOverride);
	}

	public void ShowMissText ()
	{
		ShowText(m_missText, m_missTextColor);
	}

	private void OnTakeDamage( EntityEquipmentPlugin.TakeDamageCallback _damageInfo )
	{
		if (m_linkedEntity.Data.FrameData != null && m_linkedEntity.Data.FrameData.isImmortal)
			ShowText(m_indestructibleText, m_indestructibleTextColor);
		else
		{
			foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _damageInfo.damages)
				m_flyingTextManagerDamage.ShowNumber(pair.Value, GameAssets.current.ui.damageIconPerType[pair.Key], false, false, 1f, m_damageTextColor);
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
