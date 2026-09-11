using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;

public class TutoConsole : MonoBehaviour
{
	[Title("Dependancies")]
	[SerializeField] private GameObject m_dialogueParent;
	[SerializeField] private TextMeshProUGUI m_dialogueTMP;
	[SerializeField] private TextMeshProUGUI m_characterNameTMP;
	[SerializeField] private Image m_dialogueImg;
	[SerializeField] private BaseButton m_previousBtn;
	[SerializeField] private BaseButton m_nextBtn;
	[SerializeField] private BaseButton m_showBtn;
	[SerializeField] private BaseButton m_hideBtn;

	[Title("Parameters")]
	[SerializeField] private float m_charactersPerSecond = 30f;
	[SerializeField] private float m_scrollSpeed = 25f;
	[SerializeField] private float m_scrollEdgePause = 1.5f;

	private List<TutoDialogueContainer> m_allDialogs = new();
	public List<TutoDialogueContainer> AllDialogs => m_allDialogs;

	[Serializable]
	public class TutoDialogueContainer
	{
		public DialogueData dialogue;
		public string highlightedZoneId;
	}

	private TutoDialogueContainer m_currentDialogueData;
	private int m_currentDialogueIndex = -1;
	private int m_currentLineIndex;
	private Tween m_currentTextTween;
	private RectTransform m_dialogueViewport;
	private RectTransform m_dialogueTextRect;
	private Sequence m_scrollSequence;

	private void Awake ()
	{
		BuildDialogueViewport();
		m_previousBtn.onClick += GoToPreviousLineOrDialogue;
		m_nextBtn.onClick += GoToNextLineOrDialogue;
		m_showBtn.onClick += OnClickShow;
		m_hideBtn.onClick += OnClickHide;
	}

	private void OnDestroy ()
	{
		m_scrollSequence?.Kill();
		m_previousBtn.onClick -= GoToPreviousLineOrDialogue;
		m_nextBtn.onClick -= GoToNextLineOrDialogue;
		m_showBtn.onClick -= OnClickShow;
		m_hideBtn.onClick -= OnClickHide;
	}

	private void BuildDialogueViewport ()
	{
		m_dialogueTextRect = m_dialogueTMP.rectTransform;
		if (m_dialogueViewport != null || m_dialogueTextRect.parent == null)
			return;

		GameObject viewportGO = new GameObject("DialogueViewport", typeof(RectTransform), typeof(RectMask2D));
		m_dialogueViewport = viewportGO.GetComponent<RectTransform>();
		m_dialogueViewport.SetParent(m_dialogueTextRect.parent, false);
		m_dialogueViewport.SetSiblingIndex(m_dialogueTextRect.GetSiblingIndex());

		m_dialogueViewport.anchorMin = m_dialogueTextRect.anchorMin;
		m_dialogueViewport.anchorMax = m_dialogueTextRect.anchorMax;
		m_dialogueViewport.pivot = m_dialogueTextRect.pivot;
		m_dialogueViewport.anchoredPosition = m_dialogueTextRect.anchoredPosition;
		m_dialogueViewport.sizeDelta = m_dialogueTextRect.sizeDelta;

		m_dialogueTextRect.SetParent(m_dialogueViewport, false);
		m_dialogueTextRect.anchorMin = new Vector2(0f, 1f);
		m_dialogueTextRect.anchorMax = new Vector2(1f, 1f);
		m_dialogueTextRect.pivot = new Vector2(0.5f, 1f);
		m_dialogueTextRect.anchoredPosition = Vector2.zero;
		m_dialogueTextRect.sizeDelta = new Vector2(0f, m_dialogueViewport.rect.height);

		m_dialogueTMP.overflowMode = TextOverflowModes.Overflow;
		m_dialogueTMP.enableWordWrapping = true;
	}

	private float GetHiddenTextHeight ()
	{
		if (m_dialogueViewport == null)
			return 0f;

		return Mathf.Max(0f, m_dialogueTMP.preferredHeight - m_dialogueViewport.rect.height);
	}

	private void ResetScroll ()
	{
		m_scrollSequence?.Kill();
		m_scrollSequence = null;

		if (m_dialogueTextRect != null)
			m_dialogueTextRect.anchoredPosition = Vector2.zero;
	}

	private void ScrollToBottom ()
	{
		if (m_dialogueTextRect != null)
			m_dialogueTextRect.anchoredPosition = new Vector2(0f, GetHiddenTextHeight());
	}

	private void StartScrollLoop ()
	{
		ResetScroll();

		float hiddenHeight = GetHiddenTextHeight();
		if (hiddenHeight <= 1f || m_scrollSpeed <= 0f)
			return;

		m_scrollSequence = DOTween.Sequence();
		m_scrollSequence.AppendInterval(m_scrollEdgePause);
		m_scrollSequence.Append(m_dialogueTextRect.DOAnchorPosY(hiddenHeight, hiddenHeight / m_scrollSpeed).SetEase(Ease.Linear));
		m_scrollSequence.AppendInterval(m_scrollEdgePause);
		m_scrollSequence.AppendCallback(() => m_dialogueTextRect.anchoredPosition = Vector2.zero);
		m_scrollSequence.SetLoops(-1);
	}

