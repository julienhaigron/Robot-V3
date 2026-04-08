using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CreateAssetMenu(fileName = "Currency", menuName = "ScriptableObject/Currency")]
public class Currency : ScriptableEnum<CurrencyType>
{
	[Required]
	public string displayName = "";
	public bool onlyVisibleWhenGreaterThanZero = true;
	public bool affectedByCurrencyHistory = false;
	public bool showOnPlayerWhenCurrencyAdded = true;

	[HorizontalGroup("Icon", .5f), LabelWidth(0), BoxGroup("Icon/Icon")]
	[HideLabel]
	[PreviewField(ObjectFieldAlignment.Center, Height = 100f)]
	public Sprite icon;//withoutOutline
	[HorizontalGroup("Icon", .5f), LabelWidth(0), BoxGroup("Icon/Icon with Outline")]
	[HideLabel]
	[PreviewField(ObjectFieldAlignment.Center, Height = 100f)]
	public Sprite iconWithOutline;//used for flying Particle Image
	[Space]
	[AssetsOnly]
	[PreviewField(ObjectFieldAlignment.Left, Height = 100f)]
	public Mesh model;

	public ulong baseCurrency;

	public bool showOnlyIfUpgradeOneTime = false;
	public CurrencyMaxCapacityType maxCapacityType;
	public bool IsUsingMaxCapacity => maxCapacityType != CurrencyMaxCapacityType.None;
	[ShowIf("@maxCapacityType == CurrencyMaxCapacityType.UpgradeAsset || showOnlyIfUpgradeOneTime")]
	public UpgradeAsset upgradeAsset;
	[ShowIf("maxCapacityType", CurrencyMaxCapacityType.Flat)]
	public ulong maxCapacityFlat;
	[ShowIf("maxCapacityType", CurrencyMaxCapacityType.UpgradeAsset)]
	public ulong[] maxCapacityPerLevel;


	public ulong MaxCapacity
	{
		get
		{
			switch (maxCapacityType)
			{
				case CurrencyMaxCapacityType.Flat:
					return maxCapacityFlat;
				case CurrencyMaxCapacityType.UpgradeAsset:
					return maxCapacityPerLevel[Mathf.Min(maxCapacityPerLevel.Length - 1, upgradeAsset.GetCurrentLevel())];
				default:
					return 0;
			}
		}
	}

	public bool IsCapacityMaxed => GameDatas.current.currentPlayerSave.currencies[enumID] >= MaxCapacity;

	public enum CurrencyMaxCapacityType
	{
		None,
		Flat,
		UpgradeAsset
	}
}
