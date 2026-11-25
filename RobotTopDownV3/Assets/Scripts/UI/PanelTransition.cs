using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Sirenix.OdinInspector;

public class PanelTransition : MonoBehaviour
{
	public enum Style
	{
		None,
		DiveAndDoors,
		HoloSweep,
	}

	private const float k_readyTimeout = 6f;

	private static readonly int s_sweepID = Shader.PropertyToID("_PanelTransitionSweep");
	private static readonly int s_doorsID = Shader.PropertyToID("_PanelTransitionDoors");
	private static readonly int s_tintTrailID = Shader.PropertyToID("_TintTrail");

	[Title("Dive and doors")]
	[SerializeField] private float m_panelFadeDuration = .25f;
	[SerializeField] private float m_diveDuration = 1.4f;
	[SerializeField] private float m_doorsCloseDuration = .35f;
	[SerializeField] private float m_doorsImpactDuration = .12f;
	[SerializeField] private float m_doorsImpactShake = 6f;
	[SerializeField] private float m_doorsHoldDuration = .2f;
	[SerializeField] private float m_doorsOpenDuration = .45f;
	[SerializeField] private float m_hangarPushDistance = 1f;
	[SerializeField] private float m_hangarPushDuration = .9f;
	[SerializeField] private float m_captionFadeDuration = .25f;

	[Title("Holo sweep")]
	[SerializeField] private float m_sweepDuration = .7f;
	[SerializeField] private float m_captureInterval = .1f;

	[Title("References")]
	[SerializeField] private RawImage m_captureImage;
	[SerializeField] private Image m_doorsImage;
	[SerializeField] private CanvasGroup m_captionGroup;
	[SerializeField] private TextMeshProUGUI m_captionTMP;
	[SerializeField] private TextMeshProUGUI m_captionProgressTMP;
	[SerializeField] private RectTransform m_captionProgressFill;
	[SerializeField] private Image m_blocker;

	private RenderTexture m_capture;
	private AUIPanel m_capturedPanel;
	private float m_nextCaptureTime;
	private int m_shownDoorsPercent = -1;
	private bool m_isPlaying;
	private bool m_isSwitchingPanel;
	public bool IsSwitchingPanel => m_isSwitchingPanel;

	private void Awake ()
	{
		StartCoroutine(CaptureLoop());
	}

	private void OnDestroy ()
	{
		if (m_capture != null)
		{
			m_capture.Release();
			Destroy(m_capture);
		}
	}

	public static Style GetStyle ( AUIPanel _from, AUIPanel _to )
	{
		if (_from == null || _to == null)
			return Style.None;

		if ((_from is HangarPanel && IsHubPlacePanel(_to)) || (_to is HangarPanel && IsHubPlacePanel(_from)))
			return Style.DiveAndDoors;

		if (IsPair<SoloHubPanel, MissionPanel>(_from, _to) || IsPair<SoloHubPanel, TournamentPanel>(_from, _to) || IsPair<HangarPanel, EntityConfigPanel>(_from, _to))
			return Style.HoloSweep;

		return Style.None;
	}

	private static bool IsPair<TFirst, TSecond> ( AUIPanel _from, AUIPanel _to )
	{
		return (_from is TFirst && _to is TSecond) || (_from is TSecond && _to is TFirst);
	}

	private static bool IsHubPlacePanel ( AUIPanel _panel )
	{
		return _panel is SoloHubPanel or ShopPanel or RecyclePanel or RepairStationPanel or MissionPanel or TournamentPanel or SelectMissionPanel;
	}

	private static bool IsOpening ( AUIPanel _to )
	{
		return _to is MissionPanel or TournamentPanel or EntityConfigPanel;
	}

	private static bool IsHoloSweepSource ( AUIPanel _panel )
	{
		return _panel is SoloHubPanel or MissionPanel or TournamentPanel or HangarPanel or EntityConfigPanel;
	}

	public bool TryPlay ( AUIPanel _from, AUIPanel _to, Action _switchPanel )
	{
		Style style = GetStyle(_from, _to);
		if (style == Style.None)
			return false;

		if (m_isPlaying)
			return true;

		if (style == Style.HoloSweep)
		{
			if (m_capturedPanel != _from)
				return false;

			UIManager.Instance.StartCoroutine(PlayHoloSweep(_switchPanel, IsOpening(_to)));
			return true;
		}

		UIManager.Instance.StartCoroutine(PlayDiveAndDoors(_from, _to, _switchPanel));
		return true;
	}