	public void Init ()
	{
		m_allDialogs.Clear();

		m_currentTextTween?.Kill();
		m_currentDialogueData = null;
		m_currentDialogueIndex = -1;
		m_currentLineIndex = 0;

		ResetScroll();
		m_characterNameTMP.text = "";
		m_dialogueTMP.text = "";
		m_dialogueImg.sprite = null;

		RefreshButtons();
		Hide(true);
	}

	private void OnClickShow ()
	{
		Show(false);
	}

	private void OnClickHide ()
	{
		Hide(false);
	}

	public void Show ( bool _isInstant )
	{
		m_dialogueParent.SetActive(true);
		m_showBtn.SetVisible(false, true);
		m_hideBtn.SetVisible(true, true);
	}

	public void Hide ( bool _isInstant )
	{
		m_dialogueParent.SetActive(false);
		m_showBtn.SetVisible(true, true);
		m_hideBtn.SetVisible(false, true);
	}

	public void PlayDialogue ( DialogueData _dialogueData, string _higlightedZoneID = "" )
	{
		bool wasCaughtUp = IsCaughtUp();

		m_allDialogs.Add(new() { dialogue = _dialogueData, highlightedZoneId = _higlightedZoneID });

		Show(false);

		if (wasCaughtUp)
			DisplayDialogue(m_allDialogs.Count - 1);
		else
			RefreshButtons();
	}

	private bool IsCaughtUp ()
	{
		if (m_currentDialogueIndex < 0 || m_currentDialogueData == null)
			return true;

		if (m_currentTextTween.IsActive())
			return false;

		return m_currentDialogueIndex == m_allDialogs.Count - 1
			&& m_currentLineIndex >= m_currentDialogueData.dialogue.lines.Count - 1;
	}

	private void DisplayDialogue ( int _dialogueIndex, int _lineIndex = 0 )
	{
		int previousDialogueIndex = m_currentDialogueIndex;

		m_currentDialogueIndex = Mathf.Clamp(_dialogueIndex, 0, m_allDialogs.Count - 1);
		m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
		m_currentLineIndex = Mathf.Clamp(_lineIndex, 0, m_currentDialogueData.dialogue.lines.Count - 1);

		if (previousDialogueIndex != m_currentDialogueIndex)
			RefreshHighlightZone(previousDialogueIndex);

		DisplayCurrentLine();
	}

	private void RefreshHighlightZone ( int _previousDialogueIndex )
	{
		if (_previousDialogueIndex >= 0 && _previousDialogueIndex < m_allDialogs.Count
			&& FTUEManager.Instance.TryGetTutorialHighlightZone(m_allDialogs[_previousDialogueIndex].highlightedZoneId, out TutorialHighlightZone previousZone))
			previousZone.Hide();

		if (FTUEManager.Instance.TryGetTutorialHighlightZone(m_currentDialogueData.highlightedZoneId, out TutorialHighlightZone currentZone))
			currentZone.Show();
	}

	private void DisplayCurrentLine ()
	{
		DialogueData.Line line = m_currentDialogueData.dialogue.lines[m_currentLineIndex];

		m_characterNameTMP.text = line.characterName;
		m_dialogueImg.sprite = line.characterSprite;
		m_dialogueTMP.text = "";
		ResetScroll();
		float duration = line.sentence.Length / m_charactersPerSecond;

		m_currentTextTween?.Kill();
		m_currentTextTween = DOTween.To(
			() => 0,
			x => m_dialogueTMP.text = line.sentence.Substring(0, x),
			line.sentence.Length,
			duration)
			.SetEase(Ease.Linear)
			.OnStart(() => m_dialogueTMP.text = "")
			.OnUpdate(ScrollToBottom)
			.OnComplete(() =>
			{
				m_dialogueTMP.text = line.sentence;
				m_dialogueTMP.ForceMeshUpdate();
				StartScrollLoop();
			}
		);

		RefreshButtons();
	}

	private void RefreshButtons ()
	{
		bool hasDialogue = m_currentDialogueIndex >= 0 && m_currentDialogueData != null;

		bool canGoPrevious = hasDialogue
			&& (m_currentLineIndex > 0 || m_currentDialogueIndex > 0);

		bool canGoNext = hasDialogue
			&& (m_currentLineIndex < m_currentDialogueData.dialogue.lines.Count - 1
			|| m_currentDialogueIndex < m_allDialogs.Count - 1);

		m_previousBtn.SetInteractability(canGoPrevious);
		m_nextBtn.SetInteractability(canGoNext);
	}

	public void GoToPreviousLineOrDialogue ()
	{
		if (m_currentDialogueIndex < 0)
			return;

		if (m_currentTextTween.IsActive())
			m_currentTextTween.Complete();
		else if (m_currentLineIndex > 0)
			DisplayDialogue(m_currentDialogueIndex, m_currentLineIndex - 1);
		else if (m_currentDialogueIndex > 0)
			DisplayDialogue(m_currentDialogueIndex - 1, m_allDialogs[m_currentDialogueIndex - 1].dialogue.lines.Count - 1);
	}

	public void GoToNextLineOrDialogue ()
	{
		if (m_currentDialogueIndex < 0)
			return;

		if (m_currentTextTween.IsActive())
			m_currentTextTween.Complete();
		else if (m_currentLineIndex < m_currentDialogueData.dialogue.lines.Count - 1)
			DisplayDialogue(m_currentDialogueIndex, m_currentLineIndex + 1);
		else if (m_currentDialogueIndex < m_allDialogs.Count - 1)
			DisplayDialogue(m_currentDialogueIndex + 1);
	}
}
