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
		public float musicVolume = 1f;
		public float sfxVolume = 1f;
	}

	[System.Serializable]
	public partial class Game
	{
		public int lastPlayerSaveSelectedID = -1;
	}

	public void CreateSave (string _saveName)
	{
		PlayerSave newSave = new PlayerSave();
		newSave.saveName = _saveName;
		playerSaves.Add(newSave);
		newSave.Initialize();
	}
	
	[System.Serializable]
	public class PlayerSave
	{
		public string saveName;
		public List<EntitySavedData> allBuiltUnits = new();
		public List<EntitySavedData> squadUnits = new();
		public List<Equipment> equipmentInventory = new ();
		public int equipmentCounter = 0;

		public SerializableDictionary<CurrencyType, ulong> currencies = new SerializableDictionary<CurrencyType, ulong>();
		public SerializableDictionary<CurrencyType, ulong> totalCurrenciesGot = new SerializableDictionary<CurrencyType, ulong>();
		public SerializableDictionary<CurrencyType, ulong> totalCurrenciesSpent = new SerializableDictionary<CurrencyType, ulong>();

		public SerializableDictionary<string, int> upgradeLevels = new SerializableDictionary<string, int>();

		public DayData dayData = new();
		public CycleData cycleData = new();
		public int cycleCount = 0;
		public int dayCount = 0;

		//tutos
		public bool didStartTuto = false;
		public bool DidFirstIntroLevel => sequencesProgressions.ContainsKey(FTUEManager.FTUEID) && (sequencesProgressions[FTUEManager.FTUEID] > 0 || sequencesProgressions[FTUEManager.FTUEID] == -1);
		public SerializableDictionary<string, int> sequencesProgressions = new SerializableDictionary<string, int>();

		[Serializable]
		public class DayData
		{
			public List<ShopComponentData> itemsInShop = new();
			public List<RecyclingComponentData> currentlyRecyclingComponents = new();
			public List<RepairingComponentData> repairingComponents = new();

			[Serializable]
			public class ShopComponentData
			{
				public Equipment component;
				public bool isFrozen = false;
			}

			[Serializable]
			public class RecyclingComponentData
			{
				public Equipment component;
				public int remainingTime;
			}

			[Serializable]
			public class RepairingComponentData
			{
				public Equipment component;
				public int remainingTime;
			}

		}

		[Button]
		public void NewDay ()
		{
			dayCount++;
			if (dayCount >= GameConfig.current.game.nbOfDayInCycle)
				NewCycle();

			//new items in shop
			for (int i = 0; i < GameAssets.current.game.ShopStructureUpgrade.GetMaxItemAmount(); i++)
			{
				EntityEquipmentData equipmentData = GameAssets.current.equipments.Values.ToArray().RandomElement();
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

			//repairing units
			foreach (DayData.RepairingComponentData data in dayData.repairingComponents)
			{
				if (data == null)
					continue;

				data.remainingTime--;
				if (data.remainingTime < 0)
					data.remainingTime = 0;
			}

			onNewDay?.Invoke();
		}

		[Serializable]
		public class CycleData
		{
			public List<MissionDataEnumID> availableMissionsIds = new();
			public List<MissionDataEnumID> selectedMissionsIds = new();
			public MissionDataEnumID[] roundsDatas;

			public bool didSelectMissions = false;
			public bool hasInitTournament = false;

			public void StartTournament ()
			{
				hasInitTournament = true;

				//TODO : design tournament matchup
				roundsDatas = new MissionDataEnumID[3];
				roundsDatas[0] = GameAssets.current.game.missions.Keys.ToList().RandomElement();
				roundsDatas[1] = GameAssets.current.game.missions.Keys.ToList().RandomElement();
				roundsDatas[2] = GameAssets.current.game.missions.Keys.ToList().RandomElement();
			}
		}

		[Button]
		public void NewCycle ()
		{
			cycleData.didSelectMissions = false;
			cycleData.hasInitTournament = false;
			cycleData.roundsDatas = new MissionDataEnumID[3];
			//missions
			cycleData.availableMissionsIds.Clear();
			for (int i = 0; i < GameConfig.current.game.missionAmountInMissionSelectionPanel; i++)
			{
				cycleData.availableMissionsIds.Add(GameAssets.current.game.missions.Keys.ToList().RandomElement());
			}

			cycleCount++;
		}

		public Equipment AddEquipmentToInventory ( EntityEquipmentData _data )
		{
			string newID = _data == null ? null : _data.name + equipmentCounter;
			if (_data == null || string.IsNullOrEmpty(newID))
				return null;

			Equipment equipment = new() { ID = newID, dataID = _data.name };
			/*int slotCount = 0;
			if(_data is FrameEquipmentData frameData)
			{
				slotCount += frameData.armouringSlotAvailable;
				slotCount += frameData.occultorSlotAvailable;
			}
			else if(_data is BrainEquipmentData brainData)
			{
				slotCount += brainData.chipsetSlotAvailable;
			}
			else if(_data is NeuronalMembraneEquipmentData nmData)
			{
				slotCount += nmData.equipmentSlotAvailable;
			}
			List<bool> slots = new();
			for(int i = 0; i < slotCount; i++)
				slots.Add(false);
			equipment.areSlotsDamaged = slots.ToArray();*/

			equipmentInventory.Add(equipment);
			equipmentCounter++;

			return equipment;
		}

		public void RemoveEquipmentFromInventory ( Equipment _data )
		{
			equipmentInventory.Remove(_data);
		}

		public EntitySavedData AddNewUnit ( EntitySavedData _newUnit)
		{
			//_newUnit.name = "New Unit";

			if(_newUnit.CanAddToSquad())
				squadUnits.Add(_newUnit);
			allBuiltUnits.Add(_newUnit);

			return _newUnit;
		}

		[Serializable]
		public class Equipment : INetworkSerializable
		{
			public string ID;
			public string dataID;
			public bool isDamaged = false;
			//public bool[] areSlotsDamaged;

			public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
			{
				serializer.SerializeValue(ref ID);
				serializer.SerializeValue(ref dataID);
				serializer.SerializeValue(ref isDamaged);
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

#if UNITY_EDITOR
			if (GameConfig.current.debug.skipFTUE)
				return;
#endif
			cycleData.availableMissionsIds.Clear();
			for (int i = 0; i < FTUEManager.Instance.Cycle1Missions.Length; i++)
				cycleData.availableMissionsIds.Add(FTUEManager.Instance.Cycle1Missions[i].enumID);
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
