using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;
using MPUIKIT;
using DG.Tweening;

public class EntityConfigPanel : AUIPanel
{

	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, ComponentSlot> m_mainComponentSlotDictionary;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, SubSlotContainer> m_subComponentSlotDictionary;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	[Title("Inventory")]
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	public ComponentDisplayGrid InventoryGrid => m_inventoryGrid;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, BaseButton> m_componentTypeFilterBtnDictionary = new();
	[SerializeField] private BaseButton m_allFilterTypeToggleBtn;
	[SerializeField] private GameObject m_filterCursorFO;
	[SerializeField] private TextMeshProUGUI m_currentFilterSelectedTMP;

	[Title("Unit")]
	[SerializeField] private TMP_InputField m_unitNameInputField;
	[SerializeField] private BaseButton m_renameBtn;
	[SerializeField] private BaseButton m_disassembleBtn;
	[SerializeField] private BaseButton m_createUnitBtn;
	[SerializeField] private TextMeshProUGUI m_unitCreationStatusTMP;
	[SerializeField] private Color m_unitReadyColor = new(0.47058827f, 0.9686275f, 0.28627452f, 1f);
	[SerializeField] private Color m_unitNotReadyColor = new(1f, 0.30980393f, 0.42745098f, 1f);
	[SerializeField] private ActionButton[] m_actionBtns;
	[SerializeField] private StatDisplay[] m_unitStatDisplays;
	[SerializeField] private Image m_dominentCorpoIcon;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_displayStaticStatsFilter;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_displayConditionalStatsFilter;
	[SerializeField] private float m_extendedWidth = 419f;
	[SerializeField] private float m_minimizedWidth = 279f;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, GameObject> m_mainComponentSectionPlusIconGOs = new();
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, MPImage> m_mainComponentSectionsBgsImgs = new();
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, GameObject> m_mainComponentSectionsBgsGos = new();
	[SerializeField] private int m_minimizedSubSlotCount = 3;
	[SerializeField] private float m_sectionResizeDuration = .2f;
	[SerializeField] private Ease m_sectionResizeEase = Ease.OutCubic;

	private readonly Dictionary<EntityEquipmentData.EquipmentType, PointerHoverRelay> m_sectionHoverRelays = new();
	private readonly Dictionary<EntityEquipmentData.EquipmentType, Tween> m_sectionResizeTweens = new();
	private readonly HashSet<EntityEquipmentData.EquipmentType> m_extendedSections = new();

	private EntityEquipmentData.EquipmentType? m_selectedFilterType;
	private EntitySavedData m_entityData;
	private bool m_doesComeFromMissionPanel;
	public bool DoesComeFromMissionPanel => m_doesComeFromMissionPanel;
	public bool IsNewUnit => m_isNewUnit;
	public bool IsCurrentUnitValid => m_entityData != null && m_entityData.IsUnitValid();
	private bool m_isNewUnit = false;

	private System.Func<GameDatas.PlayerSave.Component, bool> InventoryGridPredicate => item => item != null && item.TryGetData(out EntityEquipmentData _data)
		&& (m_selectedFilterType == null || _data.GetEquipmentType() == m_selectedFilterType);

	[System.Serializable]
	public class SubSlotContainer
	{
		public List<ComponentSlot> slots = new();
	}

