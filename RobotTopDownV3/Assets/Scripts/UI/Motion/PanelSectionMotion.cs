using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectMask2D), typeof(CanvasGroup))]
public class PanelSectionMotion : MonoBehaviour
{
	public enum Exit
	{
		Down,
		Up,
		Fade,
	}

	[SerializeField] private RectMask2D m_mask;
	[SerializeField] private CanvasGroup m_group;
	[SerializeField] private Image m_scanLine;
	[SerializeField] private Image m_tint;
	[SerializeField] private Exit m_exit = Exit.Down;

	private readonly List<Graphic> m_graphics = new();
	private readonly Vector3[] m_corners = new Vector3[4];
	private RectTransform m_rect;
	private Vector2 m_restPosition;
	private Sequence m_sequence;
	private Rect m_area;
	private Color m_trailColor;
	private bool m_isInitialized;

	public bool IsMoving => m_sequence != null && m_sequence.IsActive();

	private void Awake ()
	{
		Initialize();
	}

	private void OnDestroy ()
	{
		m_sequence?.Kill();
	}

	private void Initialize ()
	{
		if (m_isInitialized)
			return;

		m_isInitialized = true;
		m_rect = (RectTransform)transform;
		m_restPosition = m_rect.anchoredPosition;
	}

	public static void PlayEnter ( IReadOnlyList<PanelSectionMotion> _sections, float _delay )
	{
		float stagger = UIMotion.Settings.revealStagger;
		for (int i = 0; i < _sections.Count; i++)
			_sections[i].PlayEnter(_delay + i * stagger);
	}

	public static void PlayExit ( IReadOnlyList<PanelSectionMotion> _sections )
	{
		float stagger = UIMotion.Settings.exitStagger;
		for (int i = 0; i < _sections.Count; i++)
			_sections[_sections.Count - 1 - i].PlayExit(i * stagger);
	}

	public static bool AreMoving ( IReadOnlyList<PanelSectionMotion> _sections )
	{
		foreach (PanelSectionMotion section in _sections)
			if (section.IsMoving)
				return true;
		return false;
	}

	public static void Rest ( IReadOnlyList<PanelSectionMotion> _sections )
	{
		foreach (PanelSectionMotion section in _sections)
			section.Rest();
	}

	public static void Hide ( IReadOnlyList<PanelSectionMotion> _sections )
	{
		foreach (PanelSectionMotion section in _sections)
			section.Hide();
	}

	public void PlayEnter ( float _delay )
	{
		Rest();
		GameConfig.PanelMotion settings = UIMotion.Settings;
		float step = settings.revealDuration * .08f;
		m_group.alpha = 0f;
		m_group.blocksRaycasts = false;

		m_sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
			.InsertCallback(_delay, BeginReveal)
			.Insert(_delay, DOVirtual.Float(0f, 1f, settings.revealDuration, SetReveal).SetEase(Ease.InOutSine))
			.Insert(_delay, DOTween.Sequence()
				.Append(m_group.DOFade(1f, step))
				.Append(m_group.DOFade(.35f, step * .75f))
				.Append(m_group.DOFade(1f, step))
				.Append(m_group.DOFade(.7f, step))
				.Append(m_group.DOFade(1f, step)))
			.OnComplete(ApplyRest);
	}

	public void PlayExit ( float _delay )
	{
		Rest();
		GameConfig.PanelMotion settings = UIMotion.Settings;
		m_group.blocksRaycasts = false;
		Vector2 direction = m_exit switch
		{
			Exit.Down => Vector2.down,
			Exit.Up => Vector2.up,
			_ => Vector2.zero
		};

		m_sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
			.Insert(_delay, m_rect.DOAnchorPos(m_restPosition + direction * settings.exitDistance, settings.exitDuration).SetEase(Ease.InCubic))
			.Insert(_delay, m_group.DOFade(0f, settings.exitDuration).SetEase(Ease.InQuad));
	}

	public void Hide ()
	{
		Rest();
		m_group.alpha = 0f;
		m_group.blocksRaycasts = false;
	}

	public void Rest ()
	{
		Initialize();
		Sequence sequence = m_sequence;
		m_sequence = null;
		sequence?.Kill();
		ApplyRest();
	}

	private void BeginReveal ()
	{
		m_group.blocksRaycasts = true;
		GameConfig.PanelMotion settings = UIMotion.Settings;
		if (!TryGetContentArea(out m_area))
			return;

		m_mask.softness = new Vector2Int(0, Mathf.RoundToInt(settings.revealSoftness));
		m_mask.enabled = true;

		m_scanLine.rectTransform.sizeDelta = new Vector2(m_area.width, settings.scanLineHeight);
		m_scanLine.color = settings.revealColor;
		m_scanLine.gameObject.SetActive(true);

		m_tint.rectTransform.sizeDelta = new Vector2(m_area.width, Mathf.Min(settings.trailHeight, m_area.height));
		m_trailColor = settings.revealColor;
		m_trailColor.a = settings.trailAlpha;
		m_tint.gameObject.SetActive(true);

		SetReveal(0f);
	}

	private void SetReveal ( float _progress )
	{
		if (!m_mask.enabled)
			return;

		Rect rect = m_rect.rect;
		float softness = m_mask.softness.y;
		float bottom = Mathf.Lerp(m_area.yMax, m_area.yMin - softness, _progress);
		m_mask.padding = new Vector4(m_area.xMin - rect.xMin, bottom - rect.yMin, rect.xMax - m_area.xMax, rect.yMax - m_area.yMax - softness);

		float lineY = Mathf.Max(bottom + softness, m_area.yMin);
		float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.8f, 1f, _progress));
		m_scanLine.rectTransform.localPosition = new Vector3(m_area.center.x, lineY, 0f);
		Color color = m_scanLine.color;
		color.a = fade;
		m_scanLine.color = color;

		m_tint.rectTransform.localPosition = new Vector3(m_area.center.x, lineY, 0f);
		Color trail = m_trailColor;
		trail.a *= fade;
		m_tint.color = trail;
	}

	private bool TryGetContentArea ( out Rect _area )
	{
		GetComponentsInChildren(false, m_graphics);
		Vector2 min = new(float.MaxValue, float.MaxValue);
		Vector2 max = new(float.MinValue, float.MinValue);
		foreach (Graphic graphic in m_graphics)
		{
			if (graphic == m_scanLine || graphic == m_tint || !graphic.enabled)
				continue;

			graphic.rectTransform.GetWorldCorners(m_corners);
			foreach (Vector3 corner in m_corners)
			{
				Vector2 local = m_rect.InverseTransformPoint(corner);
				min = Vector2.Min(min, local);
				max = Vector2.Max(max, local);
			}
		}

		_area = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
		return max.x > min.x && max.y > min.y;
	}

	private void ApplyRest ()
	{
		m_mask.enabled = false;
		m_mask.padding = Vector4.zero;
		m_mask.softness = Vector2Int.zero;
		m_group.alpha = 1f;
		m_group.blocksRaycasts = true;
		m_rect.anchoredPosition = m_restPosition;
		m_scanLine.gameObject.SetActive(false);
		m_tint.gameObject.SetActive(false);
	}
}
