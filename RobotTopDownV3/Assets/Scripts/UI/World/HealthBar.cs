using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
	[SerializeField] private Image m_fill;
	[SerializeField] private Image m_trail;
	[SerializeField] private Image m_outline;
	[SerializeField] private RectTransform[] m_notches;
	[SerializeField] private TextMeshProUGUI m_valueText;
	[SerializeField] private Color m_allyColor = new(.35f, .85f, 1f);
	[SerializeField] private Color m_enemyColor = new(1f, .35f, .3f);
	[SerializeField] private Color m_outlineColor = new(1f, 1f, 1f, .3f);
	[SerializeField, Min(1)] private int m_healthPerNotch = 25;
	[SerializeField, Range(0f, 1f)] private float m_criticalRatio = .25f;
	[SerializeField, Range(0f, 1f)] private float m_criticalPulseAlpha = .25f;
	[SerializeField] private float m_criticalPulseDuration = .35f;
	[SerializeField] private float m_trailDelay = .3f;
	[SerializeField] private float m_trailDuration = .35f;

	private Color m_teamColor = Color.white;
	private int m_notchedMax = -1;
	private bool m_isCritical;
	private Tween m_trailTween;
	private Tween m_pulseTween;

	public void Init ( bool _isAlly, int _current, int _max )
	{
		m_teamColor = _isAlly ? m_allyColor : m_enemyColor;
		m_fill.color = m_teamColor;
		ShowValue(false);
		SetHealth(_current, _max, false);
		RefreshPulse();
	}

	public void SetHealth ( int _current, int _max, bool _animate = true )
	{
		float ratio = _max > 0 ? Mathf.Clamp01((float)_current / _max) : 0f;

		m_trailTween?.Kill();
		m_trailTween = null;
		if (_animate && isActiveAndEnabled && ratio < m_fill.fillAmount)
			m_trailTween = m_trail.DOFillAmount(ratio, m_trailDuration).SetDelay(m_trailDelay).SetEase(Ease.OutQuad);
		else
			m_trail.fillAmount = ratio;

		m_fill.fillAmount = ratio;
		m_valueText.text = Mathf.Max(0, _current).ToString();
		RefreshNotches(_max);
		SetCritical(_current > 0 && ratio <= m_criticalRatio);
	}

	public void ShowValue ( bool _isShown )
	{
		m_valueText.gameObject.SetActive(_isShown);
	}

	private void RefreshNotches ( int _max )
	{
		if (_max == m_notchedMax)
			return;

		m_notchedMax = _max;
		for (int i = 0; i < m_notches.Length; i++)
		{
			int value = (i + 1) * m_healthPerNotch;
			bool isShown = value < _max;
			m_notches[i].gameObject.SetActive(isShown);
			if (isShown)
				m_notches[i].anchorMin = m_notches[i].anchorMax = new Vector2((float)value / _max, .5f);
		}
	}

	private void SetCritical ( bool _isCritical )
	{
		if (_isCritical == m_isCritical)
			return;

		m_isCritical = _isCritical;
		RefreshPulse();
	}

	private void RefreshPulse ()
	{
		m_pulseTween?.Kill();
		m_pulseTween = null;

		if (!m_isCritical)
		{
			m_outline.color = m_outlineColor;
			return;
		}

		m_outline.color = m_teamColor;
		if (isActiveAndEnabled)
			m_pulseTween = m_outline.DOFade(m_criticalPulseAlpha, m_criticalPulseDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
	}

	private void OnEnable ()
	{
		RefreshPulse();
	}

	private void OnDisable ()
	{
		m_trailTween?.Kill();
		m_trailTween = null;
		m_trail.fillAmount = m_fill.fillAmount;
		m_pulseTween?.Kill();
		m_pulseTween = null;
	}
}
