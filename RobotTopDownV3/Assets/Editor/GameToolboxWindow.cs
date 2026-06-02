using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameToolboxWindow : EditorWindow
{

	[MenuItem("Tools/Game Toolbox")]
	public static void LoadWindows ()
	{
		GetWindow<GameToolboxWindow>("Game Toolbox");
	}

	private void OnGUI ()
	{
		Parsing();
	}


	private void Parsing ()
	{
		GUILayoutOption group = GUILayout.Height(30f);

		StartBox("Parsing");

		EditorGUILayout.BeginVertical(group);

		if (GUILayout.Button("Parse all components", group))
		{
			CsvImporter.ImportFromUrl<FrameEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Frame])
				, "Assets/Objects/Component/Final/Frame");
			CsvImporter.ImportFromUrl<ReactorEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Reactor])
				, "Assets/Objects/Component/Final/Reactor");
			CsvImporter.ImportFromUrl<BrainEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Brain])
				, "Assets/Objects/Component/Final/Brain");
			CsvImporter.ImportFromUrl<WeaponEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Weapon])
				, "Assets/Objects/Component/Final/Weapon");
			CsvImporter.ImportFromUrl<NeuronalMembraneEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.NeuronalMembrane])
				, "Assets/Objects/Component/Final/NeuronalMembrane");
			CsvImporter.ImportFromUrl<ToolEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Tool])
				, "Assets/Objects/Component/Final/Tool");
			CsvImporter.ImportFromUrl<ArmorEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Armor])
				, "Assets/Objects/Component/Final/Armor");
			CsvImporter.ImportFromUrl<OccultorEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Occultor])
				, "Assets/Objects/Component/Final/Occultor");
			CsvImporter.ImportFromUrl<ChipsetEquipmentData>(MakeUrl(GameConfig.current.parsing.componentsSpreadSheetID, GameConfig.current.parsing.componentGUIDPerPage[EntityEquipmentData.EquipmentType.Chipset])
				, "Assets/Objects/Component/Final/Chipset");
		}
		EditorGUILayout.EndVertical();

		EndBox();

	}

	private string MakeUrl(string _spreadsheetID, string _sheetName )
	{
		return "https://docs.google.com/spreadsheets/d/" + _spreadsheetID + "/export?format=csv&gid=" + _sheetName;
	}

	#region window visual

	private void StartBox ( string _label/*, string _icon */)
	{
		EditorGUILayout.BeginVertical(GUI.skin.box);
		EditorGUILayout.BeginHorizontal(GUI.skin.box);
		//EditorGUILayout.LabelField(new GUIContent(_label, EditorGUIUtility.FindTexture(_icon)), EditorStyles.boldLabel);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space(5);
	}

	private void EndBox ()
	{
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space(10);
	}

	private void Title ( string _text )
	{
		EditorGUILayout.LabelField(_text);
	}

	private void SetSelection ( GameObject _target )
	{
		Selection.activeObject = _target;
	}

	#endregion
}
