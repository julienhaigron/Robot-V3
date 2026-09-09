using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameToolboxWindow : EditorWindow
{

	private int m_missingUnitCount;
	private int m_missingActionCount;
	private int m_missingComponentCount;

	private const float SoftLockWarningDelay = 5f;
	private Vector2 m_scroll;

	[MenuItem("Tools/Game Toolbox")]
	public static void LoadWindows ()
	{
		GetWindow<GameToolboxWindow>("Game Toolbox");
	}

	private void Update ()
	{
		if (Application.isPlaying)
			Repaint();
	}

	private void OnGUI ()
	{
		m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
		TickWatch();
		Parsing();
		EditorGUILayout.EndScrollView();
	}

	private void TickWatch ()
	{
		StartBox("Tick watch");

		if (!Application.isPlaying || TurnManager.Instance == null)
		{
			EditorGUILayout.HelpBox("Enter play mode in a level to watch running actions and events.", MessageType.None);
			EndBox();
			return;
		}

		TurnManager turn = TurnManager.Instance;
		float elapsed = turn.CurrentTickElapsedTime;

		EditorGUILayout.LabelField($"Phase {turn.currentPhase}   Round {turn.RoundCount}   Tick {TurnManager.currentTick}"
			+ (turn.currentPhase == TurnManager.TurnPhase.Playing ? $"   running for {elapsed:0.0}s" : ""), EditorStyles.boldLabel);

		List<string> pending = new();
		DrawActionsBeingDone(turn, pending);
		DrawInPlayEvents(turn, pending);

		if (turn.currentPhase == TurnManager.TurnPhase.Playing && elapsed > SoftLockWarningDelay)
		{
			EditorGUILayout.HelpBox(pending.Count > 0
				? $"Tick has been running for {elapsed:0.0}s, still waiting on:\n- " + string.Join("\n- ", pending)
				: $"Tick has been running for {elapsed:0.0}s with nothing pending - TryEndRoundTick was never re-entered."
				, MessageType.Warning);
		}

		EndBox();
	}

	private void DrawActionsBeingDone ( TurnManager _turn, List<string> _pending )
	{
		List<string> lines = new();

		foreach (KeyValuePair<int, Tuple<TurnManager.RecordedAction, bool>> pair in _turn.ActionsBeingDone)
		{
			if (pair.Value == null || pair.Value.Item1 == null)
				continue;

			TurnManager.RecordedAction recordedAction = pair.Value.Item1;
			bool isDone = pair.Value.Item2;
			AEntityAction performingAction = GetPerformingAction(recordedAction);

			if (performingAction == null)
			{
				if (!isDone)
				{
					string idleLabel = $"[{GetEntityName(pair.Key)}] nothing performing, not reported done"
						+ $"  (planned {DescribeAction(recordedAction.action)})";
					lines.Add(idleLabel);
					_pending.Add(idleLabel);
				}
				continue;
			}

			if (performingAction.enumID == EntityActionEnumID.Wait)
			{
				if (!isDone)
					_pending.Add($"[{GetEntityName(pair.Key)}] {DescribeAction(performingAction)}  RUNNING");

				continue;
			}

			string label = $"[{GetEntityName(pair.Key)}] {DescribeAction(performingAction)}"
				+ $"  state {recordedAction.entityState}"
				+ (performingAction.wasReplacedByAI ? "  (AI swapped)" : "")
				+ (isDone ? "  DONE" : "  RUNNING");

			lines.Add(label);

			if (!isDone)
				_pending.Add(label);
		}

		EditorGUILayout.LabelField($"Actions playing ({lines.Count})");
		EditorGUI.indentLevel++;
		foreach (string line in lines)
			EditorGUILayout.LabelField(line);
		EditorGUI.indentLevel--;
	}

	private AEntityAction GetPerformingAction ( TurnManager.RecordedAction _recordedAction )
	{
		if (_recordedAction.freeAction != null && _recordedAction.freeAction.IsPerforming)
			return _recordedAction.freeAction;

		return _recordedAction.action != null && _recordedAction.action.IsPerforming ? _recordedAction.action : null;
	}

	private string DescribeAction ( AEntityAction _action )
	{
		if (_action == null)
			return "<null>";

		return $"{_action.Data.name} lifetime {_action.lifetime}/{_action.TotalDuration} ticks {_action.timeAtStart}-{_action.TimeAtEnd}";
	}

	private void DrawInPlayEvents ( TurnManager _turn, List<string> _pending )
	{
		EditorGUILayout.LabelField($"In play events ({_turn.InPlayEvents.Count})");
		EditorGUI.indentLevel++;

		foreach (TurnManager.RecordedEvent gameEvent in _turn.InPlayEvents)
		{
			if (gameEvent == null)
				continue;

			string label = $"{gameEvent.label}  started tick {gameEvent.startTick}, {Time.time - gameEvent.startTime:0.0}s ago";
			EditorGUILayout.LabelField(label);
			_pending.Add(label);
		}

		EditorGUI.indentLevel--;
	}

	private string GetEntityName ( int _entityID )
	{
		if (GameManager.Instance == null)
			return "entity " + _entityID;

		Entity entity = GameManager.Instance.GetEntityFromID(_entityID);
		return entity == null || entity.Data == null ? "entity " + _entityID : entity.Data.name;
	}


	private void Parsing ()
	{
		GUILayoutOption group = GUILayout.Height(30f);

		StartBox("Parsing");

		EditorGUILayout.BeginVertical(group);

		if (GUILayout.Button("Parse all Actions", group))
		{
			ParseAllActions();
			GenerateAllEnums();
			GameAssets.current.ReloadAll();
		}

		if (GUILayout.Button("Parse all Components", group))
		{
			ParseAllComponents();
			GameAssets.current.ReloadAll();
		}

		if(GUILayout.Button("Parse all Units", group))
		{
			ParseAllUnits();
		}
		EditorGUILayout.EndVertical();

		EndBox();

		StartBox("Micro");

		EditorGUILayout.BeginVertical(group);
		if (GUILayout.Button("Force Win", group))
		{
			TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Win);
		}
		if (GUILayout.Button("Force Draw", group))
		{
			TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Draw);
		}
		if (GUILayout.Button("Force Loose", group))
		{
			TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Loose);
		}
		if (GUILayout.Button("Force kill all ally units and Loose", group))
		{
			foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities)
				entity.Equipment.InstantDeath();

			TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Loose);
		}
		EditorGUILayout.EndVertical();

		EndBox();


		/*if (GUILayout.Button("Check misssing data", group))
			CheckMissingData();

		if (GUILayout.Button("Load new assets into GameAssets"))
		{
			GenerateAllEnums();
			GameAssets.current.ReloadAll();
		}

		if(GUILayout.Button("Parse all others"))
		{
			ParseAllActions();
			GenerateAllEnums();
			ParseAllComponents();
			ParseAllUnits();
			GameAssets.current.ReloadAll();
		}*/

	}

	private void ParseAllActions ()
	{
		CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
				, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.Movement]), "Assets/Objects/Actions/Final/Movement");
		CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
			, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.DistanceAttack]), "Assets/Objects/Actions/Final/Tir");
		CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
			, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.MeleeAttack]), "Assets/Objects/Actions/Final/Melee");
		CsvImporter.ImportFromUrl<EntityActionData>(MakeUrl(GameConfig.current.parsing.actionSpreadSheetID
			, GameConfig.current.parsing.actionGUIDPerPage[EntityActionData.ActionType.Special]), "Assets/Objects/Actions/Final/Special");
		Debug.Log("Action parsing initiated");
	}

	private void ParseAllComponents ()
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
		Debug.Log("Components parsing initiated");
	}

	private void ParseAllUnits ()
	{
		CsvImporter.ImportFromUrl<UnitPreset>(MakeUrl(GameConfig.current.parsing.unitSpreadSheetID, "0")
				, "Assets/Objects/UnitPreset/Final");
		Debug.Log("Unit parsing initiated");
	}

	private void CheckMissingData ()
	{
		//m_missingUnitCount = GameAssets.current.game.uni
	}

	private void GenerateAllEnums ()
	{
		EntityActionDataEditor.ComputeEnum();
		ScriptableEnumAutoEditor.GenerateAllEnums();
		Debug.Log("Parsed all new auto generated enums");
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
