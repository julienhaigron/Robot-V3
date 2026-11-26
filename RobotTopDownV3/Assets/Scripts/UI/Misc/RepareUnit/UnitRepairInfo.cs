using System.Collections.Generic;

public readonly struct UnitRepairInfo
{
	public readonly struct DamagedPart
	{
		public readonly EntityEquipmentData data;
		public readonly bool isMain;

		public EntityEquipmentData.EquipmentType Type => data.GetEquipmentType();

		public DamagedPart ( EntityEquipmentData _data, bool _isMain )
		{
			data = _data;
			isMain = _isMain;
		}
	}

	public readonly List<DamagedPart> parts;
	public readonly int componentCount;
	public readonly int mainCount;
	public readonly string necessaryPriceText;
	public readonly string fullPriceText;

	public bool IsOutOfService => mainCount > 0;

	public UnitRepairInfo ( EntitySavedData _unit )
	{
		parts = new();
		mainCount = 0;
		List<GameDatas.PlayerSave.Component> mains = _unit.GetComponentsToRepair(false);
		foreach (GameDatas.PlayerSave.Component component in _unit.GetComponentsToRepair(true))
		{
			if (!component.TryGetData(out EntityEquipmentData data))
				continue;

			bool isMain = mains.Contains(component);
			parts.Add(new DamagedPart(data, isMain));
			if (isMain)
				mainCount++;
		}

		componentCount = _unit.GetAllEquipments().Count;
		necessaryPriceText = mainCount > 0 ? PriceText(_unit, false) : NoPrice;
		fullPriceText = parts.Count > 0 ? PriceText(_unit, true) : NoPrice;
	}

	public const string NoPrice = "—";

	private static string PriceText ( EntitySavedData _unit, bool _isFullRepair )
	{
		return DayEventState.FormatPrice(_unit.GetRepairPrice(_isFullRepair).Item2, _unit.GetRepairPrice(_isFullRepair, false).Item2);
	}
}
