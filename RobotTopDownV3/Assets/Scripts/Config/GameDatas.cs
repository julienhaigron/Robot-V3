using Sirenix.OdinInspector;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "GameDatas", menuName = "ScriptableObject/GameDatas")]
public partial class GameDatas : ScriptableObject
{
	public static GameDatas current => ApplicationManager.datas;
	private static string m_defaultSaveFile = "product.sav";
	public static string defaultSaveFile => m_defaultSaveFile;

	public static Action onBeforeSave;
	public static Action onAfterSave;
	public static Action onBeforeLoad;
	public static Action onAfterLoad;

	public static Action<CurrencyType> onCurrencyChanged;
	public static Action<CurrencyType, ulong> onCurrencyAdded;
	public static Action<CurrencyType, ulong, PlayerSave.CurrencyRemoveMode> onCurrencyRemoved;

	public static Action onNewDay;
	public static Action onNewCycle;

#if UNITY_EDITOR
	[TitleGroup("Quick Settings")]
	[InfoBox("PreventSave is ignored in builds.")]
	public bool preventSave = false;

#endif

	[TitleGroup("Saved Datas")]
	public App app = new App();
	[TitleGroup("Saved Datas")]
	public Game game = new Game();
	[TitleGroup("Saved Datas")]
	public List<PlayerSave> playerSaves = new List<PlayerSave>();
	public PlayerSave currentPlayerSave
	{
		get
		{
			if (playerSaves == null || game.lastPlayerSaveSelectedID == -1 || playerSaves.Count <= game.lastPlayerSaveSelectedID)
			{
				//Debug.LogError("No saves detected. Creating default save");
				game.lastPlayerSaveSelectedID = 0;
				CreateSave("NewSave");
				return playerSaves[0];
			}
			else
				return playerSaves[game.lastPlayerSaveSelectedID];
		}
	}

	[System.Serializable]
	public partial class App
	{
		public bool hapticEnabled = true;
		public float masterVolume = 1f;
		public float musicVolume = 1f;
		public float sfxVolume = 1f;
		public float uiVolume = 1f;
		public SystemLanguage language = SystemLanguage.English;
		public string inputBindingOverridesJson = "";
	}

	[System.Serializable]
	public partial class Game
	{
		public int lastPlayerSaveSelectedID = -1;
	}

	public void CreateSave ( string _saveName )
	{
		PlayerSave newSave = new PlayerSave();
		newSave.saveName = _saveName;
		playerSaves.Add(newSave);
		newSave.Initialize();
	}

	public bool RenameSave ( int _saveID, string _newName )
	{
		string trimmedName = _newName == null ? null : _newName.Trim();
		if (!IsValidSaveID(_saveID) || string.IsNullOrEmpty(trimmedName))
			return false;

		playerSaves[_saveID].saveName = trimmedName;
		return true;
	}

	public bool DeleteSave ( int _saveID )
	{
		if (!IsValidSaveID(_saveID))
			return false;

		playerSaves.RemoveAt(_saveID);

		if (playerSaves.Count == 0 || game.lastPlayerSaveSelectedID == _saveID)
			game.lastPlayerSaveSelectedID = -1;
		else if (game.lastPlayerSaveSelectedID > _saveID)
			game.lastPlayerSaveSelectedID--;

		return true;
	}

	public bool IsValidSaveID ( int _saveID )
	{
		return playerSaves != null && _saveID >= 0 && _saveID < playerSaves.Count && playerSaves[_saveID] != null;
	}

	private void StampCurrentSaveTime ()
	{
		if (IsValidSaveID(game.lastPlayerSaveSelectedID))
			playerSaves[game.lastPlayerSaveSelectedID].lastSaveTimeTicks = DateTime.Now.Ticks;
	}

	[System.Serializable]
	public class PlayerSave
	{
		public string saveName;

		public long lastSaveTimeTicks = 0;

