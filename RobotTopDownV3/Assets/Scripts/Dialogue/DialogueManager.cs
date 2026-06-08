using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using System;
using UnityEngine.UI;
using DG.Tweening;

public class DialogueManager : Singleton<DialogueManager>
{
	[Title("Dependancies")]
	[SerializeField] private GameObject m_dialogueParent;
	[SerializeField] private TextMeshProUGUI m_dialogueTMP;
	[SerializeField] private TextMeshProUGUI m_characterNameTMP;
	[SerializeField] private Image m_dialogueImg;
	[SerializeField] private BaseButton m_shipDialogueButton;

	[Title("Parameters")]
	[SerializeField] private float m_charactersPerSecond = 30f;

	private DialogueData m_currentDialogueData;
	private Action m_onDialogueEnded;
	private bool m_isTextAnimationOn = false;
	private bool m_waitingForNextLine;
	private int m_currentLineIndex;
	private Tween m_currentTextTween;

	public override void Awake ()
	{
		base.Awake();
		m_shipDialogueButton.onClick += OnClickSkipDialogue;
		m_dialogueParent.SetActive(false);
	}

	private void OnDestroy ()
	{
		m_shipDialogueButton.onClick -= OnClickSkipDialogue;
	}

	public void PlayDialogue ( DialogueData _dialogueData, Action _onDialogueEnded )
	{
		if (_dialogueData == null || _dialogueData.lines.Count == 0)
		{
			_onDialogueEnded?.Invoke();
			return;
		}

		m_currentTextTween?.Kill();

		m_currentDialogueData = _dialogueData;
		m_onDialogueEnded = _onDialogueEnded;
		m_currentLineIndex = 0;

		m_dialogueParent.SetActive(true);

		DisplayCurrentLine();
	}

	private void DisplayCurrentLine ()
	{
		DialogueData.Line line = m_currentDialogueData.lines[m_currentLineIndex];

		m_characterNameTMP.text = line.characterName;
		m_dialogueImg.sprite = line.characterSprite;

		RectTransform portraitRect = m_dialogueImg.rectTransform;
		portraitRect.anchorMin = line.isSpriteOnLeft ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
		portraitRect.anchorMax = portraitRect.anchorMin;
		portraitRect.anchoredPosition3D = new Vector3(0, portraitRect.localPosition.y, portraitRect.localPosition.z);

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

	}

	private void OnClickSkipDialogue ()
	{
		if (m_currentDialogueData == null)
			return;

		if (m_isTextAnimationOn)
		{
			m_currentTextTween.Complete();
			return;
		}

		m_currentLineIndex++;

		if (m_currentLineIndex >= m_currentDialogueData.lines.Count)
		{
			EndDialogue();
			return;
		}

		DisplayCurrentLine();
	}

	private void EndDialogue ()
	{
		m_currentTextTween?.Kill();

		m_dialogueParent.SetActive(false);

		m_currentDialogueData = null;

		m_onDialogueEnded?.Invoke();
		m_onDialogueEnded = null;
	}
}
