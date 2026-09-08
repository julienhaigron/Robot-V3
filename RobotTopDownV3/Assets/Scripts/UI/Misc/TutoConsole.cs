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

	private Action m_onDialogueEnded;
	private TutoDialogueContainer m_currentDialogueData;
	private int m_currentDialogueIndex = -1;
	private bool m_waitingForNextLine;
	private int m_currentLineIndex;
	private Tween m_currentTextTween;
	private bool m_didEndLastDialogue = true;
	private RectTransform m_dialogueViewport;
	private RectTransform m_dialogueTextRect;
	private Sequence m_scrollSequence;

	private void Awake ()
	{
		BuildDialogueViewport();
		m_previousBtn.onClick += OnClickPreviousLineOrDialogue;
		m_nextBtn.onClick += OnClickNextLineOrDialogue;
	}

	private void OnDestroy ()
	{
		m_scrollSequence?.Kill();
		m_previousBtn.onClick -= OnClickPreviousLineOrDialogue;
		m_nextBtn.onClick -= OnClickNextLineOrDialogue;
	}

	//The dialogue box has a fixed height, so a long line has to scroll. The clipping viewport is
	//created here rather than in the prefab: the text keeps its authored rect, it is just reparented
	//into a mask of that same size and left free to grow downwards.
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

		Hide(true);
	}

	public void Show ( bool _isInstant )
	{
		m_dialogueParent.SetActive(true);
	}

	public void Hide ( bool _isInstant )
	{
		m_dialogueParent.SetActive(false);
	}

	public void PlayDialogue ( DialogueData _dialogueData, Action _onDialogueEnded, string _higlightedZoneID = "" )
	{
		m_onDialogueEnded = _onDialogueEnded;
		m_allDialogs.Add(new() { dialogue = _dialogueData, highlightedZoneId = _higlightedZoneID });

		if (m_didEndLastDialogue)
		{
			m_currentDialogueIndex = m_allDialogs.Count - 1;
			m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
			m_currentLineIndex = 0;
			m_currentTextTween?.Kill();
			m_didEndLastDialogue = false;

			Show(false);
			DisplayCurrentLine();
		}

	}

	private void DisplayCurrentLine ()
	{
		DialogueData.Line line = m_currentDialogueData.dialogue.lines[m_currentLineIndex];
		TutorialHighlightZone highlightZone = string.IsNullOrEmpty(m_currentDialogueData.highlightedZoneId) ? null : FTUEManager.Instance.RegisterdTutorialHighlightZones[m_currentDialogueData.highlightedZoneId];
		if (m_currentLineIndex == 0 && highlightZone != null)
		{

			highlightZone.Show();
		}

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

	private void EndDialogue ()
	{
		m_didEndLastDialogue = true;
		ResetScroll();
		//Hide(false);
		m_characterNameTMP.text = "";
		m_dialogueTMP.text = "";
		m_dialogueImg.sprite = null;

		if (!string.IsNullOrEmpty(m_currentDialogueData.highlightedZoneId))
			FTUEManager.Instance.RegisterdTutorialHighlightZones[m_currentDialogueData.highlightedZoneId].Hide();

		if (m_onDialogueEnded != null)
		{
			Action previousAction = new(m_onDialogueEnded);
			m_onDialogueEnded -= previousAction;
			previousAction?.Invoke();
		}

		RefreshButtons();
	}

	private void RefreshButtons ()
	{
		bool canGoPrevious = m_currentDialogueIndex > 0 || m_currentLineIndex > 0
			|| m_didEndLastDialogue;

		bool canGoNext = m_currentDialogueIndex < m_allDialogs.Count - 1
			|| !m_didEndLastDialogue
			|| (m_currentLineIndex < m_currentDialogueData.dialogue.lines.Count - 1);

		m_previousBtn.SetInteractability(canGoPrevious);
		m_nextBtn.SetInteractability(canGoNext);
		m_nextBtn.SetVisible(canGoNext, true);
	}

	private void OnClickPreviousLineOrDialogue ()
	{
		if (m_currentDialogueIndex < 0)
			return;

		if (m_currentTextTween.IsActive())
		{
			m_currentTextTween.Complete();
		}
		else if (m_currentLineIndex > 0)
		{
			m_currentLineIndex--;
			DisplayCurrentLine();
		}
		else if (m_didEndLastDialogue)
		{
			m_didEndLastDialogue = false;
			m_currentLineIndex = m_currentDialogueData.dialogue.lines.Count - 1;
			DisplayCurrentLine();
		}
		else if (m_currentDialogueIndex > 0)
		{
			m_currentDialogueIndex--;
			m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
			m_currentLineIndex = m_currentDialogueData.dialogue.lines.Count - 1;
			DisplayCurrentLine();
		}
	}

	private void OnClickNextLineOrDialogue ()
	{
		if (m_currentDialogueIndex < 0)
			return;

		if (m_currentTextTween.IsActive())
		{
			m_currentTextTween.Complete();
		}
		else if (m_currentLineIndex < m_currentDialogueData.dialogue.lines.Count - 1)
		{
			m_currentLineIndex++;
			DisplayCurrentLine();
		}
		else if (m_currentDialogueIndex < m_allDialogs.Count - 1)
		{
			m_currentDialogueIndex++;
			m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
			m_currentLineIndex = 0;
			DisplayCurrentLine();
		}
		else if (!m_didEndLastDialogue && m_currentLineIndex + 1 == m_currentDialogueData.dialogue.lines.Count)
		{
			EndDialogue();
		}
	}
}
