using UnityEngine;

public abstract class TooltipCard : MonoBehaviour
{
	[Header("Card")]
	[SerializeField] protected CanvasGroup m_canvasGroup;
	[SerializeField] protected RectTransform m_card;
	[SerializeField] protected float m_screenMargin = 16f;
	[SerializeField] protected float m_fadeDuration = .12f;

	private readonly Vector3[] m_corners = new Vector3[4];
	private float m_targetAlpha;

	public bool IsShown => gameObject.activeSelf && m_targetAlpha > 0f;

	protected void Open ()
	{
		gameObject.SetActive(true);
		m_targetAlpha = 1f;
	}

	public virtual void Hide ()
	{
		m_targetAlpha = 0f;
	}

	private void Update ()
	{
		if (!Mathf.Approximately(m_canvasGroup.alpha, m_targetAlpha))
			m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, m_targetAlpha, m_fadeDuration > 0f ? Time.unscaledDeltaTime / m_fadeDuration : 1f);
		else if (m_targetAlpha <= 0f)
		{
			gameObject.SetActive(false);
			return;
		}

		if (m_targetAlpha > 0f)
			OnShownUpdate();
	}

	protected virtual void OnShownUpdate ()
	{
	}

	protected static Camera CameraOf ( RectTransform _target )
	{
		Canvas canvas = _target == null ? null : _target.GetComponentInParent<Canvas>();
		return canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
	}

	protected static bool IsOnOverlay ( RectTransform _target )
	{
		Canvas canvas = _target == null ? null : _target.GetComponentInParent<Canvas>();
		return canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
	}

	protected Rect ScreenRect ()
	{
		return LocalRect(Vector2.zero, new Vector2(Screen.width, Screen.height));
	}

	protected Rect LocalRectOf ( RectTransform _target, Camera _camera )
	{
		_target.GetWorldCorners(m_corners);
		return LocalRect(RectTransformUtility.WorldToScreenPoint(_camera, m_corners[0]), RectTransformUtility.WorldToScreenPoint(_camera, m_corners[2]));
	}

	private Rect LocalRect ( Vector2 _screenMin, Vector2 _screenMax )
	{
		RectTransform root = (RectTransform)transform;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, _screenMin, null, out Vector2 min);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, _screenMax, null, out Vector2 max);
		return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
	}

	protected void PlaceAbove ( Rect _anchor, float _gap )
	{
		Rect screen = ScreenRect();
		Vector2 size = m_card.rect.size;
		m_card.pivot = Vector2.zero;

		float x = Mathf.Clamp(_anchor.xMin, screen.xMin + m_screenMargin, Mathf.Max(screen.xMin + m_screenMargin, screen.xMax - m_screenMargin - size.x));
		float y = _anchor.yMax + _gap;
		if (y + size.y > screen.yMax - m_screenMargin)
			y = Mathf.Max(screen.yMin + m_screenMargin, _anchor.yMin - _gap - size.y);

		m_card.localPosition = new Vector3(x, y, 0f);
	}

	protected void PlaceAtCursor ( Vector2 _offset )
	{
		Rect screen = ScreenRect();
		Vector2 cursor = Input.mousePosition;
		bool isRightSide = cursor.x > Screen.width * .5f;
		m_card.pivot = new Vector2(isRightSide ? 1f : 0f, 0f);

		Vector2 position = LocalRect(cursor, cursor).min + new Vector2(isRightSide ? -_offset.x : _offset.x, _offset.y);
		Vector2 size = m_card.rect.size;
		float left = isRightSide ? position.x - size.x : position.x;
		left = Mathf.Clamp(left, screen.xMin + m_screenMargin, Mathf.Max(screen.xMin + m_screenMargin, screen.xMax - m_screenMargin - size.x));
		position.x = isRightSide ? left + size.x : left;
		position.y = Mathf.Clamp(position.y, screen.yMin + m_screenMargin, Mathf.Max(screen.yMin + m_screenMargin, screen.yMax - m_screenMargin - size.y));
		m_card.localPosition = position;
	}
}
