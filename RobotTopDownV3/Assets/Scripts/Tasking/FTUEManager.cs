using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FTUEManager : SingletonPersistant<FTUEManager>
{
	[SerializeField] private DialogueData m_firstTutoDialogue;

	private Dictionary<string, TutorialHighlightZone> registerdTutorialHighlightZones = new();

	public override void Awake ()
	{
		base.Awake();
		UIManager.onFocusedWindowChanged += OnFocusedWindowChanged;
		GameManager.onStartLevel += OnStartLevel;
	}

	private void OnDestroy ()
	{
		UIManager.onFocusedWindowChanged -= OnFocusedWindowChanged;
		GameManager.onStartLevel -= OnStartLevel;
	}

	private void OnFocusedWindowChanged ()
	{
		CheckTutos();
	}

	private void OnStartLevel ()
	{
		CheckTutos();
	}

	public void AddTutorialHighlightZone ( TutorialHighlightZone _highlightZone )
	{
		if (registerdTutorialHighlightZones.ContainsKey(_highlightZone.ID))
		{
			Debug.LogError("this highlightZone has the same ID has another one. ID = " + _highlightZone.ID, _highlightZone.gameObject);
			return;
		}

		registerdTutorialHighlightZones.Add(_highlightZone.ID, _highlightZone);
	}


	private void CheckTutos ()
	{
		if (!GameDatas.current.currentPlayerSave.IsInTuto)
			return;

		MicroTuto1();
		MicroTuto2();
		MicroTuto3();
		MacroTuto1();
		MacroTuto2();
		MacroTuto3();
	}

	#region Tutos

	private void MicroTuto1 ()
	{
		if (!string.Equals(GameManager.Instance.CurrentMission.name, "TutoLevel1")
			|| GameDatas.current.currentPlayerSave.tutoProgression[0])
			return;

		/*
		 * - play dialogue first game introduction prt1
		 *   - mise en situation micro (InGamePanel hidden for this part)
		 *   - selectionné une unité
		 *   - ajouter une action dans la queue (et ce qu'est la queue au passage) + explication sur les actions
		 * - ask goto position
		 * - play dialogue first game introduction prt1
		 *   - explication log avec touche et dégat
		 * - click menu btn
		 * - click quit btn
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	private void MicroTuto2 ()
	{
		if (!string.Equals(GameManager.Instance.CurrentMission.name, "TutoLevel2")
			|| GameDatas.current.currentPlayerSave.tutoProgression[3])
			return;


		/*
		 * - play dialogue micro 2
		 *   - Fonctionnement tour par tour prt2 (state + pfc)
		 *   - type de dégat, mort des unités et leur impact macro (le fait que c'est permanent)
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	private void MicroTuto3 ()
	{
		if (!string.Equals(GameManager.Instance.CurrentMission.name, "TutoLevel3")
			|| GameDatas.current.currentPlayerSave.tutoProgression[5])
			return;
	}

	private void MacroTuto1 ()
	{
		if (UIManager.Instance.currentPanel is not SoloHubPanel
			|| GameDatas.current.currentPlayerSave.tutoProgression[1])
			return;

		/*
		 * - play dialogue macro 1
		 *   - le joueur est dans le hub
		 *   - comment acceder au hangar (GoToMissionBtn must be deactivated until player when to hangar once)
		 * - le joueur se retrouve directement dans le hangar à la fin du dialogue
		 * - play dialogue explication hangar
		 *   - hangar
		 *   - edit uunité
		 *   - ajouter / retiré unit dans la squad
		 *   - bouton explication pour quitter le hangar et retour au hub
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	private void MacroTuto2 ()
	{
		if (UIManager.Instance.currentPanel is not SoloHubPanel
			|| GameDatas.current.currentPlayerSave.tutoProgression[2])
			return;

		/*
		 * - play dialogue macro 2
		 *   - Mission Panel
		 * - FastForward btn must be active
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	private void MacroTuto3 ()
	{
		if (UIManager.Instance.currentPanel is not HangarPanel
			|| GameDatas.current.currentPlayerSave.tutoProgression[4])
			return;

		/*
		 * - play dialogue macro 3
		 *   - Station de réparation, station upgrade and all structure upgrade at the same time
		 *   - Main Currency
		 *   - Les cycles (nombre de jour, trois dernier pour le tournoi)
		 *   - Tournoi (composé de trois missions, si tu rate le premier tu n'a pas accès au suivant etc)
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	private void MacroTuto4 ()
	{
		if (UIManager.Instance.currentPanel is not HangarPanel
			|| GameDatas.current.currentPlayerSave.tutoProgression[4])
			return;

		/*
		 * - play dialogue macro 4
		 *   - Recyclage
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	private void MacroTuto5 ()
	{
		if (UIManager.Instance.currentPanel is not HangarPanel
			|| GameDatas.current.currentPlayerSave.tutoProgression[4])
			return;

		/*
		 * - play dialogue macro 4
		 *   - Shop
		 *   - Shop currencies and shop upgrades
		 *   - selection des mission
		 */

		TaskSequence tutoSequence = new();

		tutoSequence.Append(new DialogueTask("Introduction", false, m_firstTutoDialogue));
		tutoSequence.Append(new ClickButtonTask("Open Deck", false, deckButton));
		tutoSequence.Append(new DialogueHighlightTask("Look here", false, m_firstTutoDialogue, registerdTutorialHighlightZones["deckHighlight"]));
		tutoSequence.Append(new MoveEntityToTileTask("Move to tile", false, new TileCoordinates(4, 7, 3))).onCompleted += ( Task t ) =>
		{
			GameDatas.current.currentPlayerSave.tutoProgression[0] = true;
		};

		tutoSequence.StartSequence();
	}

	#endregion
}