		public string GetLastSaveTimeText ()
		{
			if (lastSaveTimeTicks <= 0)
				return LocalizationManager.Instance.Get(LocalizationKey.save_never_saved);

			DateTime lastSave = new DateTime(lastSaveTimeTicks);
			TimeSpan elapsed = DateTime.Now - lastSave;
			if (elapsed < TimeSpan.Zero)
				elapsed = TimeSpan.Zero;

			if (elapsed.TotalMinutes < 1d)
				return string.Format(LocalizationManager.Instance.Get(LocalizationKey.save_time_seconds), (int)elapsed.TotalSeconds);
			if (elapsed.TotalHours < 1d)
				return string.Format(LocalizationManager.Instance.Get(LocalizationKey.save_time_minutes), (int)elapsed.TotalMinutes);
			if (elapsed.TotalDays < 1d)
				return string.Format(LocalizationManager.Instance.Get(LocalizationKey.save_time_hours), (int)elapsed.TotalHours);
			if (elapsed.TotalDays < 7d)
				return string.Format(LocalizationManager.Instance.Get(LocalizationKey.save_time_days), (int)elapsed.TotalDays);

			return string.Format(LocalizationManager.Instance.Get(LocalizationKey.save_last_played), lastSave.ToString("g"));
		}

		public List<EntitySavedData> allBuiltUnits = new();
		public List<int> squadUnitsIndex = new();
		public List<Component> equipmentInventory = new();
		public int equipmentCounter = 0;

		public SerializableDictionary<CurrencyType, ulong> currencies = new SerializableDictionary<CurrencyType, ulong>();
		public SerializableDictionary<CurrencyType, ulong> totalCurrenciesGot = new SerializableDictionary<CurrencyType, ulong>();
		public SerializableDictionary<CurrencyType, ulong> totalCurrenciesSpent = new SerializableDictionary<CurrencyType, ulong>();

		public SerializableDictionary<string, int> upgradeLevels = new SerializableDictionary<string, int>();

		public DayData dayData = new();
		public CycleData cycleData = new();
		public int cycleCount = 0;
		public int dayCount = 0;

		public bool IsTournamentDay => dayCount > 3;

		//tutos
		public bool didStartTuto = false;
		public bool didUnlockReturnToHubPopup = false;
		public bool didUnlockRepareStation = false;
		public bool didUnlockRecycler = false;
		public bool didUnlockShops = false;
		public bool DidFirstIntroLevel => sequencesProgressions.ContainsKey(FTUEManager.FTUEID) && (sequencesProgressions[FTUEManager.FTUEID] > 0 || sequencesProgressions[FTUEManager.FTUEID] == -1);
		public SerializableDictionary<string, int> sequencesProgressions = new SerializableDictionary<string, int>();

		[Serializable]
		public class DayData
		{
			public List<ShopComponentData> itemsInShop = new();
			public List<RecyclingComponentData> currentlyRecyclingComponents = new();
			public List<RepairingUnitData> repairingComponents = new();

			[Serializable]
			public class ShopComponentData
			{
				public Component component;
				public bool isFrozen = false;
			}

			[Serializable]
			public class RecyclingComponentData
			{
				public Component component;
				public int remainingTime;
			}

			[Serializable]
			public class RepairingUnitData
			{
				public EntitySavedData unit;
				public bool wasUsedToday;
			}

		}

		[Button]
		public void NewDay ()
		{
			dayCount++;
			if (dayCount >= GameConfig.current.game.nbOfDayInCycle)
				NewCycle();

			//new items in shop
			List<EntityEquipmentData> shopPool = GameAssets.current.GetRandomlyDroppableEquipments();
			for (int i = 0; shopPool.Count > 0 && i < GameAssets.current.game.ShopStructureUpgrade.GetMaxItemAmount(); i++)
			{
				EntityEquipmentData equipmentData = shopPool.RandomElement();
				dayData.itemsInShop.Add(new() { component = new() { ID = equipmentData.name + current.currentPlayerSave.equipmentCounter++, dataID = equipmentData.name, isDamaged = false }, isFrozen = false });
			}

			//recycling component
			foreach (DayData.RecyclingComponentData data in dayData.currentlyRecyclingComponents)
			{
				if (data == null)
					continue;

				data.remainingTime--;
				if (data.remainingTime < 0)
					data.remainingTime = 0;
			}

			foreach (DayData.RepairingUnitData data in dayData.repairingComponents)
				if (data != null)
					data.wasUsedToday = false;

			onNewDay?.Invoke();
		}