	public bool TryPlayCoverSweep ( Action _onFinished )
	{
		if (m_isPlaying || m_capturedPanel == null || m_capturedPanel != UIManager.Instance.currentPanel)
			return false;

		UIManager.Instance.StartCoroutine(PlayHoloSweep(null, true, _onFinished));
		return true;
	}

	#region Overlay

	private void SetBlocking ( bool _isBlocking )
	{
		m_blocker.gameObject.SetActive(_isBlocking);
	}

	private static void SetSweep ( float _progress, float _direction )
	{
		Shader.SetGlobalVector(s_sweepID, new Vector4(_progress, _direction, Screen.width, Screen.height));
	}

	private static void SetDoors ( float _closure, float _offset )
	{
		Shader.SetGlobalVector(s_doorsID, new Vector4(_closure, _offset, Screen.width, Screen.height));
	}

	#endregion

	#region Capture

	private IEnumerator CaptureLoop ()
	{
		WaitForEndOfFrame endOfFrame = new();
		while (true)
		{
			yield return null;

			AUIPanel panel = UIManager.Instance != null ? UIManager.Instance.currentPanel : null;
			if (m_isPlaying || !IsHoloSweepSource(panel))
			{
				m_capturedPanel = null;
				continue;
			}

			if (panel == m_capturedPanel && Time.unscaledTime < m_nextCaptureTime && !IsPointerPressed())
				continue;

			yield return endOfFrame;

			if (m_isPlaying || UIManager.Instance == null || UIManager.Instance.currentPanel != panel)
			{
				m_capturedPanel = null;
				continue;
			}

			EnsureCaptureTexture();
			ScreenCapture.CaptureScreenshotIntoRenderTexture(m_capture);
			m_capturedPanel = panel;
			m_nextCaptureTime = Time.unscaledTime + m_captureInterval;
		}
	}

	private static bool IsPointerPressed ()
	{
		Pointer pointer = Pointer.current;
		return pointer != null && pointer.press.isPressed;
	}

