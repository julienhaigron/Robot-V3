using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public sealed class UIManager : SingletonPersistant<UIManager>
{
	public static Action onChangeScreen;
	//Panel contain TopCanvas

	//GOALS :
	//1) Get and Open Panel
	//2) Get and Open TopCanvas
	//3) Show Panel / Show TopCanvas

	[SerializeField] private AUIPanel m_firstPanelToOpen;

	private static List<AUIPanel> m_panels;
	private static List<AUIPopup> m_popups;
	private static List<AUITopCanvas> m_topCanvases;
	private Queue<AUIPopup> m_succesivePopups = new Queue<AUIPopup>();
	private float m_nextShowPanelDelay;
	private bool m_nextShowPanelInstant;

	#region Getter-Setter
	private Dictionary<Type, AUIPanel> panelsDictionary { get; set; }

	private Dictionary<Type, AUIPopup> popupsDictionary;
	public AUIPopup activePopup { get; private set; }
	private List<Type> previousPanels { get; set; }
	public AUIPanel currentPanel { get; private set; }
	public AUIPanel nextPanel { get; private set; }
	public AUIPanel rootPanel { get; private set; }
	private Dictionary<Type, AUITopCanvas> topCanvasesDictionary { get; set; }
	#endregion

	#region Init

	public override void Awake ()
	{
		base.Awake();
		m_panels = new List<AUIPanel>();
		m_popups = new List<AUIPopup>();
		m_topCanvases = new List<AUITopCanvas>();

		AUIWindow[] windows = FindObjectsByType<AUIWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < windows.Length; i++)
		{
			windows[i].OnLoad();
			windows[i].WindowCanvas.enabled = false;
			windows[i].gameObject.SetActive(false);

			if (windows[i] is AUIPanel)
				m_panels.Add(windows[i] as AUIPanel);
			else if (windows[i] is AUIPopup)
				m_popups.Add(windows[i] as AUIPopup);

		}

		m_topCanvases.AddRange(FindObjectsByType<AUITopCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None));

		for (int i = 0; i < m_topCanvases.Count; i++)
		{
			m_topCanvases[i].OnLoad();
		}


		SetupPanels();
		SetupPopups();
		SetupTopCanvases();
	}

	private void Start ()
	{
		OpenPanel(m_firstPanelToOpen);
	}

	private void SetupPanels ()
	{
		this.panelsDictionary = new Dictionary<Type, AUIPanel>();
		this.previousPanels = new List<Type>();

		foreach (AUIPanel panel in m_panels)
		{
			if (panel != null)
			{
				this.panelsDictionary.Add(panel.GetType(), panel);
				panel.gameObject.SetActive(false);
			}
		}
	}

	private void SetupPopups ()
	{
		this.popupsDictionary = new Dictionary<Type, AUIPopup>();

		foreach (AUIPopup popup in m_popups)
		{
			if (popup != null)
			{
				this.popupsDictionary.Add(popup.GetType(), popup);
				popup.SetCanvasEnable(false);
				popup.gameObject.SetActive(false);
			}
		}
	}

	private void SetupTopCanvases ()
	{
		topCanvasesDictionary = new Dictionary<Type, AUITopCanvas>();
		foreach (AUITopCanvas topCanvas in m_topCanvases)
		{
			if (topCanvas != null && !topCanvasesDictionary.ContainsKey(topCanvas.GetType()))
			{
				topCanvasesDictionary.Add(topCanvas.GetType(), topCanvas);
				topCanvas.OnRegister();
			}
		}
	}
	#endregion

	#region Panel

	public T OpenPanel<T> ( float _showDelay = 0f, bool _showInstant = false, bool _pushHistory = true, bool _additive = false, bool _closePreviousInstant = false, bool _closeAndOpenSimultaneous = true ) where T : AUIPanel
	{
		Type type = typeof(T);
		return (this.OpenPanel(type, _showDelay, _showInstant, _pushHistory, _additive, _closePreviousInstant, _closeAndOpenSimultaneous) as T);
	}

	public AUIPanel OpenPanel ( AUIPanel _panel, float _showDelay = 0f, bool _showInstant = false, bool _pushHistory = true, bool _additive = false, bool _closePreviousInstant = false, bool _closeAndOpenSimultaneous = true )
	{
		Type type = _panel.GetType();
		return (this.OpenPanel(type, _showDelay, _showInstant, _pushHistory, _additive, _closePreviousInstant, _closeAndOpenSimultaneous) as AUIPanel);
	}

	private AUIPanel OpenPanel ( Type _type, float _showDelay, bool _showInstant, bool _pushHistory, bool _additive, bool _closePreviousInstant = false, bool _closeAndOpenSimultaneous = true )
	{
		if (panelsDictionary.ContainsKey(_type) == false)
			throw new Exception(GetType().Name + " - Do not have panel [" + _type.Name + "].");

		nextPanel = panelsDictionary[_type];
		m_nextShowPanelDelay = _showDelay;
		m_nextShowPanelInstant = _showInstant;

		if (currentPanel != null)
		{
			currentPanel.CanClick = false;

			if (!_additive && _pushHistory)
				currentPanel.Close(0f, _closePreviousInstant);
			if (_pushHistory)
				previousPanels.Add(currentPanel.GetType());

			//wait for the end of close to show next
			if (!_additive && !_closePreviousInstant && !_closeAndOpenSimultaneous)
			{
				currentPanel.SetCanvasEnable(false);
				//the next Panel will be activated OnPanelClosed
				/*this.currentPanel.OnCloseAnimationFinishedAction += () =>
				{
					this.currentPanel.SetCanvasEnable(false);
					this.panels[_type].SetCanvasEnable(true);
					this.panels[_type].ShowWindow(_showDelay, _showInstant);
					this.currentPanel = this.panels[_type];
					#if DEBUG
					if (m_showLog)
						Debug.Log("AUIManager - Open panel [" + _type.Name + "].");
					#endif
				};*/
				return (nextPanel);
			}
		}
		else
			rootPanel = nextPanel;

		nextPanel.SetCanvasEnable(true);
		nextPanel.ShowWindow(m_nextShowPanelDelay, m_nextShowPanelInstant);
		currentPanel = nextPanel;

		nextPanel = null;

		onChangeScreen?.Invoke();
		return currentPanel;
	}

	public void ClosePanel<T> ()
	{
		Type type = typeof(T);
		this.ClosePanel(type);
	}

	private void ClosePanel ( Type _type )
	{
		if (this.panelsDictionary.ContainsKey(_type) == false)
			throw new Exception(this.GetType().Name + " - Do not have panel [" + _type.Name + "].");


		AUIPanel panel = this.panelsDictionary[_type];

		if (this.previousPanels.Count == 0 && this.currentPanel != null && panel == this.currentPanel)
		{
			Debug.LogWarning(this.GetType().Name + " - You attempting to close manually the root panel");
			return;
		}

		if (panel.CanvasEnabled)
			panel.Close(0f, false);

		this.previousPanels.Remove(_type);
	}

	public T GetPanel<T> () where T : AUIPanel
	{
		Type type = typeof(T);

		if (this.panelsDictionary.ContainsKey(type) == false)
			throw new Exception(this.GetType().Name + " - Do not have panel [" + type.Name + "].");
		return (this.panelsDictionary[type] as T);
	}


	public void AddPanel ( AUIPanel _aUIPanel )
	{
		if (!m_panels.Contains(_aUIPanel))
			m_panels.Add(_aUIPanel);
	}

	public void RemovePanel ( AUIPanel _aUIPanel )
	{
		m_panels.Remove(_aUIPanel);
	}

	#endregion

	#region Popup
	public T OpenPopup<T> ( float _showDelay = 0f, bool _showInstant = false ) where T : AUIPopup
	{
		Type type = typeof(T);
		return (this.OpenPopup(type, _showDelay, _showInstant) as T);
	}

	public AUIPopup OpenPopup ( AUIPopup _popup, float _delay = 0f, bool _instant = false )
	{
		Type type = _popup.GetType();
		return (this.OpenPopup(type, _delay, _instant) as AUIPopup);
	}

	private AUIPopup OpenPopup ( Type _type, float _delay = 0f, bool _instant = false )
	{
		AUIPopup popup;

		if (this.popupsDictionary.ContainsKey(_type) == false)
			throw new Exception(this.GetType().Name + " - Do not have popup [" + _type.Name + "].");

		popup = this.popupsDictionary[_type];

		if (activePopup != null)
		{
			if (popup == activePopup)
			{
				Debug.LogError("Popup " + _type.Name + " already opened");
				return null;
			}
			else
			{
				if (!m_succesivePopups.Contains(popup))
				{
					m_succesivePopups.Enqueue(popup);
					return popup;
				}
				else
				{
					Debug.LogError("Popup " + _type.Name + "is already in queue");
					return null;
				}
			}
		}

		popup.SetCanvasEnable(true);
		popup.ShowWindow(_delay, _instant);
		SetActivePopup(popup);
		onChangeScreen?.Invoke();
		return (popup);
	}

	public void ClosePopup<T> ( float _delay = 0f, bool _instant = false ) where T : AUIPopup
	{
		Type type = typeof(T);
		this.ClosePopup(type, _delay, _instant);
	}

	private void ClosePopup ( Type _type, float _delay = 0f, bool _instant = false )
	{
		AUIPopup popup;

		if (this.popupsDictionary.ContainsKey(_type) == false)
			throw new Exception(this.GetType().Name + " - Do not have popup [" + _type.Name + "].");

		popup = this.popupsDictionary[_type];
		popup.Close(_delay, _instant);

		if (this.activePopup != null && popup == this.activePopup)
			this.activePopup = null;
	}

	public T GetPopup<T> () where T : AUIPopup
	{
		Type type = typeof(T);

		if (this.popupsDictionary.ContainsKey(type) == false)
			throw new Exception(this.GetType().Name + " - Do not have popup [" + type.Name + "].");
		return (this.popupsDictionary[type] as T);
	}

	public void CloseActivePopup ( bool _forceClose = false )
	{
		if (this.activePopup == null)
			return;


		/*if (!_forceClose && !this.activePopup.BackInputAllowed())
		{
#if UNITY_EDITOR
			if (ApplicationManager.config.debug.showUIManagerLog)
				Debug.LogWarning("AUIManger - Current popup cannot be closed from user Input");
#endif
			return;
		}*/

		this.activePopup.Close(0, _forceClose);
		this.activePopup = null;
	}

	private void SetActivePopup ( AUIPopup _popup )
	{
		if (!m_popups.Contains(_popup))
			return;

		if (this.activePopup == _popup)
			return;

		if (this.activePopup != null)
			this.activePopup.Close();

		this.activePopup = _popup;
	}

	public bool HaveAnActivePopup ()
	{
		return activePopup != null && activePopup.CanvasEnabled;
	}

	public void OnPopupClosed ( AUIPopup _popup )
	{
		if (this.activePopup != null && this.activePopup == _popup)
			this.activePopup = null;

		_popup.SetCanvasEnable(false);
		if (this.currentPanel.CanvasEnabled == false)
			this.currentPanel.SetCanvasEnable(true);

		if (m_succesivePopups.Count > 0)
		{
			_popup = m_succesivePopups.Dequeue();
			OpenPopup(_popup);
		}
		onChangeScreen?.Invoke();
	}

	/*public void OpenProcessingPopup ( string _message )
	{
		m_processingPopup.gameObject.SetActive(true);
		m_processingPopup.message = _message;
	}

	public void CloseProcessingPopup ()
	{
		m_processingPopup.gameObject.SetActive(false);
	}*/

	public void InvokeNowOrAtPopupClose ( Action _action )
	{
		if (!HaveAnActivePopup())
			_action?.Invoke();
		else
		{
			_action += () => activePopup.onWindowClosed -= _action;
			activePopup.onWindowClosed += _action;
		}
	}

	#endregion

	#region TopCanvas

	public T ShowTopCanvas<T> () where T : AUITopCanvas
	{
		Type type = typeof(T);
		return (this.ShowTopCanvas(type) as T);
	}

	public AUITopCanvas ShowTopCanvas ( AUITopCanvas _topCanvas )
	{
		Type type = _topCanvas.GetType();
		return (this.ShowTopCanvas(type) as AUITopCanvas);
	}

	private AUITopCanvas ShowTopCanvas ( Type _type )
	{
		AUITopCanvas topCanvas;

		if (this.topCanvasesDictionary.ContainsKey(_type) == false)
			throw new Exception(this.GetType().Name + " - Do not have topCanvas [" + _type.Name + "].");

		topCanvas = this.topCanvasesDictionary[_type];

		topCanvas.SetVisible(true);

		return (topCanvas);
	}

	public void HideTopCanvas<T> () where T : AUITopCanvas
	{
		Type type = typeof(T);
		this.HideTopCanvas(type);
	}

	private void HideTopCanvas ( Type _type )
	{
		AUITopCanvas topCanvas;

		if (this.topCanvasesDictionary.ContainsKey(_type) == false)
			throw new Exception(this.GetType().Name + " - Do not have topCanvas [" + _type.Name + "].");

		topCanvas = this.topCanvasesDictionary[_type];

		topCanvas.SetVisible(false);
	}

	public T GetTopCanvas<T> () where T : AUITopCanvas
	{
		Type type = typeof(T);

		if (this.topCanvasesDictionary.ContainsKey(type) == false)
			throw new Exception(this.GetType().Name + " - Do not have topCanvas [" + type.Name + "].");
		return (this.topCanvasesDictionary[type] as T);
	}

	#endregion

}