	private void Awake ()
	{
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.Frame);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.Frame);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.Reactor);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.Reactor);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.Brain);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.Brain);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.NeuronalMembrane);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.NeuronalMembrane);

		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i];
			slot.onItemAdded += ( ComponentContainer container, ComponentDisplay display ) => OnItemAddedOnSubSlot(display, EntityEquipmentData.EquipmentType.Frame);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSubSlot(display, EntityEquipmentData.EquipmentType.Frame);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i];
			slot.onItemAdded += ( ComponentContainer container, ComponentDisplay display ) => OnItemAddedOnSubSlot(display, EntityEquipmentData.EquipmentType.Brain);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSubSlot(display, EntityEquipmentData.EquipmentType.Brain);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i];
			slot.onItemAdded += ( ComponentContainer container, ComponentDisplay display ) => OnItemAddedOnSubSlot(display, EntityEquipmentData.EquipmentType.NeuronalMembrane);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSubSlot(display, EntityEquipmentData.EquipmentType.NeuronalMembrane);
		}

		m_inventoryGrid.onItemAdded += ( container, item ) => { GameDatas.current.currentPlayerSave.AddEquipmentToInventory(item.SavedData); RefreshVisuals(); };
		m_inventoryGrid.onItemRemoved += ( container, item ) => { GameDatas.current.currentPlayerSave.RemoveEquipmentFromInventory(item.SavedData); RefreshVisuals(); };

		foreach (KeyValuePair<EntityEquipmentData.EquipmentType, BaseButton> pair in m_componentTypeFilterBtnDictionary)
			pair.Value.onClick = () => OnClickFilterType(pair.Key);
		m_allFilterTypeToggleBtn.onClick += OnClickAllFilterType;

		InitReleaseRules();

		foreach (KeyValuePair<EntityEquipmentData.EquipmentType, GameObject> pair in m_mainComponentSectionsBgsGos)
		{
			if (!pair.Value.TryGetComponent(out PointerHoverRelay relay))
				relay = pair.Value.AddComponent<PointerHoverRelay>();

			relay.onHoverChanged += _isHovered => RefreshSections(false);
			m_sectionHoverRelays[pair.Key] = relay;
		}

		m_unitNameInputField.onEndEdit.AddListener(( string s ) => OnInputFieldChange());
		m_renameBtn.onClick += OnClickRenameBtn;

		if (m_disassembleBtn != null)
			m_disassembleBtn.onClick += OnClickDisassembleBtn;
		m_createUnitBtn.onClick += OnClickCreateUnitBtn;
	}

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += OnLanguageChanged;
		RefreshFilterVisuals();
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged ()
	{
		RefreshFilterVisuals();
		RefreshUnitCreation();
	}

	private void OnDestroy ()
	{
		foreach (Tween tween in m_sectionResizeTweens.Values)
			tween?.Kill();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
	}

	protected override void OnHideStarted ()
	{
		base.OnHideStarted();

		OnInputFieldChange();
	}

	protected override void OnHideFinished ()
	{
		m_inventoryGrid.Cleanup();
		foreach (ComponentSlot slot in m_mainComponentSlotDictionary.Values)
			slot.Cleanup();

		foreach (SubSlotContainer slotContainer in m_subComponentSlotDictionary.Values)
			foreach (ComponentSlot slot in slotContainer.slots)
				slot.Cleanup();

		base.OnHideFinished();
	}

	private void OnClickDisassembleBtn ()
	{
		if (m_entityData == null)
			return;

		if (m_isNewUnit)
			DiscardUnit();
		else
			GameDatas.current.currentPlayerSave.DisassembleUnit(m_entityData);

		m_entityData = null;
		UIManager.Instance.OpenPanel<HangarPanel>();
	}

	private void OnClickCreateUnitBtn ()
	{
		if (m_isNewUnit && TryCommitUnit())
			UIManager.Instance.OpenPanel<HangarPanel>();
	}

	public bool TryCommitUnit ()
	{
		if (!IsCurrentUnitValid)
			return false;

		if (m_isNewUnit)
		{
			GameDatas.current.currentPlayerSave.AddNewUnit(m_entityData, false);
			m_isNewUnit = false;
		}

		return true;
	}

	public void DiscardUnit ()
	{
		if (m_entityData == null)
			return;

		if(m_isNewUnit)
			GameDatas.current.currentPlayerSave.DisassembleUnit(m_entityData);
		//else
			//GameDatas.current.currentPlayerSave.allBuiltUnits.Remove(m_entityData);

		m_isNewUnit = false;
	}

	public void InitNewUnit ()
	{
		EntitySavedData newUnit = new();
		newUnit.name = "New Unit";
		Init(newUnit, false);
		m_isNewUnit = true;
		RefreshUnitCreation();
		onConfigChanged?.Invoke();
	}

	private void RefreshUnitCreation ()
	{
		if (m_disassembleBtn != null)
			m_disassembleBtn.gameObject.SetActive(!m_isNewUnit);
		m_createUnitBtn.gameObject.SetActive(m_isNewUnit);
		m_unitCreationStatusTMP.gameObject.SetActive(m_isNewUnit);

		if (!m_isNewUnit || m_entityData == null)
			return;

		bool isValid = IsCurrentUnitValid;
		m_createUnitBtn.SetInteractability(isValid);
		m_unitCreationStatusTMP.color = isValid ? m_unitReadyColor : m_unitNotReadyColor;
		m_unitCreationStatusTMP.text = isValid ? LocalizationManager.Instance.Get(LocalizationKey.entity_config_unit_ready) : string.Join("\n", GetUnitCreationProblems());
	}

	private List<string> GetUnitCreationProblems ()
	{
		List<string> problems = new();
		AddMainComponentProblem(problems, m_entityData.frame, EntityEquipmentData.EquipmentType.Frame);
		AddMainComponentProblem(problems, m_entityData.reactor, EntityEquipmentData.EquipmentType.Reactor);
		AddMainComponentProblem(problems, m_entityData.neuronalMembrane, EntityEquipmentData.EquipmentType.NeuronalMembrane);
		AddMainComponentProblem(problems, m_entityData.brain, EntityEquipmentData.EquipmentType.Brain);

		if (problems.Count == 0 && m_entityData.ReactorData != null)
		{
			int energyUsed = m_entityData.GetTotalEnergyUsed();
			if (energyUsed > m_entityData.ReactorData.energyProduced)
				problems.Add(string.Format(LocalizationManager.Instance.Get(LocalizationKey.entity_config_energy_exceeded), energyUsed, m_entityData.ReactorData.energyProduced));
		}

		return problems;
	}

	private static void AddMainComponentProblem ( List<string> _problems, GameDatas.PlayerSave.Component _component, EntityEquipmentData.EquipmentType _type )
	{
		if (_component == null || !_component.TryGetData(out EntityEquipmentData _))
			_problems.Add(string.Format(LocalizationManager.Instance.Get(LocalizationKey.entity_config_missing_component), EntityEquipmentData.GetLocalizedTypeName(_type)));
		else if (_component.isDamaged)
			_problems.Add(string.Format(LocalizationManager.Instance.Get(LocalizationKey.entity_config_damaged_component), EntityEquipmentData.GetLocalizedTypeName(_type)));
	}

	public void Init ( EntitySavedData _entity, bool _doesComeFromMissionPanel )
	{
		m_isNewUnit = false;
		m_entityData = _entity;
		m_doesComeFromMissionPanel = _doesComeFromMissionPanel;
		m_unitNameInputField.text = _entity.name;

		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].Init(m_inventoryGrid, _entity, _entity.frame
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Frame, ComponentDisplay.DisplayMode.Unit);
		//m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Frame]);

		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].Init(m_inventoryGrid, _entity, _entity.brain
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Brain, ComponentDisplay.DisplayMode.Unit);
		//m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Brain]);

		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].Init(m_inventoryGrid, _entity, _entity.neuronalMembrane
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.NeuronalMembrane, ComponentDisplay.DisplayMode.Hangar);
		//m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.NeuronalMembrane]);

		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].Init(m_inventoryGrid, _entity, _entity.reactor
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Reactor, ComponentDisplay.DisplayMode.Unit);
		//m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Reactor]);

		InitSubSlots(null);

		m_inventoryGrid.Init(null, _entity, null, item => item != null, ComponentDisplay.DisplayMode.Hangar);
		/*foreach(ComponentDisplay display in m_inventoryGrid.Items)
		{
			if(display.SavedData != null && display.SavedData.TryGetData(out EntityEquipmentData data))
				display.SetOutlineColor(GameAssets.current.ui.componentColors[data.GetEquipmentType()]);
		}*/

		m_hoveredComponentFullInfoDisplay.Init(null);
		RefreshVisuals();
		RefreshSections(true);
	}

	private GameDatas.PlayerSave.Component GetFrameSubComponentAt ( int _slotIndex )
	{
		if (m_entityData == null || m_entityData.FrameData == null || m_entityData.auxiliar == null)
			return null;

		bool isArmourSlot = _slotIndex < m_entityData.FrameData.armouringSlotAvailable;
		EntityEquipmentData.EquipmentType wantedType = isArmourSlot ? EntityEquipmentData.EquipmentType.Armor : EntityEquipmentData.EquipmentType.Occultor;
		int wantedRank = isArmourSlot ? _slotIndex : _slotIndex - m_entityData.FrameData.armouringSlotAvailable;

		int rank = 0;
		foreach (GameDatas.PlayerSave.Component component in m_entityData.auxiliar)
		{
			if (component == null || !component.TryGetData(out EntityEquipmentData data) || data.GetEquipmentType() != wantedType)
				continue;

			if (rank == wantedRank)
				return component;

			rank++;
		}

		return null;
	}

	private static bool IsUndamaged ( ComponentDisplay _display )
	{
		return _display == null || _display.SavedData == null || !_display.SavedData.isDamaged;
	}

	private bool HasDamagedSubComponent ( EntityEquipmentData.EquipmentType _mainType )
	{
		GameDatas.PlayerSave.Component[] subComponents = null;
		switch (_mainType)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				subComponents = m_entityData == null ? null : m_entityData.auxiliar;
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				subComponents = m_entityData == null ? null : m_entityData.chipsets;
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				subComponents = m_entityData == null ? null : m_entityData.arms;
				break;
		}

		if (subComponents == null)
			return false;

		foreach (GameDatas.PlayerSave.Component component in subComponents)
			if (component != null && component.isDamaged)
				return true;

		return false;
	}

	private void InitReleaseRules ()
	{
		foreach (KeyValuePair<EntityEquipmentData.EquipmentType, ComponentSlot> pair in m_mainComponentSlotDictionary)
		{
			EntityEquipmentData.EquipmentType mainType = pair.Key;
			pair.Value.SetReleasePredicate(display => IsUndamaged(display) && !HasDamagedSubComponent(mainType));
		}

		foreach (SubSlotContainer slotContainer in m_subComponentSlotDictionary.Values)
			foreach (ComponentSlot slot in slotContainer.slots)
				slot.SetReleasePredicate(IsUndamaged);
	}

	private void InitSubSlots ( EntityEquipmentData.EquipmentType? _type )
	{
		foreach (KeyValuePair<EntityEquipmentData.EquipmentType, SubSlotContainer> pair in m_subComponentSlotDictionary)
			if (_type == null || pair.Key == _type.Value)
				foreach (ComponentSlot slot in pair.Value.slots)
					slot.Cleanup();

		if (_type == null || _type.Value == EntityEquipmentData.EquipmentType.Frame)
		{
			for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
			{
				if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable > i)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
					&& (type == EntityEquipmentData.EquipmentType.Armor), ComponentDisplay.DisplayMode.Unit);
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetSubComponentType(EntityEquipmentData.EquipmentType.Armor);

					//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment.isDamaged);
					//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Armor]);
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
				}
				else if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable + m_entityData.FrameData.occultorSlotAvailable > i)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
					&& (type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Unit);
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetSubComponentType(EntityEquipmentData.EquipmentType.Occultor);
					//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment.isDamaged);
					//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Occultor]);
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
				}
				else
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
					&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Unit);
					//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment.isDamaged);
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
				}
			}
		}
		if (_type == null || _type.Value == EntityEquipmentData.EquipmentType.Brain)
		{
			for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.chipsets != null && m_entityData.chipsets.Length > i
					? m_entityData.chipsets[i] : null,
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Unit);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].SetSubComponentType(EntityEquipmentData.EquipmentType.Chipset);

				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Chipset]);
				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Equipment.isDamaged);
				if (m_entityData.BrainData != null && m_entityData.BrainData.chipsetSlotAvailable > i)
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
				else
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
			}
		}
		if (_type == null || _type.Value == EntityEquipmentData.EquipmentType.NeuronalMembrane)
		{
			for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.arms != null && m_entityData.arms.Length > i
					? m_entityData.arms[i] : null,
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
					&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Unit);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].SetSubComponentType(EntityEquipmentData.EquipmentType.Weapon);

				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Weapon]);
				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Equipment.isDamaged);
				if (m_entityData.NeuronalMembraneData != null && m_entityData.NeuronalMembraneData.equipmentSlotAvailable > i)
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
				else
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
			}
		}
	}

	public ComponentContainer GetContainerFor ( ComponentDisplay _display )
	{
		if (_display == null || _display.SavedData == null)
			return null;

		foreach (ComponentSlot mainSlot in m_mainComponentSlotDictionary.Values)
		{
			if (mainSlot.gameObject.activeInHierarchy && mainSlot.IsValid(_display))
				return mainSlot;
		}

		ComponentSlot occupiedSlot = null;
		foreach (SubSlotContainer slotContainer in m_subComponentSlotDictionary.Values)
		{
			foreach (ComponentSlot slot in slotContainer.slots)
			{
				if (!slot.gameObject.activeInHierarchy || !slot.IsValid(_display))
					continue;

				if (slot.CurrentDisplay == null)
					return slot;

				if (occupiedSlot == null)
					occupiedSlot = slot;
			}
		}

		return occupiedSlot;
	}

	public static System.Action onConfigChanged;

	private void RefreshVisuals ()
	{
		onConfigChanged?.Invoke();
		RefreshUnitCreation();

		//actions
		List<EntityActionEnumID> actions = new();
		foreach (GameDatas.PlayerSave.Component equipmentData in m_entityData.GetAllEquipments())
		{
			if (equipmentData.TryGetData(out EntityEquipmentData data))
				actions.AddRange(data.knownedActions);
		}
		for (int i = 0; i < m_actionBtns.Length; i++)
		{
			if (actions.Count > i)
			{
				m_actionBtns[i].InitEntityConfigPanelMode(actions[i]);
				m_actionBtns[i].SetVisible(_isVisible: true, _isInstant: true);
			}
			else
				m_actionBtns[i].SetVisible(_isVisible: false, _isInstant: true);
		}

		//set unit stats
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDescriptions = m_entityData.GetStatsDesciptions();
		List<EntityEquipmentData.SecondaryStat.StatType> keys = GetDisplayedStats(statsDescriptions);
		for (int i = 0; i < m_unitStatDisplays.Length; i++)
		{
			if (keys.Count <= i)
				m_unitStatDisplays[i].gameObject.SetActive(false);
			else
			{
				m_unitStatDisplays[i].gameObject.SetActive(true);
				m_unitStatDisplays[i].Init(statsDescriptions[keys[i]]);
			}
		}

		//inventory
		//m_inventoryGrid.RefreshPredicate(InventoryGridPredicate);
		m_inventoryGrid.Cleanup();
		for (int i = 0; i < GameConfig.current.game.maxInventoryCapacity; i++)
		{
			if (GameDatas.current.currentPlayerSave.equipmentInventory.Count > i && InventoryGridPredicate(GameDatas.current.currentPlayerSave.equipmentInventory[i]))
				m_inventoryGrid.CreateNewDisplay(null, GameDatas.current.currentPlayerSave.equipmentInventory[i], ComponentDisplay.DisplayMode.Hangar);
			else
			{
				m_inventoryGrid.CreateNewDisplay(null, null, ComponentDisplay.DisplayMode.Empty);
			}
		}
		EntityEquipmentData.EntityFaction dominentCorpo = m_entityData.GetDominentFaction(out float percentage);
		m_dominentCorpoIcon.sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
		m_dominentCorpoIcon.color = GameAssets.current.ui.corporationsColors[dominentCorpo];

		RefreshSections(false);
	}

	private void RefreshSections ( bool _isInstant )
	{
		_isInstant |= !isActiveAndEnabled;

		foreach (EntityEquipmentData.EquipmentType type in m_mainComponentSectionsBgsGos.Keys)
		{
			bool isExtended = HasHiddenSubSlots(type) && m_sectionHoverRelays.TryGetValue(type, out PointerHoverRelay relay) && relay.IsHovered;

			if (_isInstant || m_extendedSections.Contains(type) != isExtended)
				ResizeSection(type, isExtended, _isInstant);

			RefreshSectionPlusIcon(type);
		}
	}

	private bool HasHiddenSubSlots ( EntityEquipmentData.EquipmentType _type )
	{
		return m_subComponentSlotDictionary.TryGetValue(_type, out SubSlotContainer slotContainer)
			&& slotContainer.slots.Count(slot => slot.gameObject.activeSelf) > m_minimizedSubSlotCount;
	}

	private void ResizeSection ( EntityEquipmentData.EquipmentType _type, bool _isExtended, bool _isInstant )
	{
		if (_isExtended)
			m_extendedSections.Add(_type);
		else
			m_extendedSections.Remove(_type);

		if (m_sectionResizeTweens.TryGetValue(_type, out Tween previousTween))
		{
			previousTween?.Kill();
			m_sectionResizeTweens.Remove(_type);
		}

		RectTransform section = m_mainComponentSectionsBgsGos[_type].transform as RectTransform;
		Vector2 size = new Vector2(_isExtended ? m_extendedWidth : m_minimizedWidth, section.sizeDelta.y);

		if (_isInstant)
		{
			section.sizeDelta = size;
			return;
		}

		m_sectionResizeTweens[_type] = section.DOSizeDelta(size, m_sectionResizeDuration)
			.SetEase(m_sectionResizeEase).SetUpdate(true)
			.OnComplete(() =>
			{
				m_sectionResizeTweens.Remove(_type);
				RefreshSectionPlusIcon(_type);
			});
	}

	private void RefreshSectionPlusIcon ( EntityEquipmentData.EquipmentType _type )
	{
		if (m_mainComponentSectionPlusIconGOs.TryGetValue(_type, out GameObject plusIcon))
			plusIcon.SetActive(!m_extendedSections.Contains(_type) && !m_sectionResizeTweens.ContainsKey(_type) && HasHiddenSubSlots(_type));
	}


	#region Callbacks

	private static List<GameDatas.PlayerSave.Component> ToSafeList ( GameDatas.PlayerSave.Component[] _slots )
	{
		return _slots == null ? new() : _slots.ToList();
	}

	private void OnItemAddedOnSubSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Component> newArray = ToSafeList(m_entityData.auxiliar);
				newArray.Add(_display.SavedData);
				m_entityData.auxiliar = newArray.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
				{
					if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor), ComponentDisplay.DisplayMode.Unit);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable + m_entityData.FrameData.occultorSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Unit);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Unit);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
					}
				}

				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Component> newArray2 = ToSafeList(m_entityData.chipsets);
				newArray2.Add(_display.SavedData);
				m_entityData.chipsets = newArray2.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.chipsets != null && m_entityData.chipsets.Length > i
						? m_entityData.chipsets[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Unit);
					if (m_entityData.BrainData != null && m_entityData.BrainData.chipsetSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
				}

				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Component> newArray3 = ToSafeList(m_entityData.arms);
				newArray3.Add(_display.SavedData);
				m_entityData.arms = newArray3.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.arms != null && m_entityData.arms.Length > i
						? m_entityData.arms[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Unit);
					if (m_entityData.NeuronalMembraneData != null && m_entityData.NeuronalMembraneData.equipmentSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
				}

				break;
		}
		RefreshVisuals();
	}

	private void OnItemRemovedOnSubSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Component> newArray = ToSafeList(m_entityData.auxiliar);
				newArray.Remove(_display.SavedData);
				m_entityData.auxiliar = newArray.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
				{
					if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor), ComponentDisplay.DisplayMode.Unit);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable + m_entityData.FrameData.occultorSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Unit);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, GetFrameSubComponentAt(i),
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Unit);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
					}
				}

				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Component> newArray2 = ToSafeList(m_entityData.chipsets);
				newArray2.Remove(_display.SavedData);
				m_entityData.chipsets = newArray2.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.chipsets != null && m_entityData.chipsets.Length > i
						? m_entityData.chipsets[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Unit);
					if (m_entityData.BrainData != null && m_entityData.BrainData.chipsetSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
				}

				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Component> newArray3 = ToSafeList(m_entityData.arms);
				newArray3.Remove(_display.SavedData);
				m_entityData.arms = newArray3.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.arms != null && m_entityData.arms.Length > i
						? m_entityData.arms[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Unit);
					if (m_entityData.NeuronalMembraneData != null && m_entityData.NeuronalMembraneData.equipmentSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
				}

				break;
		}
		RefreshVisuals();
	}

	private void OnItemAddedOnMainSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				m_entityData.frame = _display.SavedData;
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				m_entityData.brain = _display.SavedData;
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				m_entityData.neuronalMembrane = _display.SavedData;
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				m_entityData.reactor = _display.SavedData;
				break;
		}

		FitSubComponentsToMain(_type);
		InitSubSlots(_type);
		RefreshVisuals();
	}

	private void FitSubComponentsToMain ( EntityEquipmentData.EquipmentType _mainType )
	{
		List<GameDatas.PlayerSave.Component> remaining;
		List<GameDatas.PlayerSave.Component> kept;

		switch (_mainType)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				remaining = GetEquippedComponents(m_entityData.auxiliar);
				kept = TakeFirst(remaining, type => type == EntityEquipmentData.EquipmentType.Armor
					, m_entityData.FrameData == null ? 0 : m_entityData.FrameData.armouringSlotAvailable);
				kept.AddRange(TakeFirst(remaining, type => type == EntityEquipmentData.EquipmentType.Occultor
					, m_entityData.FrameData == null ? 0 : m_entityData.FrameData.occultorSlotAvailable));
				m_entityData.auxiliar = kept.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				remaining = GetEquippedComponents(m_entityData.chipsets);
				kept = TakeFirst(remaining, type => type == EntityEquipmentData.EquipmentType.Chipset
					, m_entityData.BrainData == null ? 0 : m_entityData.BrainData.chipsetSlotAvailable);
				m_entityData.chipsets = kept.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				remaining = GetEquippedComponents(m_entityData.arms);
				kept = TakeFirst(remaining, type => type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool
					, m_entityData.NeuronalMembraneData == null ? 0 : m_entityData.NeuronalMembraneData.equipmentSlotAvailable);
				m_entityData.arms = kept.ToArray();
				break;
			default:
				return;
		}

		foreach (GameDatas.PlayerSave.Component component in remaining)
			GameDatas.current.currentPlayerSave.AddEquipmentToInventory(component);
	}

	private static List<GameDatas.PlayerSave.Component> GetEquippedComponents ( GameDatas.PlayerSave.Component[] _components )
	{
		return _components == null ? new() : _components.Where(component => component != null && !string.IsNullOrEmpty(component.ID)).ToList();
	}

	private static List<GameDatas.PlayerSave.Component> TakeFirst ( List<GameDatas.PlayerSave.Component> _source, System.Func<EntityEquipmentData.EquipmentType, bool> _isAccepted, int _amount )
	{
		List<GameDatas.PlayerSave.Component> taken = _source.Where(component => component.TryGetData(out EntityEquipmentData data) && _isAccepted(data.GetEquipmentType()))
			.Take(Mathf.Max(0, _amount)).ToList();
		_source.RemoveAll(taken.Contains);
		return taken;
	}

	private void OnItemRemovedOnMainSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		if (m_mainComponentSlotDictionary[_type].IsSwapping)
			return;

		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				int auxiliarSlotCount = m_entityData.FrameData == null ? 0
					: m_entityData.FrameData.armouringSlotAvailable + m_entityData.FrameData.occultorSlotAvailable;

				m_entityData.frame = null;
				if (m_entityData.auxiliar != null)
				{
					foreach (GameDatas.PlayerSave.Component eq in m_entityData.auxiliar)
					{
						if (eq != null)
							GameDatas.current.currentPlayerSave.AddEquipmentToInventory(eq);
					}
					m_entityData.auxiliar = new GameDatas.PlayerSave.Component[auxiliarSlotCount];
				}
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				m_entityData.brain = null;
				if (m_entityData.chipsets != null)
				{
					foreach (GameDatas.PlayerSave.Component eq in m_entityData.chipsets)
					{
						if (eq != null)
							GameDatas.current.currentPlayerSave.AddEquipmentToInventory(eq);
					}
					m_entityData.chipsets = new GameDatas.PlayerSave.Component[0];
				}
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				m_entityData.neuronalMembrane = null;
				if (m_entityData.arms != null)
				{
					foreach (GameDatas.PlayerSave.Component eq in m_entityData.arms)
					{
						if (eq != null)
							GameDatas.current.currentPlayerSave.AddEquipmentToInventory(eq);
					}
					m_entityData.arms = new GameDatas.PlayerSave.Component[0];
				}
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				m_entityData.reactor = null;
				break;
		}

		InitSubSlots(_type);
		RefreshVisuals();
	}

	private void OnClickFilterType ( EntityEquipmentData.EquipmentType _type )
	{
		SetSelectedFilterType(m_selectedFilterType == _type ? null : _type);
	}

	private void OnClickAllFilterType ()
	{
		SetSelectedFilterType(null);
	}

	private void SetSelectedFilterType ( EntityEquipmentData.EquipmentType? _type )
	{
		m_selectedFilterType = _type;
		RefreshFilterVisuals();
		RefreshVisuals();
	}

	private void RefreshFilterVisuals ()
	{
		BaseButton selectedBtn = null;
		if (m_selectedFilterType.HasValue)
			m_componentTypeFilterBtnDictionary.TryGetValue(m_selectedFilterType.Value, out selectedBtn);

		m_currentFilterSelectedTMP.text = m_selectedFilterType.HasValue ? EntityEquipmentData.GetLocalizedTypeName(m_selectedFilterType.Value) : "";
		m_filterCursorFO.SetActive(selectedBtn != null);

		if (selectedBtn == null)
			return;

		Vector3 cursorPosition = m_filterCursorFO.transform.position;
		cursorPosition.x = selectedBtn.transform.position.x;
		m_filterCursorFO.transform.position = cursorPosition;
	}

	public List<EntityEquipmentData.SecondaryStat.StatType> GetDisplayedStats ( SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> _statsDescriptions )
	{
		List<EntityEquipmentData.SecondaryStat.StatType> keys = _statsDescriptions.Keys.ToList();

		foreach (EntityEquipmentData.SecondaryStat.StatType stat in keys.ToArray())
		{
			bool conditionalPredicate = m_displayConditionalStatsFilter.Contains(stat)
				&& ((_statsDescriptions[stat].floatValue != 0 && (_statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Int || _statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Percentage || _statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Cell))
				|| (_statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.String));
			bool staticPredicate = m_displayStaticStatsFilter.Contains(stat);
			if (!conditionalPredicate && !staticPredicate)
				keys.Remove(stat);
		}

		List<EntityEquipmentData.SecondaryStat.StatType> order = GameConfig.current.ui.statsDisplayOrder.ToList();
		return keys.OrderBy(e => order.IndexOf(e) < 0 ? int.MaxValue : order.IndexOf(e)).ToList();
	}

	private static readonly HashSet<EntityEquipmentData.SecondaryStat.StatType> s_slotAmountStats = new()
	{
		EntityEquipmentData.SecondaryStat.StatType.ChipsetSlot,
		EntityEquipmentData.SecondaryStat.StatType.EquipmentSlot,
		EntityEquipmentData.SecondaryStat.StatType.ArmourySlot,
		EntityEquipmentData.SecondaryStat.StatType.OccultorSlot,
	};

	public List<EntityEquipmentData.SecondaryStat.StatType> GetUnitSummaryStats ( SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> _statsDescriptions )
	{
		return GetDisplayedStats(_statsDescriptions).Where(stat => !s_slotAmountStats.Contains(stat)).ToList();
	}

	private void OnInputFieldChange ()
	{
		if (m_entityData == null)
			return;

		if (string.IsNullOrWhiteSpace(m_unitNameInputField.text))
		{
			m_unitNameInputField.SetTextWithoutNotify(m_entityData.name);
			return;
		}

		m_entityData.name = m_unitNameInputField.text;
	}

	private void OnClickRenameBtn ()
	{
		m_unitNameInputField.Select();
	}

	#endregion

}
