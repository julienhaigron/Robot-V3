using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class FTUEManager : SingletonPersistant<FTUEManager>
{
	[SerializeField] private MissionDataEnumID[] m_cycle1Missions;
	public MissionDataEnumID[] Cycle1Missions => m_cycle1Missions;
	public UnitPreset[] playerStartingSquadUnits;

	[Title("MicroTuto0")]
	[SerializeField] private DialogueData[] m_firstTutoDialogues;
	
	[Title("Day1")]
	[SerializeField] private DialogueData[] m_day1TutoDialogues;

	[Title("Day2")]
	[SerializeField] private DialogueData[] m_day2TutoDialogues;

	[Title("Day3")]
	[SerializeField] private DialogueData[] m_day3TutoDialogues;

	[Title("Day4")]
	[SerializeField] private DialogueData[] m_day4TutoDialogues;

	[Title("Day5")]
	[SerializeField] private DialogueData[] m_day5TutoDialogues;

	private Dictionary<string, TutorialHighlightZone> m_registerdTutorialHighlightZones = new();
	public Dictionary<string, TutorialHighlightZone> RegisterdTutorialHighlightZones => m_registerdTutorialHighlightZones;


	public const string FTUEID = "FTUESequence";

	public void AddTutorialHighlightZone ( TutorialHighlightZone _highlightZone )
	{
		if (m_registerdTutorialHighlightZones.ContainsKey(_highlightZone.ID))
		{
			Debug.LogError("this highlightZone has the same ID has another one. ID = " + _highlightZone.ID, _highlightZone.gameObject);
			return;
		}

		m_registerdTutorialHighlightZones.Add(_highlightZone.ID, _highlightZone);
	}


	public void InitFTUE ()
	{
		if (GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(FTUEID) && GameDatas.current.currentPlayerSave.sequencesProgressions[FTUEID] == -1)
			return;

#if UNITY_EDITOR
		if (GameConfig.current.debug.skipFTUE)
			return;
#endif
		FTUESequence ftueSequence = new(FTUEID);

		ftueSequence.Append(MicroTuto0());
		ftueSequence.Append(Day1Tuto());
		ftueSequence.Append(Day2Tuto());
		ftueSequence.Append(Day3Tuto());
		ftueSequence.Append(Day4Tuto());
		ftueSequence.Append(Day5Tuto());

		ftueSequence.Start();
	}

	#region Tuto Sequences

	private TaskSequence MicroTuto0 ()
	{
		int firstPlayerEntityID = 0;

		TaskSequence tutoSequence = new("MicroTuto0");

		//input phase
		tutoSequence.Append(new DialogueTask("Select Unit", ( context ) => context.Game.CurrentMission != null && context.Game.CurrentMission.enumID == m_cycle1Missions[0]
			&& context.UI.currentPanel is InGamePanel
		, m_firstTutoDialogues[0]));
		tutoSequence.Append(new DialogueHighlightTask("Action explenation", (context) => context.Player.SelectedEntity != null
		, m_firstTutoDialogues[1], "actionBtns"));
		tutoSequence.Append(new DialogueHighlightTask("Action Queue explenation", (context) => context.Turn.RecordedActions.ContainsKey(firstPlayerEntityID) && context.Turn.RecordedActions[firstPlayerEntityID].Count > 0
		, m_firstTutoDialogues[2], "actionQueue"));
		
		//play phase
		tutoSequence.Append(new DialogueHighlightTask("Log explenation", ( context ) => context.Turn.currentPhase == TurnManager.TurnPhase.Playing
		, m_firstTutoDialogues[3], "logs"));

		//input phase
		tutoSequence.Append(new DialogueHighlightTask("State explenation", (context) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.AttackRoll)
		, m_firstTutoDialogues[4], "logs"));
		tutoSequence.Append(new DialogueHighlightTask("Attack rolls explenation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Damage)
		, m_firstTutoDialogues[5], "logs"));

		return tutoSequence;
	}

	private TaskSequence Day1Tuto ()
	{
		TaskSequence tutoSequence = new("Day1Tuto");

		//macro
		tutoSequence.Append(new OpenPanelTask<HangarPanel>("Send player directly to hangar", ( context ) => context.UI.currentPanel is SoloHubPanel 
			&& GameDatas.current.currentPlayerSave.dayCount == 1));
		tutoSequence.Append(new DialogueHighlightTask("Squad Explenation", ( context ) => context.UI.currentPanel is HangarPanel
		, m_day1TutoDialogues[0], "squadUnits"));
		tutoSequence.Append(new DialogueHighlightTask("Hub presentation", ( context ) => context.UI.currentPanel is SoloHubPanel
		, m_day1TutoDialogues[1], "missionSection"));
		tutoSequence.Append(new DialogueHighlightTask("Bla bla choisir mission", ( context ) => context.UI.currentPanel is MissionPanel
		, m_day1TutoDialogues[2], "startMissionBtn"));
		
		//micro
		tutoSequence.Append(new DialogueTask("Blabla PFC + Sous-type d’action", ( context ) => context.UI.currentPanel is InGamePanel
		, m_day1TutoDialogues[3]));
		tutoSequence.Append(new DialogueHighlightTask("Blabla statut, visible dans le log”", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Status)
		, m_day1TutoDialogues[4], "logs"));

		return tutoSequence;
	}

	private TaskSequence Day2Tuto ()
	{
		TaskSequence tutoSequence = new("Day2Tuto");

		//macro
		tutoSequence.Append(new DialogueHighlightTask("Vas dans le hangar ", ( context ) => context.UI.currentPanel is SoloHubPanel 
			&& GameDatas.current.currentPlayerSave.dayCount == 2
		, m_day2TutoDialogues[0], "hangarBtn"));
		tutoSequence.Append(new DialogueHighlightTask("tweak une unit", ( context ) => context.UI.currentPanel is HangarPanel
		, m_day2TutoDialogues[1], "squadUnit0"));

		//micro
		tutoSequence.Append(new DialogueTask("Blabla vision", ( context ) => context.UI.currentPanel is InGamePanel
		, m_day2TutoDialogues[2]));

		return tutoSequence;
	}

	private TaskSequence Day3Tuto ()
	{
		TaskSequence tutoSequence = new("Day3Tuto");

		//macro
		tutoSequence.Append(new DialogueHighlightTask("Blabla recyclage donne gold", ( context ) => context.UI.currentPanel is SoloHubPanel 
			&& GameDatas.current.currentPlayerSave.dayCount == 3
		, m_day3TutoDialogues[0], "recycleBtn"));

		//micro
		tutoSequence.Append(new DialogueTask("Blabla mort mais pas grave car réparation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Damage)
		, m_day3TutoDialogues[1]));

		return tutoSequence;
	}

	private TaskSequence Day4Tuto ()
	{
		TaskSequence tutoSequence = new("Day4Tuto");

		//macro
		tutoSequence.Append(new DialogueTask("Blabla gold", ( context ) => context.UI.currentPanel is SoloHubPanel 
			&& GameDatas.current.currentPlayerSave.dayCount == 4, m_day4TutoDialogues[0]));
		tutoSequence.Append(new DialogueTask("Blabla reparation", ( context ) => context.UI.currentPanel is SoloHubPanel, m_day4TutoDialogues[1]));

		/*//micro
		tutoSequence.Append(new DialogueTask("Blabla mort mais pas grave car réparation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Damage)
		, m_day4TutoDialogues[1]));*/

		return tutoSequence;
	}

	private TaskSequence Day5Tuto ()
	{
		TaskSequence tutoSequence = new("Day5Tuto");

		//macro
		tutoSequence.Append(new DialogueHighlightTask("Vas dans le hangar et créer une unit", ( context ) => context.UI.currentPanel is SoloHubPanel
			 && GameDatas.current.currentPlayerSave.dayCount == 5, m_day5TutoDialogues[0], "hangarBtn"));
		tutoSequence.Append(new DialogueTask("Blabla cycle + tournois", ( context ) => context.UI.currentPanel is SoloHubPanel, m_day5TutoDialogues[1]));

		/*//micro
		tutoSequence.Append(new DialogueTask("Blabla mort mais pas grave car réparation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Damage)
		, m_day4TutoDialogues[1]));*/

		return tutoSequence;
	}

	#endregion
}