		public void ForceFinishRecycling ()
		{
			foreach (DayData.RecyclingComponentData data in dayData.currentlyRecyclingComponents)
				if (data != null)
					data.remainingTime = 0;
		}

		public string CollectFinishedDayJobs ()
		{
			string report = "";

			for (int i = 0; i < dayData.currentlyRecyclingComponents.Count; i++)
			{
				DayData.RecyclingComponentData data = dayData.currentlyRecyclingComponents[i];
				if (data == null || data.component == null || string.IsNullOrEmpty(data.component.ID) || data.remainingTime > 0)
					continue;

				EntityEquipmentData componentData = data.component.GetData<EntityEquipmentData>();
				report += componentData.GetLocalizedName() + " finished recycling \n";

				System.Tuple<CurrencyType, ulong> sellingPrice = componentData.GetSellingPrice();
				AddCurrency(sellingPrice.Item1, sellingPrice.Item2);
				dayData.currentlyRecyclingComponents[i] = null;
			}

			return report;
		}

		[Serializable]
		public class CycleData
		{
			public List<MissionDataEnumID> availableMissionsIds = new();
			public List<MissionDataEnumID> selectedMissionsIds = new();
			public MissionDataEnumID[] roundsDatas;

			public bool didSelectMissions => GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds.Count > 0;
			public bool hasInitTournament = false;

			public void StartTournament ()
			{
				hasInitTournament = true;
				roundsDatas = new MissionDataEnumID[3];

				bool hasScriptedTournament = current.currentPlayerSave.cycleCount == 0;
#if UNITY_EDITOR
				if (GameConfig.current.debug.skipFTUE)
					hasScriptedTournament = false;
#endif
				if (hasScriptedTournament)
				{
					roundsDatas[0] = FTUEManager.Instance.Cycle1TournamentMissions[0].enumID;
					roundsDatas[1] = FTUEManager.Instance.Cycle1TournamentMissions[1].enumID;
					roundsDatas[2] = FTUEManager.Instance.Cycle1TournamentMissions[2].enumID;
				}
				else
				{
					roundsDatas[0] = GameAssets.current.game.tournamentMissionsPool.ToList().RandomElement().enumID;
					roundsDatas[1] = GameAssets.current.game.tournamentMissionsPool.ToList().RandomElement().enumID;
					roundsDatas[2] = GameAssets.current.game.tournamentMissionsPool.ToList().RandomElement().enumID;
				}
			}
		}

		[Button]
		public void NewCycle ()
		{
			cycleData.hasInitTournament = false;
			cycleData.roundsDatas = new MissionDataEnumID[3];

			cycleData.selectedMissionsIds.Clear();
			//missions
			cycleData.availableMissionsIds.Clear();
			for (int i = 0; i < GameConfig.current.game.missionAmountInMissionSelectionPanel; i++)
			{
				cycleData.availableMissionsIds.Add(GameAssets.current.game.missions.Keys.ToList().RandomElement());
			}

			cycleCount++;
			dayCount = 0;
			onNewCycle?.Invoke();
		}

		public List<EntitySavedData> GetSquadEntitiesData ()
		{
			List<EntitySavedData> squadData = new();
			foreach (int entityIndex in squadUnitsIndex)
				squadData.Add(allBuiltUnits[entityIndex]);

			return squadData;
		}

		public Component AddComponentToInventory ( EntityEquipmentData _data )
		{
			string newID = _data == null ? null : _data.name + equipmentCounter;
			if (_data == null || string.IsNullOrEmpty(newID))
				return null;

			Component equipment = new() { ID = newID, dataID = _data.name };

			AddEquipmentToInventory(equipment);

			return equipment;
		}

