using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class EntityUIPlugin : EntityPlugin
{
	[SerializeField] private Billboard m_billboard;
	[Title("InGame")]
	[SerializeField] private HealthBar m_healthBar;
	[SerializeField] private ActionPhaseIndicator m_actionPhaseIndicator;
	[SerializeField, FormerlySerializedAs("m_flyingNumberManagerDamage")] private FlyingTextManager m_flyingTextManagerDamage;
	[SerializeField] private Color m_damageTextColor = Color.white;
	[SerializeField] private string m_missText = "Miss";
	[SerializeField] private Color m_missTextColor = Color.red;
	[SerializeField] private string m_dodgeText = "Esquive";
	[SerializeField] private Color m_dodgeTextColor = new(.3f, .8f, 1f);
	[SerializeField, FormerlySerializedAs("m_immortalText")] private string m_indestructibleText = "Indestructible";
	[SerializeField] private Color m_indestructibleTextColor = new(.65f, .2f, .95f);
	[SerializeField] private RectTransform m_statusDisplayParent;
	[SerializeField] private int m_statusPrefabSpawnAtInitCount;
	[SerializeField] private List<EntityStatusDisplay> m_statusDisplays;
	[SerializeField] private GameObject m_kingMarker;
	[SerializeField] private Image m_targetMarker;
	[SerializeField] private Sprite m_candidateTargetSprite;
	[SerializeField] private Sprite m_designatedTargetSprite;
	[SerializeField] private Color m_targetMarkerColor = new(1f, .66f, .23f);
	[SerializeField, Range(0f, 1f)] private float m_targetPreviewAlpha = .5f;

	public enum TargetMarkerState { None, Candidate, Preview, Designated }
	private TargetMarkerState m_targetMarkerState = TargetMarkerState.None;
	private bool m_isSelected;
	private bool m_isHovered;

	private void Awake ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage += OnTakeDamage;
		m_linkedEntity.onStatusAdded += OnStatusAdded;
		m_linkedEntity.onStatusRemoved += OnStatusRemoved;
		PlayerController.onEntitySelected += OnEntitySelected;
		PlayerController.onEntityHovered += OnEntityHovered;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.Equipment.onHealthChangeDamage -= OnTakeDamage;
		m_linkedEntity.onStatusAdded -= OnStatusAdded;
		m_linkedEntity.onStatusRemoved -= OnStatusRemoved;
		PlayerController.onEntitySelected -= OnEntitySelected;
		PlayerController.onEntityHovered -= OnEntityHovered;
	}

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_isSelected = false;
		m_isHovered = false;
		m_healthBar.gameObject.SetActive(true);
		m_healthBar.Init(m_linkedEntity.IsAlliedTo(GameManager.Instance.PlayerID), m_linkedEntity.Equipment.CurrentHealth, m_linkedEntity.Equipment.MaxHealth);
		m_actionPhaseIndicator.Init(m_linkedEntity);
		SetKingMarker(false);
		m_targetMarkerState = TargetMarkerState.None;
		ApplyTargetMarker();

		foreach (EntityStatusDisplay display in m_statusDisplays)
			display.Hide();

		for(int i = 0; i < m_statusPrefabSpawnAtInitCount; i++)
			AddStatusDisplay();

		m_billboard.SetRot();
	}

	public void InitHangarMode ( EntitySavedData _entityData )
	{
		m_healthBar.gameObject.SetActive(false);
		m_actionPhaseIndicator.HideImmediate();

		m_billboard.SetRot();
	}

	public void HideUI ()
	{
		gameObject.SetActive(false);
	}

	public void SetKingMarker ( bool _isKing )
	{
		if (m_kingMarker != null)
			m_kingMarker.SetActive(_isKing);
	}

	public void SetTargetMarker ( TargetMarkerState _state )
	{
		if (_state == m_targetMarkerState)
			return;

		m_targetMarkerState = _state;
		ApplyTargetMarker();
	}

	private void ApplyTargetMarker ()
	{
		if (m_targetMarker == null)
			return;

		m_targetMarker.gameObject.SetActive(m_targetMarkerState != TargetMarkerState.None);
		if (m_targetMarkerState == TargetMarkerState.None)
			return;

		m_targetMarker.sprite = m_targetMarkerState == TargetMarkerState.Candidate ? m_candidateTargetSprite : m_designatedTargetSprite;
		Color color = m_targetMarkerColor;
		if (m_targetMarkerState == TargetMarkerState.Preview)
			color.a *= m_targetPreviewAlpha;
		m_targetMarker.color = color;
	}

	public void ShowText ( string _text, Color? _colorOverride = null )
	{
		m_flyingTextManagerDamage.ShowText(_text, _colorOverride);
	}

	public void ShowMissText ()
	{
		ShowText(m_missText, m_missTextColor);
	}

	public void ShowDodgeText ()
	{
		ShowText(m_dodgeText, m_dodgeTextColor);
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

	private void OnEntitySelected ( int? _entityID )
	{
		m_isSelected = _entityID == m_linkedEntity.ID;
		RefreshHealthValue();
	}

	private void OnEntityHovered ( int? _entityID )
	{
		m_isHovered = _entityID == m_linkedEntity.ID;
		RefreshHealthValue();
	}

	private void RefreshHealthValue ()
	{
		m_healthBar.ShowValue(m_isSelected || m_isHovered);
	}

	#region Status

	public void RefreshStatusDurations ()
	{
		foreach (EntityStatusDisplay display in m_statusDisplays)
		{
			if (display.IsActive)
				display.SetDuration(m_linkedEntity.GetRemainingStatusDuration(display.StatusID));
		}
	}

	private void OnStatusAdded ( EntityStatusEnumID _statusID )
	{
		EntityStatusDisplay newStatusDisplay = GetUnactiveStatusDisplay();
		newStatusDisplay.SetStatus(_statusID);
		newStatusDisplay.SetDuration(m_linkedEntity.GetRemainingStatusDuration(_statusID));
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
		EntityStatusDisplay statusDisplay = Instantiate(GameAssets.current.ui.statusDisplayPrefab, m_statusDisplayParent);
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
