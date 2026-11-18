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
	private const byte k_floorCode = 1;
	private const byte k_wallCode = 2;
	private const byte k_coverCode = 3;
	private const byte k_objectiveCode = 4;
	private const byte k_squadSpawnCode = 5;

	private static readonly int s_mapGridID = Shader.PropertyToID("_MapGrid");
	private static readonly int s_mapBoundsID = Shader.PropertyToID("_MapBounds");
	private static readonly int s_revealCenterID = Shader.PropertyToID("_RevealCenter");
	private static readonly int s_revealID = Shader.PropertyToID("_Reveal");

	[Title("Briefing")]
	[SerializeField] private float m_minimumDuration = 2.8f;
	[SerializeField] private float m_fadeInDuration = .3f;

	[Title("Arrival")]
	[SerializeField] private float m_briefingFadeOutDuration = .7f;
	[SerializeField] private float m_mapZoomOnFadeOut = 1.12f;
	[SerializeField] private float m_descentDelay = .2f;
	[SerializeField] private float m_descentDuration = 2.2f;
	[SerializeField] private float m_macroFramingMargin = 1.1f;
	[SerializeField, Range(0f, 1f)] private float m_materializeAt = .8f;
	[SerializeField] private float m_materializeStagger = .08f;
	[SerializeField] private float m_bannerFadeDuration = .35f;
	[SerializeField] private float m_bannerHoldDuration = 1.8f;

	[Title("References")]
	[SerializeField] private CanvasGroup m_briefingGroup;
	[SerializeField] private RectTransform m_mapRect;
	[SerializeField] private RawImage m_map;
	[SerializeField] private AspectRatioFitter m_mapFitter;
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

	private Material m_mapMaterial;
	private Texture2D m_mapData;
	private bool m_isShown;
	private bool m_isStartRequested;
	private float m_elapsed;
	private float m_loadProgress;
	private float m_displayedProgress;

	private void Awake ()
	{
		if (m_map != null && m_map.material != null)
		{
			m_mapMaterial = new Material(m_map.material);
			m_map.material = m_mapMaterial;
		}

		m_startButton.onClick += OnClickStart;
	}

	private void OnDestroy ()
	{
		if (m_startButton != null)
			m_startButton.onClick -= OnClickStart;
		if (m_mapMaterial != null)
			Destroy(m_mapMaterial);
		if (m_mapData != null)
			Destroy(m_mapData);
	}

	private void Update ()
	{
		if (!m_isShown)
			return;

		m_elapsed += Time.unscaledDeltaTime;
		float target = Mathf.Min(m_minimumDuration > 0f ? m_elapsed / m_minimumDuration : 1f, m_loadProgress);
		m_displayedProgress = Mathf.Max(m_displayedProgress, Mathf.Clamp01(target));
		ApplyProgress();
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
		BuildMap(_mission.map);
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
		ApplyProgress();

		m_mapRect.localScale = Vector3.one;
		m_startButton.SetVisible(false, true);
		m_bannerGroup.alpha = 0f;
		m_bannerGroup.gameObject.SetActive(false);
		m_briefingGroup.blocksRaycasts = true;
		m_briefingGroup.alpha = 1f;
		m_briefingGroup.gameObject.SetActive(true);

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
		StartCoroutine(PlayArrival(_onHidden));
	}

	#endregion

	#region Arrival

	private IEnumerator PlayArrival ( Action _onHidden )
	{
		SetProgress(1f);
		while (m_displayedProgress < 1f)
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
		Material hologram = GameAssets.current.game.baseGhost != null ? GameAssets.current.game.baseGhost.HologramMaterial : null;
		foreach (Entity entity in squad)
			entity.Skin.SetForcedLook(hologram);

		m_briefingGroup.blocksRaycasts = false;
		m_briefingGroup.DOFade(0f, m_briefingFadeOutDuration).SetUpdate(true);
		m_mapRect.DOScale(m_mapZoomOnFadeOut, m_briefingFadeOutDuration).SetUpdate(true);
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
		m_mapRect.localScale = Vector3.one;
		_onHidden?.Invoke();

		m_bannerGroup.gameObject.SetActive(true);
		yield return m_bannerGroup.DOFade(1f, m_bannerFadeDuration).SetUpdate(true).WaitForCompletion();
		yield return Wait(m_bannerHoldDuration);
		yield return m_bannerGroup.DOFade(0f, m_bannerFadeDuration).SetUpdate(true).WaitForCompletion();
		m_bannerGroup.gameObject.SetActive(false);
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

	#region Map

	private void BuildMap ( GridData _grid )
	{
		bool hasMap = _grid != null && _grid.width > 0 && _grid.height > 0 && _grid.tiles != null && m_mapMaterial != null;
		m_map.gameObject.SetActive(hasMap);
		if (!hasMap)
			return;

		int width = _grid.width;
		int height = _grid.height;
		if (m_mapData == null || m_mapData.width != width || m_mapData.height != height)
		{
			if (m_mapData != null)
				Destroy(m_mapData);

			m_mapData = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
			{
				name = "BriefingMapData",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			};
		}

		byte[] bytes = new byte[width * height * 4];
		for (int i = 0; i < width * height && i < _grid.tiles.Length; i++)
			bytes[i * 4] = GetTileCode(_grid.tiles[i]);

		m_mapData.SetPixelData(bytes, 0);
		m_mapData.Apply(false);
		m_map.texture = m_mapData;

		float innerRadius = Tile.innerRadius;
		float outerRadius = Tile.outerRadius;
		float minX = -innerRadius;
		float maxX = (width - .5f) * innerRadius * 2f + innerRadius;
		float minZ = -outerRadius;
		float maxZ = (height - 1) * outerRadius * 1.5f + outerRadius;
		Vector2 center = new((minX + maxX) * .5f, (minZ + maxZ) * .5f);

		m_mapMaterial.SetVector(s_mapGridID, new Vector4(width, height, innerRadius, outerRadius));
		m_mapMaterial.SetVector(s_mapBoundsID, new Vector4(minX, minZ, maxX, maxZ));
		m_mapMaterial.SetVector(s_revealCenterID, new Vector4(center.x, center.y, new Vector2(maxX - center.x, maxZ - center.y).magnitude, 0f));
		m_mapFitter.aspectRatio = (maxX - minX) / Mathf.Max(maxZ - minZ, .001f);
	}

	private static byte GetTileCode ( GridData.TileData _tile )
	{
		if (_tile == null)
			return k_floorCode;

		return _tile.groundType switch
		{
			TileGroundType.Void => 0,
			TileGroundType.Wall or TileGroundType.PlayerStructure => k_wallCode,
			TileGroundType.Cover => k_coverCode,
			TileGroundType.Zone or TileGroundType.ControlPoint or TileGroundType.PlayerCommPoint or TileGroundType.EnemyStructure => k_objectiveCode,
			TileGroundType.StaticPlayerSpawn or TileGroundType.DynamicPlayerSpawn => k_squadSpawnCode,
			_ => k_floorCode,
		};
	}

	private void ApplyProgress ()
	{
		if (m_mapMaterial != null)
			m_mapMaterial.SetFloat(s_revealID, m_displayedProgress);
		m_progressFill.anchorMax = new Vector2(m_displayedProgress, 1f);
		m_loadingTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.briefing_loading), Mathf.RoundToInt(m_displayedProgress * 100f));
	}

	#endregion
}
