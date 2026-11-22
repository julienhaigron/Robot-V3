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

	public override void Awake ()
	{
		base.Awake();
		m_canvasGroup = m_tooltipPanel.GetComponent<CanvasGroup>();

		m_tooltipParentRtfm = m_tooltipPanel.transform.parent as RectTransform;
		m_toolTipRtfm = m_tooltipPanel.transform as RectTransform;

		m_isActive = false;
		m_tooltipPanel.SetActive(false);
		m_actionTooltip.gameObject.SetActive(false);

		InputManager.onTMPLinkHovered += OnTMPHover;
		InputManager.onTMPLinkUnhovered += OnTMPUnhover;
	}

	private void OnDestroy ()
	{
		InputManager.onTMPLinkHovered -= OnTMPHover;
		InputManager.onTMPLinkUnhovered -= OnTMPUnhover;
	}

	private void Update ()
	{
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

	private void OnTMPHover ( string id )
	{
		Show(LogConsole.Instance.LogsDetails[id].title, LogConsole.Instance.LogsDetails[id].description);
	}

	private void OnTMPUnhover ()
	{
		Hide();
	}
}
