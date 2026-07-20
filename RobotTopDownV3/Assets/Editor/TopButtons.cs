using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

static class ToolbarStyles
{
	public static readonly GUIStyle pickedItemCommandButtonStyle;
	public static readonly GUIStyle commandButtonStyle;
	public static readonly GUIStyle tinyCommandButtonStyle;
	public static readonly GUIStyle longCommandButtonStyle;
	public static readonly GUIStyle veryLongCommandButtonStyle;
	public static readonly GUIStyle ToggleStyle;

	public static readonly float topSpace = 1f;

	static ToolbarStyles ()
	{
		pickedItemCommandButtonStyle = new GUIStyle("ToolbarButton")
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fixedWidth = 90,
			fixedHeight = 18,
		};

		commandButtonStyle = new GUIStyle("ToolbarButton")
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fixedWidth = 47,
			fixedHeight = 18,
		};

		tinyCommandButtonStyle = new GUIStyle("ToolbarButton")
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fixedWidth = 24,
			fixedHeight = 18,
		};

		longCommandButtonStyle = new GUIStyle("ToolbarButton")
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fixedWidth = 70,
			fixedHeight = 18,
		};

		veryLongCommandButtonStyle = new GUIStyle("ToolbarButton")
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fixedWidth = 140,
			fixedHeight = 18,
		};

		ToggleStyle = new GUIStyle(GUI.skin.toggle)
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fixedWidth = 90,
			fixedHeight = 14,
			padding = new RectOffset(10, -10, -2, 0),
		};
	}
}


[InitializeOnLoad]
public class TopButtons
{
	static TopButtons ()
	{
		ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUILeft);
		ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUIRight);
	}

	static void OnToolbarGUILeft ()
	{
		GUILayout.FlexibleSpace();

		GUIContent content = new GUIContent();

		GUILayout.BeginVertical();
		GUILayout.Space(ToolbarStyles.topSpace);
		GUILayout.BeginHorizontal();

		string savedObjectID = EditorPrefs.GetString("SelectedObjectID");
		bool isSceneObject = EditorPrefs.GetBool("IsSceneObject");

		string activeObjectName = "None";
		Object activeObj = null;
		if (!string.IsNullOrEmpty(savedObjectID))
		{
			if (isSceneObject)
			{
				activeObj = EditorUtility.EntityIdToObject(int.Parse(savedObjectID));
				if (activeObj == null)
				{
					activeObj = GameObject.Find(EditorPrefs.GetString("SelectedObjectName"));
				}
			}
			else
				activeObj = AssetDatabase.LoadAssetAtPath<Object>(savedObjectID);
		}
		if (activeObj != null)
		{
			activeObjectName = activeObj.name;
			if (activeObjectName.Length > 10)
			{
				activeObjectName = activeObjectName.Substring(0, 9) + "...";
			}
		}
		Color tmp = GUI.backgroundColor;
		if (activeObj != null && isSceneObject)
		{
			GUI.backgroundColor = Color.black;
		}
		if (GUILayout.Button(new GUIContent(activeObjectName, "Select Pinned Object"), ToolbarStyles.pickedItemCommandButtonStyle))
		{
			if (activeObj != null)
			{
				if (!isSceneObject)
				{
					Selection.activeObject = activeObj;
					EditorGUIUtility.PingObject(Selection.activeObject);
				}
				else
				{
					Selection.activeObject = activeObj;
				}
			}
		}
		GUI.backgroundColor = tmp;
		if (GUILayout.Button(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover@2x"), ToolbarStyles.tinyCommandButtonStyle))
		{
			var activeGO = Selection.activeGameObject;
			if (activeGO != null && activeGO.IsSceneBound())
			{
				EditorPrefs.SetString("SelectedObjectID", activeGO.gameObject.GetInstanceID().ToString());
				EditorPrefs.SetString("SelectedObjectName", activeGO.gameObject.name);
				EditorPrefs.SetBool("IsSceneObject", true);
			}
			else
			{
				var selectedAsset = Selection.activeObject;
				if (selectedAsset != null)
				{
					EditorPrefs.SetString("SelectedObjectID", AssetDatabase.GetAssetPath(selectedAsset));
					EditorPrefs.SetBool("IsSceneObject", false);
				}
			}
		}

		GUILayout.Space(16f);

		if (GUILayout.Button(new GUIContent("Assets", "Select Game Assets"), ToolbarStyles.commandButtonStyle))
		{
			UnityEditor.Selection.activeObject = Resources.Load<GameAssets>("GameAssets");
			UnityEditor.EditorGUIUtility.PingObject(UnityEditor.Selection.activeObject);
		}

		if (GUILayout.Button(new GUIContent("Config", "Select Game Configs"), ToolbarStyles.commandButtonStyle))
		{
			UnityEditor.Selection.activeObject = Resources.Load<GameConfig>("GameConfig");
			UnityEditor.EditorGUIUtility.PingObject(UnityEditor.Selection.activeObject);
		}

		if (GUILayout.Button(new GUIContent("Datas", "Select Game Data"), ToolbarStyles.commandButtonStyle))
		{
			UnityEditor.Selection.activeObject = Resources.Load<GameDatas>("GameDatas");
			UnityEditor.EditorGUIUtility.PingObject(UnityEditor.Selection.activeObject);
		}

		if (GUILayout.Button(new GUIContent("UI", "Select SafeArea"), ToolbarStyles.commandButtonStyle))
		{
			GameObject safeArea = null;

			if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) //is in prefab
			{
				safeArea = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().FindComponentOfType<UIManager>().gameObject;
			}
			else //in Scene
			{
				safeArea = GameObject.Find("UIManager");
			}

			if (safeArea != null)
			{
				EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
				Selection.objects = new Object[] { safeArea };

				SandolkakosDigital.EditorUtils.SceneHierarchyUtility.SetExpanded(safeArea, true);

			}
			else //no safeArea found
			{
				string guid = AssetDatabase.FindAssets("UIManager")[0];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				UnityEditor.Selection.activeObject = AssetDatabase.LoadAssetAtPath<UIManager>(path);
				UnityEditor.EditorGUIUtility.PingObject(UnityEditor.Selection.activeObject);
			}
		}

		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	static void OnToolbarGUIRight ()
	{
		GUILayout.BeginVertical();
		GUILayout.Space(ToolbarStyles.topSpace);
		GUILayout.BeginHorizontal();

		if (Application.isPlaying)
		{
			//Read only
			GUI.enabled = false;
		}
		EditorGUI.BeginChangeCheck();

		if (GUILayout.Button(new GUIContent("Clear Data", "Clear Game Data"), ToolbarStyles.longCommandButtonStyle) && EditorUtility.DisplayDialog("ClearData", "Clear Game Data?", "Yes", "No"))
		{
			GameDatas.Clear();
		}

		GameDatas data = Resources.Load<GameDatas>("GameDatas");
		data.preventSave = GUILayout.Toggle(data.preventSave, new GUIContent("Prevent Save"), ToolbarStyles.ToggleStyle);

		if (EditorGUI.EndChangeCheck())
			EditorUtility.SetDirty(data);

		GUI.enabled = true;

		/*GUILayout.Space(25);
		if (GUILayout.Button(new GUIContent("Game Toolbox", "Open Game Toolbox window"), ToolbarStyles.veryLongCommandButtonStyle))
		{
			EditorWindow.GetWindow<GameToolboxWindow>("Game Toolbox");
		}*/

		GUILayout.FlexibleSpace();

		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
}