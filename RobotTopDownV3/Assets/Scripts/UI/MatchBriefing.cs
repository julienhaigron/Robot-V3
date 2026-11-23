using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class MatchBriefing : MonoBehaviour, ILoadingScreen
{
	private const int k_standbyLabel = -1;
	private const int k_onlineLabel = 101;

	[Serializable]
	private struct UnitLabel
	{
		public RectTransform rect;
		public TextMeshProUGUI name;
		public TextMeshProUGUI status;
	}

	[Title("Briefing")]
	[SerializeField] private float m_minimumDuration = 2.8f;
	[SerializeField] private float m_fadeInDuration = .3f;
	[SerializeField] private float m_readyBeatDuration = .9f;
	[SerializeField] private Color m_transferColor = new(.5f, .82f, 1f);
	[SerializeField] private Color m_onlineColor = new(.56f, .9f, .7f);

	[Title("Arrival")]
	[SerializeField] private float m_briefingFadeOutDuration = .7f;
	[SerializeField] private float m_squadZoomOnFadeOut = 1.12f;
	[SerializeField] private float m_descentDelay = .2f;
	[SerializeField] private float m_descentDuration = 2.2f;
	[SerializeField] private float m_macroFramingMargin = 1.1f;
	[SerializeField, Range(0f, 1f)] private float m_materializeAt = .8f;
	[SerializeField] private float m_materializeStagger = .08f;
	[SerializeField] private float m_bannerFadeDuration = .35f;
	[SerializeField] private float m_bannerHoldDuration = 1.8f;

	[Title("References")]
	[SerializeField] private CanvasGroup m_briefingGroup;
	[SerializeField] private RectTransform m_squadRect;
	[SerializeField] private RawImage m_squadView;
	[SerializeField] private SquadStage m_squadStage;
	[SerializeField] private UnitLabel[] m_unitLabels;
	[SerializeField] private CanvasGroup m_readyBanner;
	[SerializeField] private TextMeshProUGUI m_labelTMP;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private TextMeshProUGUI m_goalTMP;
	[SerializeField] private TextMeshProUGUI m_squadHeaderTMP;
	[SerializeField] private TextMeshProUGUI m_squadTMP;
	[SerializeField] private TextMeshProUGUI m_opponentsTMP;
	[SerializeField] private TextMeshProUGUI m_loadingTMP;
	[SerializeField] private RectTransform m_progressFill;
	[SerializeField] private BaseButton m_startButton;
	[SerializeField] private CanvasGroup m_bannerGroup;
	[SerializeField] private TextMeshProUGUI m_bannerTMP;

	private bool m_isShown;
	private bool m_isStartRequested;
	private float m_elapsed;
	private float m_loadProgress;
	private float m_displayedProgress;
	private int m_shownPercent = -1;
	private int[] m_labelStates;
	private bool m_isReadyShown;

	private void Awake ()
	{
		m_startButton.onClick += OnClickStart;
		m_labelStates = new int[m_unitLabels.Length];
	}

	private void OnDestroy ()
	{
		if (m_startButton != null)
			m_startButton.onClick -= OnClickStart;
		if (m_squadStage != null)
			Destroy(m_squadStage.gameObject);
	}

	private void Update ()
	{
		if (!m_isShown)
			return;

		m_elapsed += Time.unscaledDeltaTime;
		float target = Mathf.Min(m_minimumDuration > 0f ? m_elapsed / m_minimumDuration : 1f, m_loadProgress);
		float displayedProgress = Mathf.Max(m_displayedProgress, Mathf.Clamp01(target));
		if (displayedProgress != m_displayedProgress)
		{
			m_displayedProgress = displayedProgress;
			ApplyProgress();
		}

		RefreshUnitLabels();
		if (m_squadStage.IsReady && !m_isReadyShown)
		{
			m_isReadyShown = true;
			m_readyBanner.DOFade(1f, m_bannerFadeDuration).SetUpdate(true);
		}
	}

	private void RefreshUnitLabels ()
	{
		Rect area = m_squadRect.rect;
		for (int i = 0; i < m_unitLabels.Length; i++)
		{
			UnitLabel label = m_unitLabels[i];
			bool isUsed = i < m_squadStage.UnitCount;
			if (label.rect.gameObject.activeSelf != isUsed)
				label.rect.gameObject.SetActive(isUsed);
			if (!isUsed)
				continue;

			Vector2 anchor = m_squadStage.GetLabelAnchor(i);
			label.rect.anchoredPosition = new Vector2((anchor.x - .5f) * area.width, (anchor.y - .5f) * area.height);

			float reveal = m_squadStage.GetReveal(i);
			int state = reveal <= 0f ? k_standbyLabel : reveal >= 1f ? k_onlineLabel : Mathf.Clamp(Mathf.RoundToInt(reveal * 100f), 0, 99);
			if (state == m_labelStates[i])
				continue;

			m_labelStates[i] = state;
			LocalizationManager localization = LocalizationManager.Instance;
			label.status.text = state == k_standbyLabel ? localization.Get(LocalizationKey.briefing_unit_standby)
				: state == k_onlineLabel ? localization.Get(LocalizationKey.briefing_unit_online)
				: string.Format(localization.Get(LocalizationKey.briefing_unit_transfer), state);
			label.status.color = state == k_onlineLabel ? m_onlineColor : m_transferColor;
		}
	}

	public void Prepare ( MissionData _mission )
	{
		LocalizationManager localization = LocalizationManager.Instance;

		m_labelTMP.text = localization.Get(LocalizationKey.briefing_label);
		m_titleTMP.text = _mission.type.GetLocalizedTitle();
		m_goalTMP.text = _mission.type.GetLocalizedGoal();
		m_squadHeaderTMP.text = localization.Get(LocalizationKey.briefing_squad);
		m_opponentsTMP.text = string.Format(localization.Get(LocalizationKey.briefing_opponents), _mission.enemies != null ? _mission.enemies.Length : 0);

		StringBuilder squad = new();
		foreach (EntitySavedData unit in GameDatas.current.currentPlayerSave.GetSquadEntitiesData())
		{
			if (unit != null)
				squad.AppendLine(unit.name);
		}
		m_squadTMP.text = squad.ToString();

		m_bannerTMP.text = "<size=125%>" + localization.Get(LocalizationKey.briefing_start) + "</size>\n<size=80%>" + m_goalTMP.text + "</size>";
	}

	private void OnClickStart ()
	{
		m_isStartRequested = true;
	}

	#region ILoadingScreen

	public void Show ( Action _onCovered )
	{
		m_isShown = true;
		m_isStartRequested = false;
		m_elapsed = 0f;
		m_loadProgress = 0f;
		m_displayedProgress = 0f;
		m_shownPercent = -1;

		m_squadRect.localScale = Vector3.one;
		m_startButton.SetVisible(false, true);
		m_bannerGroup.alpha = 0f;
		m_bannerGroup.gameObject.SetActive(false);
		m_briefingGroup.blocksRaycasts = true;
		m_briefingGroup.alpha = 1f;
		m_briefingGroup.gameObject.SetActive(true);
		m_squadView.texture = m_squadStage.Show(GameDatas.current.currentPlayerSave.GetSquadEntitiesData(), GetPixelSize(m_squadRect));
		m_isReadyShown = false;
		m_readyBanner.DOKill();
		m_readyBanner.alpha = 0f;
		for (int i = 0; i < m_unitLabels.Length; i++)
		{
			m_labelStates[i] = int.MinValue;
			if (i < m_squadStage.UnitCount)
				m_unitLabels[i].name.text = m_squadStage.GetUnitName(i);
		}

		ApplyProgress();
		RefreshUnitLabels();

		PanelTransition transition = UIManager.Instance.PanelTransition;
		if (transition != null && transition.TryPlayCoverSweep(() => _onCovered?.Invoke()))
			return;

		m_briefingGroup.alpha = 0f;
		m_briefingGroup.DOFade(1f, m_fadeInDuration).SetUpdate(true).OnComplete(() => _onCovered?.Invoke());
	}

	public void SetProgress ( float _progress )
	{
		m_loadProgress = Mathf.Max(m_loadProgress, Mathf.Clamp01(_progress));
	}

	public void Hide ( Action _onHidden )
	{
		UIManager.Instance.StartCoroutine(PlayArrival(_onHidden));
	}

	#endregion

	#region Arrival

	private IEnumerator PlayArrival ( Action _onHidden )
	{
		SetProgress(1f);
		while (m_displayedProgress < 1f || !m_squadStage.IsReady || Time.unscaledTime - m_squadStage.ReadyTime < m_readyBeatDuration)
			yield return null;

		m_isStartRequested = false;
		m_startButton.SetVisible(true, true);
		m_startButton.SetInteractability(true);
		while (!m_isStartRequested)
			yield return null;

		m_startButton.SetInteractability(false);

		Camera camera = CameraManager.Instance.Camera;
		Vector3 gameplayPosition = camera.transform.position;
		Quaternion gameplayRotation = camera.transform.rotation;
		bool hasDescent = CameraManager.Instance.TryGetGridOverviewPosition(gameplayRotation * Vector3.forward, m_macroFramingMargin, out Vector3 macroPosition);
		if (hasDescent)
			CameraManager.Instance.PlaceCameraAt(macroPosition, gameplayRotation);

		List<Entity> squad = GetPlayerSquad();
		Material hologram = GhostEntity.DefaultHologram;
		foreach (Entity entity in squad)
			entity.Skin.SetForcedLook(hologram);

		m_briefingGroup.blocksRaycasts = false;
		m_briefingGroup.DOFade(0f, m_briefingFadeOutDuration).SetUpdate(true).OnComplete(HideSquad);
		m_squadRect.DOScale(m_squadZoomOnFadeOut, m_briefingFadeOutDuration).SetUpdate(true);
		yield return Wait(m_descentDelay);

		float descentDuration = hasDescent ? m_descentDuration : 0f;
		if (hasDescent)
			CameraManager.Instance.MoveCameraAlong(new[] { macroPosition, gameplayPosition }, gameplayRotation, CameraManager.Instance.DefaultFieldOfView, descentDuration);

		float materializeTime = descentDuration * m_materializeAt;
		yield return Wait(materializeTime);
		foreach (Entity entity in squad)
		{
			if (entity != null)
				entity.Skin.SetForcedLook(null);
			yield return Wait(m_materializeStagger);
		}

		yield return Wait(descentDuration - materializeTime - m_materializeStagger * squad.Count);

		m_isShown = false;
		m_briefingGroup.gameObject.SetActive(false);
		m_squadRect.localScale = Vector3.one;
		_onHidden?.Invoke();

		m_bannerGroup.gameObject.SetActive(true);
		yield return m_bannerGroup.DOFade(1f, m_bannerFadeDuration).SetUpdate(true).WaitForCompletion();
		yield return Wait(m_bannerHoldDuration);
		yield return m_bannerGroup.DOFade(0f, m_bannerFadeDuration).SetUpdate(true).WaitForCompletion();
		m_bannerGroup.gameObject.SetActive(false);
	}

	private void HideSquad ()
	{
		m_squadStage.Hide();
		m_squadView.texture = null;
	}

	public static List<Entity> GetPlayerSquad ()
	{
		List<Entity> squad = new();
		GameManager gameManager = GameManager.Instance;
		if (gameManager == null || gameManager.PlayersEntityAnchor == null || gameManager.PlayerID >= gameManager.PlayersEntityAnchor.Length)
			return squad;

		foreach (Entity entity in gameManager.PlayersEntityAnchor[gameManager.PlayerID].Entities)
		{
			if (entity != null && entity.Skin != null)
				squad.Add(entity);
		}

		return squad;
	}

	private static IEnumerator Wait ( float _duration )
	{
		for (float elapsed = 0f; elapsed < _duration; elapsed += Time.unscaledDeltaTime)
			yield return null;
	}

	#endregion

	private static Vector2Int GetPixelSize ( RectTransform _rect )
	{
		Rect rect = _rect.GetWorldRect();
		return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(rect.width), 16, 4096), Mathf.Clamp(Mathf.RoundToInt(rect.height), 16, 4096));
	}

	private void ApplyProgress ()
	{
		m_squadStage.SetProgress(m_displayedProgress);
		m_progressFill.anchorMax = new Vector2(m_displayedProgress, 1f);

		int percent = Mathf.RoundToInt(m_displayedProgress * 100f);
		if (percent == m_shownPercent)
			return;

		m_shownPercent = percent;
		m_loadingTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.briefing_loading), percent);
	}
}
