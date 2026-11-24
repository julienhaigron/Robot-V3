using UnityEngine;
using TMPro;

public class ToolTipManager : Singleton<ToolTipManager>
{
	[Header("UI References")]
	[SerializeField] private TextTooltip m_textTooltip;
	[SerializeField] private ActionTooltip m_actionTooltip;
	[SerializeField] private LogTooltip[] m_logTooltips;

	private bool m_isActive = false;
	private bool m_isActionShown = false;
	private GameObject m_owner;
	private bool m_hasOwner;
	private LogConsole.LogDetails[] m_pinnedLogs;
	private int[] m_pinStamps;
	private int m_pinCount;
	private LogConsole.LogDetails m_hoveredLog;
	private GameObject m_hoveredLogOwner;

	private LogTooltip HoverLogTooltip => m_logTooltips[0];

	public override void Awake ()
	{
		base.Awake();

		m_isActive = false;
		m_textTooltip.gameObject.SetActive(false);
		m_actionTooltip.gameObject.SetActive(false);

		m_pinnedLogs = new LogConsole.LogDetails[m_logTooltips.Length];
		m_pinStamps = new int[m_logTooltips.Length];
		foreach (LogTooltip logTooltip in m_logTooltips)
		{
			logTooltip.SetPinned(false);
			logTooltip.gameObject.SetActive(false);
			logTooltip.onCloseRequested += OnLogTooltipCloseRequested;
		}

		InputManager.onTMPLinkHovered += OnTMPHover;
		InputManager.onTMPLinkUnhovered += OnTMPUnhover;
		LogConsole.onCleared += UnpinAllLogs;
	}

	private void OnDestroy ()
	{
		InputManager.onTMPLinkHovered -= OnTMPHover;
		InputManager.onTMPLinkUnhovered -= OnTMPUnhover;
		LogConsole.onCleared -= UnpinAllLogs;
	}

	private void Update ()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			UnpinAllLogs();

		if (m_isActive && m_hasOwner && (m_owner == null || !m_owner.activeInHierarchy))
			Hide();
	}

	public void Show ( string _title, string _description, GameObject _owner = null, Sprite _icon = null, Color? _accent = null )
	{
		HideAction();
		SetOwner(_owner);
		m_textTooltip.Show(_title, _description, _owner == null ? null : _owner.transform as RectTransform, _icon, _accent);
	}

	public void ShowAction ( ActionTooltipContent _content, GameObject _owner, string _footer = null )
	{
		m_textTooltip.Hide();
		SetOwner(_owner);
		m_isActionShown = true;
		m_actionTooltip.Show(_content, _owner == null ? null : _owner.transform as RectTransform, _footer);
	}

	public bool IsShownFor ( GameObject _owner )
	{
		return m_isActive && m_hasOwner && m_owner == _owner;
	}

	public void Hide ()
	{
		m_isActive = false;
		m_owner = null;
		m_hasOwner = false;
		HideAction();
		m_textTooltip.Hide();
	}

	private void SetOwner ( GameObject _owner )
	{
		m_isActive = true;
		m_owner = _owner;
		m_hasOwner = _owner != null;
	}

	private void HideAction ()
	{
		if (!m_isActionShown)
			return;

		m_isActionShown = false;
		m_actionTooltip.Hide();
	}

	private void OnTMPHover ( string _id, TMP_Text _text )
	{
		if (LogConsole.Instance.LogsDetails.TryGetValue(_id, out LogConsole.LogDetails details))
			ShowLog(details, _text.gameObject);
	}

	private void OnTMPUnhover ()
	{
		HideLog(m_hoveredLogOwner);
	}

	public void ShowLog ( LogConsole.LogDetails _details, GameObject _owner )
	{
		m_hoveredLog = _details;
		m_hoveredLogOwner = _owner;
		if (PinSlotOf(_details) > 0)
			HoverLogTooltip.Hide();
		else
			HoverLogTooltip.Show(_details.card);
	}

	public void HideLog ( GameObject _owner )
	{
		if (m_hoveredLog == null || m_hoveredLogOwner != _owner)
			return;

		m_hoveredLog = null;
		m_hoveredLogOwner = null;
		HoverLogTooltip.Hide();
	}

	public void TogglePin ( LogConsole.LogDetails _details )
	{
		if (_details == null || _details.card == null)
			return;

		int slot = PinSlotOf(_details);
		if (slot > 0)
		{
			Unpin(slot);
			if (_details == m_hoveredLog)
				HoverLogTooltip.Show(_details.card);
			return;
		}

		slot = FreePinSlot();
		LogTooltip pinned = m_logTooltips[slot];
		m_pinnedLogs[slot] = _details;
		m_pinStamps[slot] = ++m_pinCount;
		pinned.Show(_details.card);
		if (HoverLogTooltip.IsShown)
			pinned.CopyPlacementOf(HoverLogTooltip);
		pinned.SetPinned(true);
		HoverLogTooltip.Hide();
	}

	private int PinSlotOf ( LogConsole.LogDetails _details )
	{
		for (int i = 1; i < m_pinnedLogs.Length; i++)
			if (m_pinnedLogs[i] == _details)
				return i;
		return -1;
	}

	private int FreePinSlot ()
	{
		int oldest = 1;
		for (int i = 1; i < m_pinnedLogs.Length; i++)
		{
			if (m_pinnedLogs[i] == null)
				return i;
			if (m_pinStamps[i] < m_pinStamps[oldest])
				oldest = i;
		}

		Unpin(oldest);
		return oldest;
	}

	private void Unpin ( int _slot )
	{
		m_pinnedLogs[_slot] = null;
		m_logTooltips[_slot].SetPinned(false);
		m_logTooltips[_slot].Hide();
	}

	private void UnpinAllLogs ()
	{
		for (int i = 1; i < m_pinnedLogs.Length; i++)
			if (m_pinnedLogs[i] != null)
				Unpin(i);
	}

	private void OnLogTooltipCloseRequested ( LogTooltip _tooltip )
	{
		int slot = System.Array.IndexOf(m_logTooltips, _tooltip);
		if (slot > 0)
			Unpin(slot);
	}
}
