using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class RecyclingComponentSlot : ComponentSlot
{
	[SerializeField] private BaseButton m_recycleBtn;
	[SerializeField, FormerlySerializedAs("m_timerSectionGO"), FormerlySerializedAs("m_priceSectionGO")] private GameObject m_infoSectionGO;
	[SerializeField, FormerlySerializedAs("m_timerTMP"), FormerlySerializedAs("m_priceTMP")] private TextMeshProUGUI m_infoTMP;
	[SerializeField] private GameObject m_currencyIconGO;
	[SerializeField] private SlidingDoors m_doors;

	private bool m_isRecycleBtnHovered;
	private Tween m_recycleBtnCall;

	public GameDatas.PlayerSave.DayData.RecyclingComponentData RecyclingData => GetRecyclingDataAt(Index);
	public bool IsRecycling => IsRecyclingData(RecyclingData);
	public bool IsFree => CurrentDisplay == null && !IsRecycling;

	protected override void Awake ()
	{
		base.Awake();
		m_recycleBtn.onClick += OnClickRecycle;
		m_recycleBtn.onHoverChanged += OnRecycleBtnHoverChanged;
	}

	private void OnDestroy ()
	{
		m_recycleBtn.onClick -= OnClickRecycle;
		m_recycleBtn.onHoverChanged -= OnRecycleBtnHoverChanged;
	}

	protected override void OnEnable ()
	{
		base.OnEnable();
		LocalizationManager.onLanguageChanged += RefreshRecyclingState;
	}

	protected override void OnDisable ()
	{
		base.OnDisable();
		LocalizationManager.onLanguageChanged -= RefreshRecyclingState;
		m_isRecycleBtnHovered = false;
		GestureFeedback.SetCalling(m_recycleBtn.transform, ref m_recycleBtnCall, false);
	}

	public void Init ( ComponentContainer _inventory, int _index )
	{
		GameDatas.PlayerSave.DayData.RecyclingComponentData recyclingData = GetRecyclingDataAt(_index);

		Init(_inventory, null, IsRecyclingData(recyclingData) ? recyclingData.component : null, item => item != null
			, ComponentDisplay.DisplayMode.RecyclingStation, _index);

		RefreshRecyclingState();
	}

	private static GameDatas.PlayerSave.DayData.RecyclingComponentData GetRecyclingDataAt ( int _index )
	{
		List<GameDatas.PlayerSave.DayData.RecyclingComponentData> recyclingComponents = GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents;

		return _index >= 0 && _index < recyclingComponents.Count ? recyclingComponents[_index] : null;
	}

	private static bool IsRecyclingData ( GameDatas.PlayerSave.DayData.RecyclingComponentData _data )
	{
		return _data != null && _data.component != null && !string.IsNullOrEmpty(_data.component.ID);
	}

	private bool CanStartRecycling ()
	{
		return CurrentDisplay != null && CurrentDisplay.SavedData != null && CurrentDisplay.ComponentData != null
			&& !IsRecycling && Index < GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Count;
	}

	private void OnClickRecycle ()
	{
		if (!CanStartRecycling())
			return;

		GameDatas.PlayerSave playerSave = GameDatas.current.currentPlayerSave;
		GameDatas.PlayerSave.Component component = CurrentDisplay.SavedData;
		EntityEquipmentData componentData = CurrentDisplay.ComponentData;
		int duration = componentData.GetRecyclingDuration();

		playerSave.RemoveEquipmentFromInventory(component);

		if (duration <= 0)
		{
			ComponentDisplay display = CurrentDisplay;
			Rect from = display.VisualWorldRect;
			System.Tuple<CurrencyType, ulong> price = componentData.GetRecyclingPrice();
			GestureFeedback.HoldCurrency(price.Item1);
			playerSave.AddCurrency(price.Item1, price.Item2);
			GestureFeedback.PlayMoney(from.center, price.Item1, (long)price.Item2);
			GestureFeedback.PlayDeparture(display, this, from, GestureFeedback.ColorOf(Gesture.Kind.Deposit), from);
			Cleanup();
		}
		else
		{
			playerSave.dayData.currentlyRecyclingComponents[Index] = new() { component = component, remainingTime = duration };
			m_doors.SetClosed(true, _isInstant: false);
		}

		RefreshRecyclingState();
		if (duration > 0)
			Pulse(GestureFeedback.ColorOf(Gesture.Kind.Deposit));
	}

	private void OnRecycleBtnHoverChanged ( bool _isHovered )
	{
		m_isRecycleBtnHovered = _isHovered;
		RefreshRecyclingState();
	}

	private void RefreshRecyclingState ()
	{
		GameDatas.PlayerSave.DayData.RecyclingComponentData recyclingData = RecyclingData;
		bool isRecycling = IsRecyclingData(recyclingData);
		EntityEquipmentData componentData = CurrentDisplay == null ? null : CurrentDisplay.ComponentData;
		bool isShowingDuration = componentData != null && (isRecycling || m_isRecycleBtnHovered);
		bool isPending = componentData != null && !isRecycling;

		m_infoSectionGO.SetActive(componentData != null);

		if (m_currencyIconGO != null)
			m_currencyIconGO.SetActive(componentData != null && !isShowingDuration);

		if (componentData == null)
			m_infoTMP.text = "";
		else if (isShowingDuration)
			m_infoTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.hub_recycling_duration)
				, isRecycling ? recyclingData.remainingTime : componentData.GetRecyclingDuration());
		else
			m_infoTMP.text = "+ " + componentData.GetRecyclingPrice().Item2;

		m_recycleBtn.SetInteractability(isPending);
		m_doors.SetClosed(isRecycling, _isInstant: true);
		SetLocked(componentData != null && isRecycling);
		SetPending(componentData != null);
		GestureFeedback.SetCalling(m_recycleBtn.transform, ref m_recycleBtnCall, isPending && isActiveAndEnabled);
		if (CurrentDisplay != null)
			CurrentDisplay.SetDimmed(isRecycling);
	}

	public override Gesture.Refusal GetReleaseRefusal ( GestureDisplay _display )
	{
		return IsRecycling ? Gesture.Refusal.Recycling : base.GetReleaseRefusal(_display);
	}

	public override void RegisterInteraction ( ComponentDisplay _display )
	{
		base.RegisterInteraction(_display);
		RefreshRecyclingState();
	}

	public override void RemoveDisplay ( ComponentDisplay _display )
	{
		base.RemoveDisplay(_display);
		RefreshRecyclingState();
	}
}
