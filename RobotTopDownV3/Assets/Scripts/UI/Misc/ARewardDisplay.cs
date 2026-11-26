using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public abstract class ARewardDisplay : MonoBehaviour
{
	[SerializeField] private GameObject m_selectedGO;
	[SerializeField] private BaseButton m_btn;

	[Header("Selection feedback")]
	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private Graphic m_glow;
	[SerializeField] private GameObject m_checkBadge;
	[SerializeField] private GameObject m_emptyBadge;
	[SerializeField] private float m_glowAlpha = 1f;
	[SerializeField] private float m_declinedAlpha = .42f;
	[SerializeField] private float m_fadeDuration = .1f;
	[SerializeField] private float m_popStrength = .04f;
	[SerializeField] private float m_popDuration = .15f;

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;
	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;
	private System.Action m_onSelected;
	private Sequence m_feedbackTween;
	private Tween m_popTween;

	private void OnDisable ()
	{
		m_feedbackTween?.Complete();
		m_popTween?.Complete();
	}

	protected void InitSelection ( System.Action _onSelected, bool _isLockedOnSelected )
	{
		m_onSelected = _onSelected;

		if (_isLockedOnSelected)
		{
			m_btn.SetInteractability(false);
			SetIsSelected(true);
		}
		else
		{
			m_btn.onClick = OnClick;
			m_btn.SetInteractability(true);
			SetIsSelected(false);
		}
	}

	public void SetInteractable ( bool _isInteractable )
	{
		if (!_isInteractable)
		{
			m_onSelected = null;
			m_btn.onClick = null;
		}

		m_btn.SetInteractability(_isInteractable);
		RefreshFeedback(false);
	}

	private void OnClick ()
	{
		m_isSelected = !m_isSelected;
		RefreshFeedback(true);
		m_onSelected?.Invoke();
	}

	public void SetIsSelected ( bool _isSelected )
	{
		m_isSelected = _isSelected;
		RefreshFeedback(false);
	}

	private void RefreshFeedback ( bool _animate )
	{
		bool isChoosable = m_onSelected != null;
		bool isTaken = isChoosable && m_isSelected;
		float alpha = isChoosable && !m_isSelected ? m_declinedAlpha : 1f;
		float glow = isTaken ? m_glowAlpha : 0f;

		m_selectedGO.SetActive(isTaken);
		m_checkBadge.SetActive(isTaken);
		m_emptyBadge.SetActive(isChoosable && !m_isSelected);
		m_canvasGroup.blocksRaycasts = isChoosable;

		m_feedbackTween?.Kill();
		if (!_animate || !isActiveAndEnabled)
		{
			m_canvasGroup.alpha = alpha;
			Color color = m_glow.color;
			color.a = glow;
			m_glow.color = color;
			return;
		}

		m_feedbackTween = DOTween.Sequence().SetUpdate(true)
			.Join(m_canvasGroup.DOFade(alpha, m_fadeDuration))
			.Join(m_glow.DOFade(glow, m_fadeDuration));

		m_popTween?.Complete();
		m_popTween = transform.DOPunchScale(transform.localScale * m_popStrength, m_popDuration, 1, 0f).SetUpdate(true);
	}

	public void Show ()
	{
		m_isVisible = true;
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		m_isVisible = false;
		m_isSelected = false;
		gameObject.SetActive(false);
	}
}
