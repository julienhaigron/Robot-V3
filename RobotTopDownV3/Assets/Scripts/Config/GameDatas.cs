using Sirenix.OdinInspector;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "GameDatas", menuName = "ScriptableObject/GameDatas")]
public class GameDatas : ScriptableObject
{
	public static GameDatas current => ApplicationManager.datas;
	private static string m_defaultSaveFile = "product.sav";
	public static string defaultSaveFile => m_defaultSaveFile;

	public static Action onBeforeSave;
	public static Action onAfterSave;
	public static Action onBeforeLoad;
	public static Action onAfterLoad;

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
	public Player player = new Player();

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
		
	}
	
	[System.Serializable]
	public partial class Player
	{
		public List<string> knownedFrames = new();
		public List<EntitySavedData> units = new();
		public List<Equipment> equipmentInventory = new ();

		public Equipment AddEquipmentToInventory ( EntityEquipmentData _data )
		{
			if (_data == null || string.IsNullOrEmpty(_data.name))
				return null;

			Equipment equipment = new(_data.name);

			return equipment;
		}

		[Serializable]
		public class Equipment
		{
			public string ID;

			public Equipment ( string _ID)
			{
				this.ID = _ID;
			}

			public T GetData<T> () where T : EntityEquipmentData
			{
				if (!GameAssets.current.equipments.ContainsKey(ID))
					return null;

				return GameAssets.current.equipments[ID] as T;
			}
		}

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
