using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class StartMenuPanel : AUIPanel
{
	[Title("Start")]
	[SerializeField] private Transform m_startModeParent;
	[SerializeField] private BaseButton m_newGameBtn;
	[SerializeField] private BaseButton m_loadGameBtn;
	[SerializeField] private BaseButton m_optionBtn;
	[SerializeField] private BaseButton m_quitBtn;

	[Title("Load save")]
	[SerializeField] private Transform m_saveBtnsParent;
	[SerializeField] SaveButton[] m_savesBtns;
	[SerializeField] private BaseButton m_returnFromLoadButton;

	[Title("New save")]
	[SerializeField] private Transform m_newSaveModeParent;
	[SerializeField] private BaseButton m_returnFromNewSave;

	[Title("Multiplayer")]
	[SerializeField] private BaseButton m_multiplayerModeBtn;

	public enum StartMenuDisplayMode { Start, LoadSave, NewSave }
	[SerializeField] private StartMenuDisplayMode m_currentDisplayMode = StartMenuDisplayMode.Start;

	private void Awake ()
	{
		m_newGameBtn.onClick += OnClickNewSaveBtn;
		m_loadGameBtn.onClick += OnClickLoadSaveBtn;
		m_optionBtn.onClick += OnClickOptionBtn;
		m_quitBtn.onClick += OnClickQuitBtn;
		m_multiplayerModeBtn.onClick += OnClickMultiModeBtn;
		m_returnFromLoadButton.onClick += OnClickReturn;
		m_returnFromNewSave.onClick += OnClickReturn;
	}

	private void OnDestroy ()
	{
		m_newGameBtn.onClick -= OnClickNewSaveBtn;
		m_loadGameBtn.onClick -= OnClickLoadSaveBtn;
		m_optionBtn.onClick -= OnClickOptionBtn;
		m_quitBtn.onClick -= OnClickQuitBtn;
		m_multiplayerModeBtn.onClick -= OnClickMultiModeBtn;
		m_returnFromLoadButton.onClick -= OnClickReturn;
		m_returnFromNewSave.onClick -= OnClickReturn;
	}

	private void ChangeDisplayMode(StartMenuDisplayMode _displayMode, bool _isInstant )
	{
		m_saveBtnsParent.gameObject.SetActive(false);
		m_startModeParent.gameObject.SetActive(false);
		m_newSaveModeParent.gameObject.SetActive(false);

		m_currentDisplayMode = _displayMode;

		switch (_displayMode)
		{
			case StartMenuDisplayMode.Start:
				m_startModeParent.gameObject.SetActive(true);
				m_newGameBtn.gameObject.SetActive(GameDatas.current.playerSaves.Count < m_savesBtns.Length);
				m_loadGameBtn.gameObject.SetActive(GameDatas.current.game.lastPlayerSaveSelectedID != -1);
				break;
			case StartMenuDisplayMode.LoadSave:
				m_saveBtnsParent.gameObject.SetActive(true);
				for (int i = 0; i < m_savesBtns.Length; i++)
				{
					if (GameDatas.current.playerSaves.Count > i)
					{
						m_savesBtns[i].gameObject.SetActive(true);
						m_savesBtns[i].Init(GameDatas.current.playerSaves[i], i);
					}
					else
						m_savesBtns[i].gameObject.SetActive(false);
				}
				break;
			case StartMenuDisplayMode.NewSave:
				m_newSaveModeParent.gameObject.SetActive(true);
				break;
		}
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		ChangeDisplayMode(StartMenuDisplayMode.Start, true);
	}


	private void OnClickLoadSaveBtn ()
	{
		ChangeDisplayMode(StartMenuDisplayMode.LoadSave, false);
	}

	private void OnClickNewSaveBtn ()
	{
		//OLD :ChangeDisplayMode(StartMenuDisplayMode.NewSave, false);

		GameDatas.current.game.lastPlayerSaveSelectedID = GameDatas.current.playerSaves.Count;
		GameDatas.current.CreateSave("New save");
		GameManager.Instance.LoadSaveAndGoToHub(GameDatas.current.game.lastPlayerSaveSelectedID);
	}

	private void OnClickOptionBtn ()
	{
		UIManager.Instance.OpenPopup<OptionPopup>();
	}

	private void OnClickQuitBtn ()
	{
		Application.Quit();
	}

	private void OnClickReturn ()
	{
		switch (m_currentDisplayMode)
		{
			case StartMenuDisplayMode.Start:
				//no effect, btn isnt visible
				break;
			case StartMenuDisplayMode.LoadSave:
				ChangeDisplayMode(StartMenuDisplayMode.Start, false);
				break;
			case StartMenuDisplayMode.NewSave:
				ChangeDisplayMode(StartMenuDisplayMode.Start, false);
				break;
		}
	}


	//multi

	private void OnClickMultiModeBtn ()
	{
		GameManager.Instance.CurrentGameMode = GameManager.GameMode.Online;
		Close();
		UIManager.Instance.OpenPanel<LobbySelectionPanel>();
	}
}
