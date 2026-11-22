using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sirenix.OdinInspector;

public class ToolTipManager : Singleton<ToolTipManager>
{
	[Header("UI References")]
	[SerializeField] private GameObject m_tooltipPanel;
	[SerializeField] private TextMeshProUGUI m_tooltipTitleTMP;
	[SerializeField] private TextMeshProUGUI m_tooltipDescriptionTMP;
	[SerializeField] private ActionTooltip m_actionTooltip;
	[SerializeField] private LogTooltip[] m_logTooltips;

	[Header("Settings")]
	[SerializeField] private Vector2 m_offset = new Vector2(15, -15);
	[SerializeField] private float m_fadeDuration = 0.2f;

	private bool m_isActive = false;
	private bool m_isActionShown = false;
	private GameObject m_owner;
	private bool m_hasOwner;
	private CanvasGroup m_canvasGroup;
	private Coroutine m_fadeCoroutine;
	private RectTransform m_tooltipParentRtfm;
	private RectTransform m_toolTipRtfm;
	private LogConsole.LogDetails[] m_pinnedLogs;
	private int[] m_pinStamps;
	private int m_pinCount;
	private LogConsole.LogDetails m_hoveredLog;

	private LogTooltip HoverLogTooltip => m_logTooltips[0];

	public override void Awake ()
	{
		base.Awake();
		m_canvasGroup = m_tooltipPanel.GetComponent<CanvasGroup>();

		m_tooltipParentRtfm = m_tooltipPanel.transform.parent as RectTransform;
		m_toolTipRtfm = m_tooltipPanel.transform as RectTransform;

		m_isActive = false;
		m_tooltipPanel.SetActive(false);
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

		if (!m_isActive) return;

		if (m_hasOwner && (m_owner == null || !m_owner.activeInHierarchy))
		{
			Hide();
			return;
		}

		if (m_isActionShown)
			return;

		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			m_tooltipParentRtfm,
			Input.mousePosition,
			null,
			out Vector2 localMousePosition
		);

		bool isRightSide = Input.mousePosition.x > Screen.width / 2f;
		m_toolTipRtfm.pivot = new Vector2(isRightSide ? 1f : 0f,0f);

		float xOffset = isRightSide ? -m_offset.x : m_offset.x;
		m_toolTipRtfm.localPosition = localMousePosition + new Vector2(xOffset,m_offset.y);
	}

	[Button]
	public void Show ( string _title, string _description, GameObject _owner = null )
	{
		HideAction();
		SetOwner(_owner);
		m_tooltipTitleTMP.text = _title;
		m_tooltipDescriptionTMP.text = _description;

		m_tooltipPanel.SetActive(true);

		if (m_fadeCoroutine != null)
			StopCoroutine(m_fadeCoroutine);
		m_fadeCoroutine = StartCoroutine(FadeCanvas(1f));
	}

	public void ShowAction ( ActionTooltipContent _content, GameObject _owner, string _footer = null )
	{
		if (m_fadeCoroutine != null)
			StopCoroutine(m_fadeCoroutine);
		m_canvasGroup.alpha = 0f;
		m_tooltipPanel.SetActive(false);

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
		if (m_fadeCoroutine != null)
			StopCoroutine(m_fadeCoroutine);
		m_fadeCoroutine = StartCoroutine(FadeCanvas(0f, true));
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

	private IEnumerator FadeCanvas ( float _targetAlpha, bool _deactivateOnEnd = false )
	{
		float startAlpha = m_canvasGroup.alpha;
		float timer = 0f;

		while (timer < m_fadeDuration)
		{
			timer += Time.unscaledDeltaTime;
			m_canvasGroup.alpha = Mathf.Lerp(startAlpha, _targetAlpha, timer / m_fadeDuration);
			yield return null;
		}

		m_canvasGroup.alpha = _targetAlpha;
		if (_deactivateOnEnd) m_tooltipPanel.SetActive(false);
	}

	private void OnTMPHover ( string _id, TMP_Text _text )
	{
		if (!LogConsole.Instance.LogsDetails.TryGetValue(_id, out LogConsole.LogDetails details))
			return;

		if (details.action != null)
		{
			ShowAction(details.action, _text.gameObject);
			return;
		}

		m_hoveredLog = details;
		if (PinSlotOf(details) > 0)
			HoverLogTooltip.Hide();
		else
			HoverLogTooltip.Show(details.card);
	}

	private void OnTMPUnhover ()
	{
		m_hoveredLog = null;
		HoverLogTooltip.Hide();
		Hide();
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
