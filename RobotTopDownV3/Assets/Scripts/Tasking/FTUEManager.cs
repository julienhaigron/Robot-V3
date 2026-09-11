using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class FTUEManager : Singleton<FTUEManager>
{
	[SerializeField] private MissionData m_day0MissionData;
	public MissionData Day0MissionData => m_day0MissionData;
	[SerializeField] private MissionData[] m_cycle1Missions;
	public MissionData[] Cycle1MatchMissions => m_cycle1Missions;
	[SerializeField] private MissionData[] m_cycle1TournamentMissions;
	public MissionData[] Cycle1TournamentMissions => m_cycle1TournamentMissions;
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
	private bool m_isInit = false;
	public bool IsInit => m_isInit;

	public bool IsFTUERunning
	{
		get
		{
#if UNITY_EDITOR
			if (GameConfig.current.debug.skipFTUE)
				return false;
#endif
			return !GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(FTUEID)
				|| GameDatas.current.currentPlayerSave.sequencesProgressions[FTUEID] != -1;
		}
	}

	public void AddTutorialHighlightZone ( TutorialHighlightZone _highlightZone )
	{
		if (_highlightZone == null || string.IsNullOrEmpty(_highlightZone.ID))
			return;

		if (m_registerdTutorialHighlightZones.TryGetValue(_highlightZone.ID, out TutorialHighlightZone registeredZone))
		{
			if (registeredZone == _highlightZone)
				return;

			if (registeredZone != null)
			{
				Debug.LogError("this highlightZone has the same ID has another one. ID = " + _highlightZone.ID, _highlightZone.gameObject);
				return;
			}
		}

		m_registerdTutorialHighlightZones[_highlightZone.ID] = _highlightZone;
	}

	public bool TryGetTutorialHighlightZone ( string _id, out TutorialHighlightZone _zone )
	{
		_zone = null;

		if (string.IsNullOrEmpty(_id))
			return false;

		if (m_registerdTutorialHighlightZones.TryGetValue(_id, out _zone) && _zone != null)
			return true;

		RefreshTutorialHighlightZones();
		return m_registerdTutorialHighlightZones.TryGetValue(_id, out _zone) && _zone != null;
	}

	public void RefreshTutorialHighlightZones ()
	{
		foreach (TutorialHighlightZone zone in FindObjectsByType<TutorialHighlightZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			AddTutorialHighlightZone(zone);
	}


	public void InitFTUE ()
	{
		if (GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(FTUEID) && GameDatas.current.currentPlayerSave.sequencesProgressions[FTUEID] == -1
			|| m_isInit)
			return;

#if UNITY_EDITOR
		if (GameConfig.current.debug.skipFTUE)
			return;
#endif
		m_isInit = true;
		FTUESequence ftueSequence = new(FTUEID);

		ftueSequence.Append(MicroTuto0());
		ftueSequence.Append(Day1Tuto());
		ftueSequence.Append(Day2Tuto());
		ftueSequence.Append(Day3Tuto());
		ftueSequence.Append(Day4Tuto());
		ftueSequence.Append(Day5Tuto());
		ftueSequence.Append(Cycle1Tuto());

		ftueSequence.Start();
	}

	public void ForceFinishFTUE ()
	{
		if (GameDatas.current.currentPlayerSave.sequencesProgressions[FTUEID] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence(FTUEID);

		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("MicroTuto0") || GameDatas.current.currentPlayerSave.sequencesProgressions["MicroTuto0"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("MicroTuto0");
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("Day1Tuto") || GameDatas.current.currentPlayerSave.sequencesProgressions["Day1Tuto"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("Day1Tuto");
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("Day2Tuto") || GameDatas.current.currentPlayerSave.sequencesProgressions["Day2Tuto"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("Day2Tuto");
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("Day3Tuto") || GameDatas.current.currentPlayerSave.sequencesProgressions["Day3Tuto"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("Day3Tuto");
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("Day4Tuto") || GameDatas.current.currentPlayerSave.sequencesProgressions["Day4Tuto"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("Day4Tuto");
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("Day5Tuto") || GameDatas.current.currentPlayerSave.sequencesProgressions["Day5Tuto"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("Day5Tuto");
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey("Cycle1Tuto") || GameDatas.current.currentPlayerSave.sequencesProgressions["Cycle1Tuto"] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence("Cycle1Tuto");

		if (GameDatas.current.currentPlayerSave.sequencesProgressions[FTUEID] != -1)
			TaskManager.Instance.StopAndMarkAsCompletedSequence(FTUEID);

		GameDatas.current.currentPlayerSave.didUnlockRecycler = true;
		GameDatas.current.currentPlayerSave.didUnlockRepareStation = true;
		GameDatas.current.currentPlayerSave.didUnlockShops = true;
		GameDatas.current.currentPlayerSave.didUnlockReturnToHubPopup = true;
		GameDatas.current.currentPlayerSave.didStartTuto = true;
	}

	#region Tuto Sequences

	private TaskSequence Cycle1Tuto ()
	{
		TaskSequence tutoSequence = new("Cycle1Tuto");

		tutoSequence.Append(new ManualTask("Unlock shops", ( context ) => GameDatas.current.currentPlayerSave.cycleCount >= 1
			, () => GameDatas.current.currentPlayerSave.didUnlockShops = true));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.didUnlockShops);
		return tutoSequence;
	}

	private TaskSequence MicroTuto0 ()
	{
		int firstPlayerEntityID = 0;

		TaskSequence tutoSequence = new("MicroTuto0");

		//input phase
		tutoSequence.Append(new DialogueHighlightTask("Select Unit", ( context ) => context.Game.CurrentMission != null && context.Game.CurrentMission.enumID == m_day0MissionData.enumID
			&& context.UI.currentPanel is InGamePanel
		, m_firstTutoDialogues[0], "squadUnitsMicroBtn0"));
		//tutoSequence.Append(new SelectEntityTask("Select Entity", null, -1));
		tutoSequence.Append(new DialogueHighlightTask("Action explenation", null, m_firstTutoDialogues[1], "actionBtns"));
		tutoSequence.Append(new DialogueHighlightTask("Action Queue explenation", ( context ) => context.Turn.RecordedActions.ContainsKey(firstPlayerEntityID) && context.Turn.RecordedActions[firstPlayerEntityID].Count > 0
		, m_firstTutoDialogues[2], "actionQueue"));

		//play phase
		tutoSequence.Append(new DialogueHighlightTask("Log explenation", ( context ) => context.Turn.currentPhase == TurnManager.TurnPhase.Playing
		, m_firstTutoDialogues[3], "logs"));

		//input phase
		tutoSequence.Append(new WalkOnTileTask("Wait for unit to walk on trigger tile", null, TileGroundType.Trigger));
		tutoSequence.Append(new DialogueHighlightTask("State explenation", null, m_firstTutoDialogues[4], "stateButtons"));
		tutoSequence.Append(new DialogueHighlightTask("State modification explenation", null, m_firstTutoDialogues[5], "stateLines"));
		
		tutoSequence.Append(new HighlightLogTask("Highlight attack roll logs", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.AttackRoll)
		, LogConsole.LogEventType.AttackRoll, true));
		tutoSequence.Append(new DialogueHighlightTask("Attack roll explenation", null, m_firstTutoDialogues[6], "logs"));
		tutoSequence.Append(new HighlightLogTask("Stop highlighting attack roll logs", null, LogConsole.LogEventType.AttackRoll, false));

		tutoSequence.Append(new HighlightLogTask("Highlight damage logs", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Damage)
		, LogConsole.LogEventType.Damage, true));
		tutoSequence.Append(new DialogueHighlightTask("Damage explenation", null, m_firstTutoDialogues[7], "logs"));
		tutoSequence.Append(new HighlightLogTask("Stop highlighting damage logs", null, LogConsole.LogEventType.Damage, false));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.didStartTuto && GameDatas.current.currentPlayerSave.dayCount >= 0);
		return tutoSequence;
	}

	private TaskSequence Day1Tuto ()
	{
		TaskSequence tutoSequence = new("Day1Tuto");

		//macro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => GameDatas.current.currentPlayerSave.dayCount == 0));
		tutoSequence.Append(new OpenPanelTask<HangarPanel>("Send player directly to hangar", ( context ) => context.UI.currentPanel is SoloHubPanel
			&& GameDatas.current.currentPlayerSave.dayCount == 0));
		tutoSequence.Append(new DialogueHighlightTask("New unit won explenation", ( context ) => context.UI.currentPanel is HangarPanel
		, m_day1TutoDialogues[0], "squadUnit2").SetSkipPredicate(( context ) => context.UI.currentPanel is SoloHubPanel));
		tutoSequence.Append(new DialogueHighlightTask("Hub presentation", ( context ) => context.UI.currentPanel is SoloHubPanel
		, m_day1TutoDialogues[1], "missionSection"));
		tutoSequence.Append(new DialogueHighlightTask("Available matches explenation", ( context ) => context.UI.currentPanel is MissionPanel
		, m_day1TutoDialogues[2], "missionSelectionBtn"));
		tutoSequence.Append(new DialogueHighlightTask("Start match btn explenation", null, m_day1TutoDialogues[3], "startMissionBtn"));

		//micro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => context.Game.CurrentMission != null && context.Game.CurrentMission.enumID == MissionDataEnumID.Day1Tuto));
		tutoSequence.Append(new DialogueTask("Action types explenation", ( context ) => context.UI.currentPanel is InGamePanel, m_day1TutoDialogues[4]));
		tutoSequence.Append(new DialogueHighlightTask("Movement actions explenation", null, m_day1TutoDialogues[5], "actionBtns"));
		tutoSequence.Append(new DialogueHighlightTask("Distance attack actions explenation", null, m_day1TutoDialogues[6], "actionBtns"));
		tutoSequence.Append(new DialogueHighlightTask("Melee attack actions explenation", null, m_day1TutoDialogues[7], "actionBtns"));
		tutoSequence.Append(new DialogueHighlightTask("Special actions explenation", null, m_day1TutoDialogues[8], "actionBtns"));
		tutoSequence.Append(new DialogueHighlightTask("Status roll explenation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Status)
		, m_day1TutoDialogues[9], "logs"));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.dayCount > 0);
		return tutoSequence;
	}

	private TaskSequence Day2Tuto ()
	{
		TaskSequence tutoSequence = new("Day2Tuto");

		//macro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => GameDatas.current.currentPlayerSave.dayCount == 1));
		tutoSequence.Append(new DialogueHighlightTask("Go to hangar", ( context ) => context.UI.currentPanel is SoloHubPanel
		, m_day2TutoDialogues[0], "hangarBtn"));
		tutoSequence.Append(new DialogueHighlightTask("Go to workshop", ( context ) => context.UI.currentPanel is HangarPanel
		, m_day2TutoDialogues[1], "squadUnit2"));
		tutoSequence.Append(new DialogueTask("Unit composition explenation", ( context ) => context.UI.currentPanel is EntityConfigPanel
		, m_day2TutoDialogues[2]).SetSkipPredicate(( context ) => context.UI.currentPanel is InGamePanel));
		tutoSequence.Append(new DialogueTask("Equip neuronal membranes", null
		, m_day2TutoDialogues[3]).SetSkipPredicate(( context ) => context.UI.currentPanel is InGamePanel));

		//micro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => context.Game.CurrentMission != null && context.Game.CurrentMission.enumID == MissionDataEnumID.Day2Tuto));
		tutoSequence.Append(new DialogueTask("Perception types explenation", ( context ) => context.UI.currentPanel is InGamePanel
		, m_day2TutoDialogues[4]));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.dayCount > 1);
		return tutoSequence;
	}

	private TaskSequence Day3Tuto ()
	{
		TaskSequence tutoSequence = new("Day3Tuto");

		//macro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => GameDatas.current.currentPlayerSave.dayCount == 2));
		tutoSequence.Append(new ManualTask("Unlock recycler", null, () =>
		{
			GameDatas.current.currentPlayerSave.didUnlockRecycler = true;
			GameDatas.current.currentPlayerSave.didUnlockReturnToHubPopup = true;
		}));
		tutoSequence.Append(new DialogueHighlightTask("Go to hangar", ( context ) => context.UI.currentPanel is SoloHubPanel
		, m_day3TutoDialogues[0], "hangarBtn"));
		tutoSequence.Append(new DialogueHighlightTask("Go to workshop", ( context ) => context.UI.currentPanel is HangarPanel
		, m_day3TutoDialogues[1], "squadUnit0"));
		tutoSequence.Append(new DialogueTask("Secondary components explenation", ( context ) => context.UI.currentPanel is EntityConfigPanel
		, m_day3TutoDialogues[2]).SetSkipPredicate(( context ) => context.UI.currentPanel is InGamePanel));
		tutoSequence.Append(new DialogueHighlightTask("Recycling explenation", ( context ) => context.UI.currentPanel is SoloHubPanel
		, m_day3TutoDialogues[3], "recycleBtn"));
		tutoSequence.Append(new DialogueTask("Recycling station explenation", ( context ) => context.UI.currentPanel is RecyclePanel
		, m_day3TutoDialogues[4]).SetSkipPredicate(( context ) => context.UI.currentPanel is InGamePanel));

		//micro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => context.Game.CurrentMission != null && context.Game.CurrentMission.enumID == MissionDataEnumID.Day3Tuto));
		tutoSequence.Append(new DialogueTask("Nemesis presentation", ( context ) => context.UI.currentPanel is InGamePanel, m_day3TutoDialogues[5]));
		tutoSequence.Append(new DialogueTask("Unit is gonna die to doom status", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Status)
		, m_day3TutoDialogues[6]));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.dayCount > 2);
		return tutoSequence;
	}

	private TaskSequence Day4Tuto ()
	{
		TaskSequence tutoSequence = new("Day4Tuto");

		//macro
		tutoSequence.Append(new WaitEndLoadingTask("Wait for end loading", ( context ) => GameDatas.current.currentPlayerSave.dayCount == 3));
		tutoSequence.Append(new ManualTask("Force finish recycling", null, () => GameDatas.current.currentPlayerSave.ForceFinishRecycling()));
		tutoSequence.Append(new ManualTask("Unlock repare station", null, () => GameDatas.current.currentPlayerSave.didUnlockRepareStation = true));
		tutoSequence.Append(new DialogueHighlightTask("Goto repair station", ( context ) => context.UI.currentPanel is SoloHubPanel
		, m_day4TutoDialogues[0], "repareBtn"));
		tutoSequence.Append(new DialogueTask("Repair station explenation", ( context ) => context.UI.currentPanel is RepairStationPanel
			&& GameDatas.current.currentPlayerSave.dayCount == 3, m_day4TutoDialogues[1]));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.dayCount > 3);
		return tutoSequence;
	}

	private TaskSequence Day5Tuto ()
	{
		TaskSequence tutoSequence = new("Day5Tuto");

		//macro
		tutoSequence.Append(new DialogueHighlightTask("Go to hangar to create a unit", ( context ) => context.UI.currentPanel is SoloHubPanel
			&& GameDatas.current.currentPlayerSave.dayCount == 4, m_day5TutoDialogues[0], "hangarBtn"));
		tutoSequence.Append(new DialogueHighlightTask("Create unit btn explenation", ( context ) => context.UI.currentPanel is HangarPanel, m_day5TutoDialogues[1], "createUnitBtn"));
		tutoSequence.Append(new DialogueTask("Core components explenation", ( context ) => context.UI.currentPanel is EntityConfigPanel panel && panel.IsNewUnit, m_day5TutoDialogues[2]));
		tutoSequence.Append(new DialogueTask("Cycle explenation", ( context ) => context.UI.currentPanel is SoloHubPanel, m_day5TutoDialogues[3]));
		tutoSequence.Append(new DialogueTask("Skip btn explenation", null, m_day5TutoDialogues[4]));
		tutoSequence.Append(new DialogueHighlightTask("Tournament btn explenation", null, m_day5TutoDialogues[5], "missionSection"));
		tutoSequence.Append(new DialogueTask("Tournament squad warning", ( context ) => context.UI.currentPanel is TournamentPanel, m_day5TutoDialogues[6]));

		tutoSequence.SetSkipPredicate(( context ) => GameDatas.current.currentPlayerSave.dayCount > 4);
		return tutoSequence;
	}

	#endregion
}