		public void AddEquipmentToInventory(Component _equipment )
		{
			if (_equipment == null || string.IsNullOrEmpty(_equipment.ID) || equipmentInventory.Contains(_equipment))
				return;

			if (_equipment.acquisitionDateTicks <= 0)
				_equipment.acquisitionDateTicks = DateTime.UtcNow.Ticks;

			equipmentInventory.Add(_equipment);
			equipmentCounter++;
		}

		public void RemoveEquipmentFromInventory ( Component _data )
		{
			equipmentInventory.Remove(_data);
		}

		public bool DisassembleUnit ( EntitySavedData _unit )
		{
			if (_unit == null)
				return false;

			foreach (Component component in _unit.GetAllComponents())
				AddEquipmentToInventory(component);

			_unit.ClearAllComponents();
			
			int removedIndex = allBuiltUnits.IndexOf(_unit);
			if (removedIndex < 0)
				return false;
			allBuiltUnits.RemoveAt(removedIndex);
			squadUnitsIndex.Remove(removedIndex);

			for (int i = 0; i < squadUnitsIndex.Count; i++)
				if (squadUnitsIndex[i] > removedIndex)
					squadUnitsIndex[i]--;

			for (int i = 0; i < allBuiltUnits.Count; i++)
				allBuiltUnits[i].index = i;

			return true;
		}

		public EntitySavedData AddNewUnit ( EntitySavedData _newUnit, bool _addToSquad )
		{
			//_newUnit.name = "New Unit";
			_newUnit.index = allBuiltUnits.Count;

			if (_newUnit.currentHp <= 0)
				_newUnit.currentHp = _newUnit.GetMaxHealth();

			if (_addToSquad && _newUnit.CanAddToSquad())
				squadUnitsIndex.Add(allBuiltUnits.Count);
			allBuiltUnits.Add(_newUnit);

			return _newUnit;
		}

		[Serializable]
		public class Component : INetworkSerializable
		{
			public string ID;
			public string dataID;
			public bool isDamaged = false;
			public long acquisitionDateTicks;
			//public bool[] areSlotsDamaged;

			public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
			{
				serializer.SerializeValue(ref ID);
				serializer.SerializeValue(ref dataID);
				serializer.SerializeValue(ref isDamaged);
				serializer.SerializeValue(ref acquisitionDateTicks);
				//serializer.SerializeValue(ref areSlotsDamaged);
			}

			public T GetData<T> () where T : EntityEquipmentData
			{
				if (string.IsNullOrEmpty(dataID) || !GameAssets.current.equipments.ContainsKey(dataID))
					return null;

				return GameAssets.current.equipments[dataID] as T;
			}

			public bool TryGetData<T> ( out T _data ) where T : EntityEquipmentData
			{
				_data = GetData<T>();
				return _data != null;
			}
		}

		public void Initialize ()
		{
			if (currencies == null)
			{
				currencies = new SerializableDictionary<CurrencyType, ulong>();
			}
			if (totalCurrenciesGot == null)
			{
				totalCurrenciesGot = new SerializableDictionary<CurrencyType, ulong>();
			}
			if (totalCurrenciesSpent == null)
			{
				totalCurrenciesSpent = new SerializableDictionary<CurrencyType, ulong>();
			}
			foreach (KeyValuePair<CurrencyType, Currency> currency in GameAssets.current.currencies)
			{
				if (!currencies.ContainsKey(currency.Key))
				{
					currencies.Add(currency.Key, currency.Value.baseCurrency);
				}
				if (!totalCurrenciesGot.ContainsKey(currency.Key))
				{
					totalCurrenciesGot.Add(currency.Key, currency.Value.baseCurrency);
				}
				if (!totalCurrenciesSpent.ContainsKey(currency.Key))
				{
					totalCurrenciesSpent.Add(currency.Key, 0);
				}
			}

			if (upgradeLevels == null)
			{
				upgradeLevels = new SerializableDictionary<string, int>();
			}
			foreach (UpgradeAsset upgrade in GameAssets.current.upgrades)
			{
				if (!upgradeLevels.ContainsKey(upgrade.saveKey))
				{
					upgradeLevels.Add(upgrade.saveKey, 0);
				}
			}

			NewCycle();
			cycleCount = 0;

			NewDay();
			dayCount = 0;
		}

