using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameToolboxWindow : EditorWindow
{

	private int m_missingUnitCount;
	private int m_missingActionCount;
	private int m_missingComponentCount;


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

		if (GUILayout.Button("Parse all Actions", group))
		{
			CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
				, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.Movement]), "Assets/Objects/Actions/Final/Movement");
			CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
				, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.DistanceAttack]), "Assets/Objects/Actions/Final/Tir");
			CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
				, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.MeleeAttack]), "Assets/Objects/Actions/Final/Melee");
			CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
				, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.Special]), "Assets/Objects/Actions/Final/Special");
		}

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

		if(GUILayout.Button("Parse all Units", group))
		{
			CsvImporter.ImportFromUrl<UnitPreset>(MakeUrl(GameConfig.current.parsing.unitSpreadSheetID, "0")
				, "Assets/Objects/UnitPreset/Final");
		}
		EditorGUILayout.EndVertical();

		EndBox();

		if (GUILayout.Button("Check misssing data", group))
			CheckMissingData();

	}

	private void CheckMissingData ()
	{
		//m_missingUnitCount = GameAssets.current.game.uni
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
