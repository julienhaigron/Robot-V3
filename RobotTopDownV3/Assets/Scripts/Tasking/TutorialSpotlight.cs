using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSpotlight : MonoBehaviour
{
	private const int MAX_HOLES = 8;
	private static readonly int s_holesID = Shader.PropertyToID("_TutorialSpotlightHoles");
	private static readonly int s_stateID = Shader.PropertyToID("_TutorialSpotlight");
	private static readonly Vector2[] s_cornerDirections = { new(-1f, 1f), new(1f, 1f), new(1f, -1f), new(-1f, -1f) };
	private static readonly List<RectTransform> s_litRects = new();

	[Serializable]
	private class Reticle
	{
		public RectTransform root;
		public CanvasGroup group;
		public RectTransform[] brackets;
		[NonSerialized] public TutorialHighlightZone zone;
		[NonSerialized] public Sequence sequence;
	}

	[Header("Spotlight")]
	[SerializeField] private Graphic m_dim;
	[SerializeField] private float m_holeMargin = 10f;
	[SerializeField] private float m_fadeDuration = .3f;

	[Header("Reticle")]
	[SerializeField] private Reticle[] m_reticles;
	[SerializeField] private float m_bracketGap = 8f;
	[SerializeField] private float m_screenInset = 16f;
	[SerializeField, Range(0f, 1f)] private float m_bracketLengthRatio = .32f;
	[SerializeField] private Vector2 m_bracketLengthRange = new(18f, 42f);
	[SerializeField] private float m_lockDistance = 36f;
	[SerializeField] private float m_lockDuration = .45f;
	[SerializeField] private float m_hoverDistance = 6f;
	[SerializeField] private float m_hoverDuration = .8f;

	private readonly Vector4[] m_holes = new Vector4[MAX_HOLES];
	private int m_holeCount;
	private float m_fade;

	public static void AddLit ( RectTransform _rect )
	{
		if (!s_litRects.Contains(_rect))
			s_litRects.Add(_rect);
	}

	public static void RemoveLit ( RectTransform _rect )
	{
		s_litRects.Remove(_rect);
	}

	private void OnDisable ()
	{
		foreach (Reticle reticle in m_reticles)
			Release(reticle);

		m_fade = 0f;
		m_dim.enabled = false;
	}

	private void LateUpdate ()
	{
		RefreshReticles();

		bool isSpotlit = CollectHoles();
		m_fade = Mathf.MoveTowards(m_fade, isSpotlit ? 1f : 0f, m_fadeDuration > 0f ? Time.unscaledDeltaTime / m_fadeDuration : 1f);
		m_dim.enabled = m_fade > 0f;
		if (!m_dim.enabled)
			return;

		Shader.SetGlobalVectorArray(s_holesID, m_holes);
		Shader.SetGlobalVector(s_stateID, new Vector4(m_fade, m_holeCount, Screen.width, Screen.height));
	}

	private bool CollectHoles ()
	{
		int count = 0;
		foreach (TutorialHighlightZone zone in TutorialHighlightZone.ActiveHighlightZones)
			if (IsShown(zone) && zone.IsSpotlit && count < MAX_HOLES)
				m_holes[count++] = HoleOf((RectTransform)zone.transform, m_holeMargin);

		if (count == 0)
			return false;

		foreach (RectTransform lit in s_litRects)
			if (count < MAX_HOLES)
				m_holes[count++] = HoleOf(lit, 0f);

		m_holeCount = count;
		return true;
	}

	private static Vector4 HoleOf ( RectTransform _rect, float _margin )
	{
		Rect rect = _rect.GetWorldRect();
		return new Vector4(rect.xMin - _margin, rect.yMin - _margin, rect.xMax + _margin, rect.yMax + _margin);
	}

	private static bool IsShown ( TutorialHighlightZone _zone )
	{
		return _zone != null && _zone.IsVisible && _zone.gameObject.activeInHierarchy;
	}

	private void RefreshReticles ()
	{
		foreach (Reticle reticle in m_reticles)
			if (reticle.zone != null && !IsShown(reticle.zone))
				Release(reticle);

		foreach (TutorialHighlightZone zone in TutorialHighlightZone.ActiveHighlightZones)
			if (IsShown(zone) && ReticleOf(zone) == null)
			{
				Reticle free = ReticleOf(null);
				if (free != null)
					Lock(free, zone);
			}

		foreach (Reticle reticle in m_reticles)
			if (reticle.zone != null)
				Place(reticle);
	}

	private Reticle ReticleOf ( TutorialHighlightZone _zone )
	{
		foreach (Reticle reticle in m_reticles)
			if (reticle.zone == _zone)
				return reticle;

		return null;
	}

	private void Place ( Reticle _reticle )
	{
		Rect rect = ((RectTransform)_reticle.zone.transform).GetWorldRect();
		RectTransform root = (RectTransform)transform;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, rect.min, null, out Vector2 min);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, rect.max, null, out Vector2 max);
		Rect bounds = root.rect;
		Vector2 inset = new(m_screenInset, m_screenInset);
		min = Vector2.Max(min, bounds.min + inset);
		max = Vector2.Min(max, bounds.max - inset);
		_reticle.root.localPosition = min;
		_reticle.root.sizeDelta = Vector2.Max(max - min, Vector2.zero);
	}

	private void Lock ( Reticle _reticle, TutorialHighlightZone _zone )
	{
		_reticle.zone = _zone;
		_reticle.root.gameObject.SetActive(true);
		Place(_reticle);

		Vector2 size = _reticle.root.sizeDelta;
		float length = Mathf.Clamp(Mathf.Min(size.x, size.y) * m_bracketLengthRatio, m_bracketLengthRange.x, m_bracketLengthRange.y);

		_reticle.sequence?.Kill();
		_reticle.group.alpha = 0f;
		Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(_reticle.root.gameObject);
		sequence.Join(_reticle.group.DOFade(1f, m_lockDuration * .5f));
		for (int i = 0; i < _reticle.brackets.Length; i++)
		{
			RectTransform bracket = _reticle.brackets[i];
			bracket.sizeDelta = new Vector2(length, length);
			bracket.anchoredPosition = s_cornerDirections[i] * (m_bracketGap + m_lockDistance);
			sequence.Join(bracket.DOAnchorPos(s_cornerDirections[i] * m_bracketGap, m_lockDuration).SetEase(Ease.OutBack));
		}

		sequence.OnComplete(() => Hover(_reticle));
		_reticle.sequence = sequence;
	}

	private void Hover ( Reticle _reticle )
	{
		Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(_reticle.root.gameObject).SetLoops(-1, LoopType.Yoyo);
		for (int i = 0; i < _reticle.brackets.Length; i++)
			sequence.Join(_reticle.brackets[i].DOAnchorPos(s_cornerDirections[i] * (m_bracketGap + m_hoverDistance), m_hoverDuration).SetEase(Ease.InOutSine));

		_reticle.sequence = sequence;
	}

	private static void Release ( Reticle _reticle )
	{
		_reticle.sequence?.Kill();
		_reticle.sequence = null;
		_reticle.zone = null;
		_reticle.root.gameObject.SetActive(false);
	}
}
