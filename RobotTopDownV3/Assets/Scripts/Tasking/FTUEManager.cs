using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class FTUEManager : SingletonPersistant<FTUEManager>
{
	[Title("Tuto1")]
	[SerializeField] private DialogueData[] m_firstTutoDialogues;

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

		ftueSequence.Append(MicroTuto1());
		//ftueSequence.Append(MicroTuto2());
		//ftueSequence.Append(MicroTuto3());
		//ftueSequence.Append(MicroTuto4());

		ftueSequence.Start();
	}

	#region Tuto Sequences

	private TaskSequence MicroTuto1 ()
	{
		int firstPlayerEntityID = 0;

		TaskSequence tutoSequence = new("MicroTuto1");

		//input phase
		tutoSequence.Append(new DialogueTask("Select Unit", ( context ) => context.Game.CurrentMission != null && context.Game.CurrentMission == GameConfig.current.game.microTuto1MissionData
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

	private TaskSequence MicroTuto2 ()
	{
		int firstPlayerEntityID = 0;

		TaskSequence tutoSequence = new("MicroTuto2");

		/*//input phase
		tutoSequence.Append(new DialogueTask("Select Unit", ( context ) => string.Equals(context.Game.CurrentMission.name, "TutoLevel1")
		, m_firstTutoDialogues[0]));
		tutoSequence.Append(new DialogueHighlightTask("Action explenation", ( context ) => string.Equals(context.Player.SelectedEntity.ID, firstPlayerEntityID)
		, m_firstTutoDialogues[1], registerdTutorialHighlightZones["actionBtns"]));
		tutoSequence.Append(new DialogueHighlightTask("Action Queue explenation", ( context ) => context.Turn.RecordedActions.ContainsKey(firstPlayerEntityID) && context.Turn.RecordedActions[firstPlayerEntityID].Count > 0
		, m_firstTutoDialogues[2], registerdTutorialHighlightZones["actionQueue"]));

		//play phase
		tutoSequence.Append(new DialogueHighlightTask("Log explenation", ( context ) => context.Turn.currentPhase == TurnManager.TurnPhase.Playing
		, m_firstTutoDialogues[3], registerdTutorialHighlightZones["logs"]));

		//input phase
		tutoSequence.Append(new DialogueHighlightTask("State explenation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.AttackRoll)
		, m_firstTutoDialogues[4], registerdTutorialHighlightZones["stateBtns"]));
		tutoSequence.Append(new DialogueHighlightTask("Attack rolls explenation", ( context ) => context.Log.Logs.ContainsKey(LogConsole.LogEventType.Damage)
		, m_firstTutoDialogues[5], registerdTutorialHighlightZones["stateBtns"]));*/

		return tutoSequence;
	}

	#endregion
}