		#region Currencies

		public enum CurrencyRemoveMode
		{
			Spent,
			Lost,
		}

		public void AddCurrency ( CurrencyType _type, ulong _amount, string eventID )
		{
			if (_amount <= 0ul)
				return;

			AddCurrency(_type, _amount);
		}

		public void RemoveCurrency ( CurrencyType _type, ulong _amount, string eventID, CurrencyRemoveMode _currencyRemoveMode = CurrencyRemoveMode.Spent )
		{
			if (_amount <= 0ul)
				return;

			RemoveCurrency(_type, _amount, _currencyRemoveMode);
		}

		public void AddCurrency ( CurrencyType _type, ulong _amount )
		{
			if (_amount <= 0ul)
				return;

			//GameConfig.current.feedbacks.addCurrencyFeedback.PlayQueue(0, feedback);
			currencies[_type] += _amount;
			totalCurrenciesGot[_type] += _amount;

			onCurrencyChanged?.Invoke(_type);
			onCurrencyAdded?.Invoke(_type, _amount);
		}

		public void RemoveCurrency ( CurrencyType _type, ulong _amount, CurrencyRemoveMode _currencyRemoveMode = CurrencyRemoveMode.Spent )
		{
			if (_amount <= 0ul)
				return;

			//GameConfig.current.feedbacks.removeCurrencyFeedback.Play(feedback);
			if (_amount > currencies[_type])
			{
				Debug.LogWarning("TRIED TO REMOVE MORE CURRENCY " + _type.ToString() + " THAN POSSESSED (" + currencies[_type] + " - " + _amount + ")");
				totalCurrenciesSpent[_type] += currencies[_type];
				onCurrencyRemoved?.Invoke(_type, currencies[_type], _currencyRemoveMode);
				currencies[_type] = 0;
			}
			else
			{
				currencies[_type] -= _amount;
				totalCurrenciesSpent[_type] += _amount;
				onCurrencyRemoved?.Invoke(_type, _amount, _currencyRemoveMode);
			}
			onCurrencyChanged?.Invoke(_type);
		}

		#endregion

	}

	#region Saving
	public static void Save ( GameDatas datas, string savePath = null, bool usePersistentPath = true )
	{
#if UNITY_EDITOR
		if (datas.preventSave)
		{
			return;
		}
#endif
		if (datas == null)
		{
			return;
		}

		onBeforeSave?.Invoke();

		datas.StampCurrentSaveTime();

		if (savePath == null)
		{
			savePath = m_defaultSaveFile;
		}
		if (usePersistentPath && !savePath.StartsWith(Application.persistentDataPath))
		{
			savePath = Application.persistentDataPath + "/" + savePath;
		}
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(savePath);
		bf.Serialize(file, DatasToJson(datas));
		file.Close();
		SetDatasDirty(datas);

		onAfterSave?.Invoke();
	}

	public static void Save ( string savePath = null )
	{
		Save(ApplicationManager.datas, savePath);
	}

	public void Save ()
	{
		Save(this, m_defaultSaveFile);
	}