	private void EnsureCaptureTexture ()
	{
		if (m_capture != null && m_capture.width == Screen.width && m_capture.height == Screen.height)
			return;

		if (m_capture != null)
		{
			m_capture.Release();
			Destroy(m_capture);
		}

		m_capture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32) { name = "PanelTransitionCapture" };
		m_capture.Create();
		m_captureImage.texture = m_capture;
	}

	#endregion

	#region Holo sweep

	private IEnumerator PlayHoloSweep ( Action _switchPanel, bool _isOpening, Action _onFinished = null )
	{
		m_isPlaying = true;
		m_isSwitchingPanel = _switchPanel != null;
		SetBlocking(true);

		float direction = _isOpening ? 1f : -1f;
		SetSweep(0f, direction);
		m_captureImage.gameObject.SetActive(true);

		_switchPanel?.Invoke();
		SoundManager.PlayUISafe(SfxId.None, _config => _config.transitionHoloSweep);

		yield return null;

		float travel = 1f + m_captureImage.material.GetFloat(s_tintTrailID);
		yield return Animate(m_sweepDuration * travel, _progress =>
		{
			float position = EaseInOut(_progress) * travel;
			SetSweep(position, direction);
			if (position >= 1f)
				SetBlocking(false);
		});

		SetBlocking(false);
		m_captureImage.gameObject.SetActive(false);
		m_isPlaying = false;
		m_isSwitchingPanel = false;
		_onFinished?.Invoke();
	}

	#endregion

	#region Dive and doors

	private IEnumerator PlayDiveAndDoors ( AUIPanel _from, AUIPanel _to, Action _switchPanel )
	{
		m_isPlaying = true;
		m_isSwitchingPanel = true;
		SetBlocking(true);

		HubManager hub = HubManager.Instance;
		bool isEnteringHangar = _to is HangarPanel;
		if (hub != null)
			hub.SetCameraHeld(true);

		_from.FadeContainers(false, m_panelFadeDuration);

		float leadDuration = 0f;
		if (hub != null)
			leadDuration = isEnteringHangar
				? hub.DiveToBuilding(HubManager.Building.Hangar, m_diveDuration)
				: hub.PullBackInHangar(m_hangarPushDistance, m_doorsCloseDuration + m_doorsImpactDuration);

		yield return Wait(Mathf.Max(m_panelFadeDuration, leadDuration - m_doorsCloseDuration));

		SetDoors(0f, 0f);
		m_doorsImage.gameObject.SetActive(true);
		SoundManager.PlayUISafe(SfxId.None, _config => _config.transitionDoorsClose);
		yield return Animate(m_doorsCloseDuration, _progress => SetDoors(_progress * _progress * _progress, 0f));
		yield return Animate(m_doorsImpactDuration, _progress => SetDoors(1f, Mathf.Sin(_progress * Mathf.PI * 4f) * m_doorsImpactShake * (1f - _progress)));
		SetDoors(1f, 0f);

		_switchPanel();
		if (hub != null && !isEnteringHangar)
			hub.PlaceCameraOnBuilding(HubManager.Building.Hangar);

		float elapsed = 0f;
		while (elapsed < m_doorsHoldDuration || (hub != null && elapsed < k_readyTimeout && (isEnteringHangar ? !hub.IsHangarReady : hub.IsHangarShown)))
		{
			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		if (hub != null)
		{
			hub.SetCameraHeld(false);
			if (isEnteringHangar)
				hub.PushIntoHangar(m_hangarPushDistance, m_hangarPushDuration);
			else
				hub.FocusCurrentBuilding();
		}

		SoundManager.PlayUISafe(SfxId.None, _config => _config.transitionDoorsOpen);
		yield return Animate(m_doorsOpenDuration, _progress => SetDoors(Mathf.Pow(1f - _progress, 3f), 0f));

		m_doorsImage.gameObject.SetActive(false);
		SetBlocking(false);
		m_isPlaying = false;
		m_isSwitchingPanel = false;
	}

	public float DoorsCloseDuration => m_doorsCloseDuration + m_doorsImpactDuration;

	public void BlockInput ( bool _isBlocked )
	{
		SetBlocking(_isBlocked);
	}

	public IEnumerator CloseDoors ( string _caption )
	{
		m_isPlaying = true;
		SetBlocking(true);
		SetDoors(0f, 0f);
		m_doorsImage.gameObject.SetActive(true);
		m_captionTMP.text = _caption;
		m_shownDoorsPercent = -1;
		SetDoorsProgress(0f);

		SoundManager.PlayUISafe(SfxId.None, _config => _config.transitionDoorsClose);
		yield return Animate(m_doorsCloseDuration, _progress => SetDoors(_progress * _progress * _progress, 0f));
		yield return Animate(m_doorsImpactDuration, _progress => SetDoors(1f, Mathf.Sin(_progress * Mathf.PI * 4f) * m_doorsImpactShake * (1f - _progress)));
		SetDoors(1f, 0f);

		m_captionGroup.alpha = 0f;
		m_captionGroup.gameObject.SetActive(true);
		yield return Animate(m_captionFadeDuration, _progress => m_captionGroup.alpha = _progress);
	}

	public void SetDoorsProgress ( float _progress )
	{
		int percent = Mathf.RoundToInt(Mathf.Clamp01(_progress) * 100f);
		if (percent == m_shownDoorsPercent)
			return;

		m_shownDoorsPercent = percent;
		m_captionProgressFill.anchorMax = new Vector2(percent * .01f, 1f);
		m_captionProgressTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.briefing_loading), percent);
	}

	public IEnumerator OpenDoors ()
	{
		yield return Animate(m_captionFadeDuration, _progress => m_captionGroup.alpha = 1f - _progress);
		m_captionGroup.gameObject.SetActive(false);

		SoundManager.PlayUISafe(SfxId.None, _config => _config.transitionDoorsOpen);
		yield return Animate(m_doorsOpenDuration, _progress => SetDoors(Mathf.Pow(1f - _progress, 3f), 0f));

		m_doorsImage.gameObject.SetActive(false);
		SetBlocking(false);
		m_isPlaying = false;
	}

	#endregion

	private static IEnumerator Wait ( float _duration )
	{
		for (float elapsed = 0f; elapsed < _duration; elapsed += Time.unscaledDeltaTime)
			yield return null;
	}

	private static IEnumerator Animate ( float _duration, Action<float> _apply )
	{
		for (float elapsed = 0f; elapsed < _duration; elapsed += Time.unscaledDeltaTime)
		{
			_apply(elapsed / _duration);
			yield return null;
		}

		_apply(1f);
	}

	private static float EaseInOut ( float _progress )
	{
		return _progress < .5f ? 2f * _progress * _progress : 1f - Mathf.Pow(-2f * _progress + 2f, 2f) * .5f;
	}
}
