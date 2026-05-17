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
    [SerializeField] private RectTransform m_backgroundRect;

    [Header("Settings")]
    [SerializeField] private Vector2 m_offset = new Vector2(15, -15);
    [SerializeField] private float m_fadeDuration = 0.2f;

    private bool m_isActive = false;
    private CanvasGroup m_canvasGroup;
    private Coroutine m_fadeCoroutine;

    public override void Awake ()
    {
        base.Awake();
        m_canvasGroup = m_tooltipPanel.GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
            m_canvasGroup = m_tooltipPanel.AddComponent<CanvasGroup>();

        m_isActive = false;
        m_tooltipPanel.SetActive(false);

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

        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_tooltipPanel.transform.parent as RectTransform,
            Input.mousePosition,
            null,
            out anchoredPosition
        );

        anchoredPosition += m_offset;

        // Clamp dans l'écran
        Vector2 size = m_backgroundRect.sizeDelta;
        RectTransform parentRect = m_tooltipPanel.transform.parent as RectTransform;

        anchoredPosition.x = Mathf.Clamp(
            anchoredPosition.x,
            -parentRect.rect.width / 2 + size.x / 2,
            parentRect.rect.width / 2 - size.x / 2
        );
        anchoredPosition.y = Mathf.Clamp(
            anchoredPosition.y,
            -parentRect.rect.height / 2 + size.y / 2,
            parentRect.rect.height / 2 - size.y / 2
        );

        m_tooltipPanel.transform.localPosition = anchoredPosition;
    }

    [Button]
    public void Show ( string _title, string _description )
    {
        m_isActive = true;
        m_tooltipTitleTMP.text = _title;
        m_tooltipDescriptionTMP.text = _description;

        /*// Resize background selon le texte
        Vector2 textSize = m_tooltipTitleTMP.GetPreferredValues(_title) + m_tooltipDescriptionTMP.GetPreferredValues(_description);
        m_backgroundRect.sizeDelta = textSize + new Vector2(10, 10); // padding*/

        m_tooltipPanel.SetActive(true);

        if (m_fadeCoroutine != null)
            StopCoroutine(m_fadeCoroutine);
        m_fadeCoroutine = StartCoroutine(FadeCanvas(1f));
    }

    public void Hide ()
    {
        m_isActive = false;
        if (m_fadeCoroutine != null)
            StopCoroutine(m_fadeCoroutine);
        m_fadeCoroutine = StartCoroutine(FadeCanvas(0f, true));
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