	public static GameDatas LoadFromJson ( string jsonDatas )
	{
		GameDatas gameDatas = ScriptableObject.CreateInstance<GameDatas>();

		if (jsonDatas != null)
		{
			try
			{
				JsonUtility.FromJsonOverwrite(jsonDatas, gameDatas);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		gameDatas.Initialize();

		//Save(gameDatas);
		return gameDatas;
	}

	public static GameDatas Load ( string savePath = null, bool usePersistentPath = true )
	{
		onBeforeLoad?.Invoke();
		string jsonDatas = null;
		if (savePath == null)
		{
			savePath = m_defaultSaveFile;
		}
		if (usePersistentPath && !savePath.StartsWith(Application.persistentDataPath))
		{
			savePath = Application.persistentDataPath + "/" + savePath;
		}
		if (File.Exists(savePath))
		{
			FileStream file = File.Open(savePath, FileMode.Open);
			BinaryFormatter bf = new BinaryFormatter();
			try
			{
				jsonDatas = (string)bf.Deserialize(file);
				file.Close();
				//TODO: save backup of this datas
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				file.Close();
				File.Delete(savePath);
				//TODO: load backup cause failed
			}
		}

		GameDatas gameDatas = LoadFromJson(jsonDatas);
		onAfterLoad?.Invoke();
		return (gameDatas);
	}

	public void Load ()
	{
		onBeforeLoad?.Invoke();
		string savePath = m_defaultSaveFile;
		if (!savePath.StartsWith(Application.persistentDataPath))
		{
			savePath = Application.persistentDataPath + "/" + savePath;
		}
		if (!File.Exists(savePath))
		{
			GameDatas gameDatas = ScriptableObject.CreateInstance<GameDatas>();
			gameDatas.name = name;
			OverrideFromJson(this, DatasToJson(gameDatas));
		}
		else
		{
			Override(this, m_defaultSaveFile);
		}
		onAfterLoad?.Invoke();
	}

	public static string DatasToJson ( GameDatas datas )
	{
		if (datas == null)
		{
			return null;
		}
		return JsonUtility.ToJson(datas);
	}

	public static void OverrideFromJson ( GameDatas gameDatasToOverride, string jsonDatas )
	{
		if (jsonDatas != null)
		{
			try
			{
				JsonUtility.FromJsonOverwrite(jsonDatas, gameDatasToOverride);
				Debug.Log("Datas Loaded with override from Json.");
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}
		else
			Debug.LogWarning("couldn't override datas : Json null");
		gameDatasToOverride.Initialize();
	}

	public static void Override ( GameDatas gameDatasToOverride, string savePath = null, bool usePersistentPath = true )
	{
		string jsonDatas = null;
		if (savePath == null)
		{
			savePath = m_defaultSaveFile;
		}
		if (usePersistentPath && !savePath.StartsWith(Application.persistentDataPath))
		{
			savePath = Application.persistentDataPath + "/" + savePath;
		}
		if (File.Exists(savePath))
		{
			FileStream file = File.Open(savePath, FileMode.Open);
			BinaryFormatter bf = new BinaryFormatter();
			try
			{
				jsonDatas = (string)bf.Deserialize(file);
				file.Close();
				//TODO: save backup of this datas
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				file.Close();
				File.Delete(savePath);
				//TODO: load backup cause failed
			}
		}

		OverrideFromJson(gameDatasToOverride, jsonDatas);
	}

	public void Overrive ( string savePath = null )
	{
		Override(this, savePath);
	}

	[Title("Editor Tools")]
	public static void SetDatasDirty ( GameDatas datas = null )
	{
#if UNITY_EDITOR
		if (datas == null)
			datas = GameDatas.current;
		EditorUtility.SetDirty(datas);
#endif
	}

	#endregion

#if UNITY_EDITOR
	public static string GetEmptyDatas ()
	{
		GameDatas gameDatas = ScriptableObject.CreateInstance<GameDatas>();
		gameDatas.name = "GameDatas";
		string datas = DatasToJson(gameDatas);
		DestroyImmediate(gameDatas);
		return datas;
	}

	public static void Clear ()
	{
		bool prevSave = current.preventSave;
		string savePath = defaultSaveFile;
		if (!savePath.StartsWith(Application.persistentDataPath))
		{
			savePath = Application.persistentDataPath + "/" + savePath;
		}
		if (File.Exists(savePath))
		{
			File.Delete(savePath);
		}

		OverrideFromJson(current, GetEmptyDatas());
		current.preventSave = prevSave;
		EditorUtility.SetDirty(current);
	}
#endif

	public void Initialize ()
	{
		//meta.Initialize();


		Debug.Log("Game Datas Initialized.");
	}

}
