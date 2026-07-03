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
	[SerializeField] private BaseButton m_validateDialogueButton;
	[SerializeField] private BaseButton m_previousBtn;
    [SerializeField] private BaseButton m_nextBtn;

	[Title("Parameters")]
	[SerializeField] private float m_charactersPerSecond = 30f;

    private List<DialogueData> m_allDialogs = new();
    public List<DialogueData> AllDialogs => m_allDialogs;
	
	private Action m_onDialogueEnded;
	private DialogueData m_currentDialogueData;
	private int m_currentDialogueIndex = -1;
	private bool m_isTextAnimationOn = false;
	private bool m_waitingForNextLine;
	private int m_currentLineIndex;
	private Tween m_currentTextTween;
	private bool m_didEndLastDialogue = false;

	private void Awake ()
	{
		m_previousBtn.onClick += OnClickPreviousLineOrDialogue;
		m_nextBtn.onClick += OnClickNextLineOrDialogue;
		m_validateDialogueButton.onClick += OnClickValidateDialogue;
	}

	private void OnDestroy ()
	{
		m_previousBtn.onClick -= OnClickPreviousLineOrDialogue;
		m_nextBtn.onClick -= OnClickNextLineOrDialogue;
		m_validateDialogueButton.onClick -= OnClickValidateDialogue;
	}

	public void Init ()
	{
		m_allDialogs.Clear();

		Show(false);
    }

    public void Show (bool _isInstant)
	{
		m_dialogueParent.SetActive(true);
	}
    
    public void Hide ( bool _isInstant )
	{
		m_dialogueParent.SetActive(false);
	}

	public void PlayDialogue(DialogueData _dialogueData, Action _onDialogueEnded)
	{
		m_onDialogueEnded = _onDialogueEnded;
		m_allDialogs.Add(_dialogueData);
		m_currentDialogueIndex = m_allDialogs.Count - 1;
		m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
		m_currentLineIndex = 0;

		m_didEndLastDialogue = false;
		m_currentTextTween?.Kill();
		Show(false);

		DisplayCurrentLine();
	}

	private void DisplayCurrentLine ()
	{
		DialogueData.Line line = m_currentDialogueData.lines[m_currentLineIndex];

		m_characterNameTMP.text = line.characterName;
		m_dialogueImg.sprite = line.characterSprite;

		/*RectTransform portraitRect = m_dialogueImg.rectTransform;
		portraitRect.anchorMin = line.isSpriteOnLeft ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
		portraitRect.anchorMax = portraitRect.anchorMin;
		portraitRect.anchoredPosition3D = new Vector3(0, portraitRect.localPosition.y, portraitRect.localPosition.z);*/

		m_dialogueTMP.text = "";
		m_isTextAnimationOn = true;
		float duration = line.sentence.Length / m_charactersPerSecond;

		m_currentTextTween?.Kill();
		m_currentTextTween = DOTween.To(
			() => 0,
			x => m_dialogueTMP.text = line.sentence.Substring(0, x),
			line.sentence.Length,
			duration)
			.SetEase(Ease.Linear)
			.OnStart(() => m_dialogueTMP.text = "")
			.OnComplete(() =>
			{
				m_dialogueTMP.text = line.sentence;
				m_isTextAnimationOn = false;
			}
		);

		RefreshButtons();
	}

	private void EndDialogue ()
	{
		m_currentTextTween?.Kill();
		m_didEndLastDialogue = true;
		//Hide(false);

		Action previousAction = new(m_onDialogueEnded);
		m_onDialogueEnded -= previousAction;
		previousAction?.Invoke();

		RefreshButtons();
	}

	private void RefreshButtons ()
	{
		bool canGoPrevious = m_currentDialogueIndex > 0 || m_currentLineIndex > 0;

		bool canGoNext = m_currentDialogueIndex < m_allDialogs.Count - 1 ||
			(/*!m_didEndLastDialogue &&  */m_currentLineIndex < m_currentDialogueData.lines.Count - 1);

		m_previousBtn.SetInteractability(canGoPrevious);
		m_nextBtn.SetInteractability(canGoNext);
		m_validateDialogueButton.SetInteractability(!m_didEndLastDialogue);
	}

	private void OnClickValidateDialogue ()
	{
		if (m_didEndLastDialogue)
			return;

		if (m_isTextAnimationOn)
		{
			m_currentTextTween.Complete();
			return;
		}

		if (m_currentLineIndex + 1 >= m_currentDialogueData.lines.Count)
		{
			EndDialogue();
			return;
		}

		m_currentLineIndex++;
		DisplayCurrentLine();
	}

	private void OnClickPreviousLineOrDialogue ()
	{
		if (m_currentDialogueIndex < 0)
			return;

		if (m_isTextAnimationOn)
		{
			m_currentTextTween.Complete();
			return;
		}

		if (m_currentLineIndex > 0)
		{
			m_currentLineIndex--;
			DisplayCurrentLine();
			return;
		}

		if (m_currentDialogueIndex > 0)
		{
			m_currentDialogueIndex--;
			m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
			m_currentLineIndex = m_currentDialogueData.lines.Count - 1;
			DisplayCurrentLine();
		}
	}

	private void OnClickNextLineOrDialogue ()
	{
		if (m_currentDialogueIndex < 0)
			return;

		if (m_isTextAnimationOn)
		{
			m_currentTextTween.Complete();
			return;
		}

		if (m_currentLineIndex < m_currentDialogueData.lines.Count - 1)
		{
			m_currentLineIndex++;
			DisplayCurrentLine();
			return;
		}

		if (m_currentDialogueIndex < m_allDialogs.Count - 1)
		{
			m_currentDialogueIndex++;
			m_currentDialogueData = m_allDialogs[m_currentDialogueIndex];
			m_currentLineIndex = 0;
			DisplayCurrentLine();
		}
	}
}
