using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class UIMotion
{
	public static GameConfig.PanelMotion Settings => GameConfig.current.ui.motion;

	public static Tween Scale ( Transform _target, float _scale, float _duration )
	{
		return _target.DOScale(_scale, _duration).SetEase(Ease.OutCubic).SetUpdate(true);
	}

	public static Tween Shine ( Image _shine )
	{
		GameConfig.PanelMotion settings = Settings;
		Color color = _shine.color;
		color.a = settings.shineAlpha;
		_shine.color = color;
		_shine.fillAmount = 0f;
		_shine.gameObject.SetActive(true);

		return DOTween.Sequence().SetUpdate(true).SetLink(_shine.gameObject)
			.Join(_shine.DOFillAmount(1f, settings.shineDuration).SetEase(Ease.OutSine))
			.Join(_shine.DOFade(0f, settings.shineDuration).SetEase(Ease.InQuad))
			.OnKill(() => _shine.gameObject.SetActive(false));
	}

	public static Tween Swap ( RectTransform _content, CanvasGroup _group, Vector2 _restPosition )
	{
		GameConfig.PanelMotion settings = Settings;
		_content.anchoredPosition = _restPosition + Vector2.down * settings.swapDistance;
		_group.alpha = 0f;

		return DOTween.Sequence().SetUpdate(true).SetLink(_content.gameObject)
			.Join(_content.DOAnchorPos(_restPosition, settings.swapDuration).SetEase(Ease.OutCubic))
			.Join(_group.DOFade(1f, settings.swapDuration * .8f))
			.OnKill(() =>
			{
				_content.anchoredPosition = _restPosition;
				_group.alpha = 1f;
			});
	}

	public static void Fill ( RectTransform _fill, float _value, bool _animate )
	{
		Vector2 anchorMax = new(Mathf.Clamp01(_value), 1f);
		_fill.DOKill();
		if (_animate)
			_fill.DOAnchorMax(anchorMax, Settings.fillDuration).SetEase(Ease.OutCubic).SetUpdate(true);
		else
			_fill.anchorMax = anchorMax;
	}

	public static Tween Grow ( Transform _target )
	{
		_target.DOKill();
		_target.localScale = new Vector3(0f, 1f, 1f);
		return _target.DOScaleX(1f, Settings.growDuration).SetEase(Ease.OutCubic).SetUpdate(true);
	}
}
