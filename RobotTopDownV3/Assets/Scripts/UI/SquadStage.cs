using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(32000)]
public class SquadStage : MonoBehaviour
{
	private const int k_maxUnits = 8;

	private static readonly int s_hologramTextureID = Shader.PropertyToID("_SquadStageHologram");
	private static readonly int s_decorTextureID = Shader.PropertyToID("_SquadStageDecor");
	private static readonly int s_slotsID = Shader.PropertyToID("_SquadStageSlots");
	private static readonly int s_revealID = Shader.PropertyToID("_SquadStageReveal");
	private static readonly int s_unitsID = Shader.PropertyToID("_SquadStageUnits");
	private static readonly int s_floorID = Shader.PropertyToID("_SquadStageFloor");
	private static readonly int s_intensityID = Shader.PropertyToID("_Intensity");

	private sealed class Unit
	{
		public readonly Entity entity;
		public readonly string name;
		public readonly MeshRenderer beam;
		private readonly Bounds m_localBounds;
		private readonly Renderer[] m_renderers;
		private readonly Material[][] m_realMaterials;
		private readonly Material[][] m_hologramMaterials;
		public float reveal;
		public float doneTime = -1f;

		public Bounds WorldBounds => new(entity.transform.position + m_localBounds.center, m_localBounds.size);
		public float Height => m_localBounds.max.y;

		public Unit ( Entity _entity, string _name, MeshRenderer _beam, Bounds _poseBounds, List<Renderer> _renderers, Material _hologram )
		{
			entity = _entity;
			name = _name;
			beam = _beam;
			m_localBounds = new Bounds(_poseBounds.center - _entity.transform.position, _poseBounds.size);
			m_renderers = _renderers.ToArray();
			m_realMaterials = new Material[m_renderers.Length][];
			m_hologramMaterials = new Material[m_renderers.Length][];
			for (int i = 0; i < m_renderers.Length; i++)
			{
				Renderer renderer = m_renderers[i];
				renderer.lightProbeUsage = LightProbeUsage.Off;
				m_realMaterials[i] = renderer.sharedMaterials;
				m_hologramMaterials[i] = _hologram != null ? Filled(_hologram, m_realMaterials[i].Length) : m_realMaterials[i];
			}
		}

		public void SetHologram ( bool _isHologram )
		{
			for (int i = 0; i < m_renderers.Length; i++)
			{
				if (m_renderers[i] != null)
					m_renderers[i].sharedMaterials = _isHologram ? m_hologramMaterials[i] : m_realMaterials[i];
			}
		}

		public void SetVisible ( bool _isVisible )
		{
			foreach (Renderer renderer in m_renderers)
			{
				if (renderer != null)
					renderer.forceRenderingOff = !_isVisible;
			}

			beam.forceRenderingOff = !_isVisible;
		}

		private static Material[] Filled ( Material _material, int _count )
		{
			Material[] materials = new Material[Mathf.Max(1, _count)];
			Array.Fill(materials, _material);
			return materials;
		}
	}

	[SerializeField] private Camera m_camera;
	[SerializeField] private Light[] m_lights;
	[SerializeField] private Transform m_unitsRoot;
	[SerializeField] private Renderer m_floor;
	[SerializeField] private MeshRenderer m_beamTemplate;
	[SerializeField] private Vector3 m_position = new(0f, -2000f, 0f);
	[SerializeField] private Color m_ambientColor = new(.38f, .42f, .5f);
	[SerializeField] private float m_unitSpacing = 2.2f;
	[SerializeField] private float m_unitYaw = 160f;
	[SerializeField] private float m_poseMargin = .25f;
	[SerializeField] private float m_padRadius = .72f;
	[SerializeField] private Vector2 m_floorMargin = new(2.5f, 3.2f);
	[SerializeField] private float m_cameraPitch = 8f;
	[SerializeField] private float m_framingMargin = 1.06f;
	[SerializeField, Range(0f, .4f)] private float m_labelBand = .12f;
	[SerializeField, Range(0f, .4f)] private float m_bannerBand = .12f;
	[SerializeField] private float m_lineMargin = .06f;
	[SerializeField, Range(0f, .9f)] private float m_revealOverlap = .4f;
	[SerializeField] private float m_unitFlashDuration = .6f;
	[SerializeField] private float m_readyFlashDuration = .9f;
	[SerializeField] private int m_msaaSamples = 4;

	private readonly List<Unit> m_units = new();
	private readonly List<Renderer> m_rendererBuffer = new();
	private readonly List<Light> m_sceneLights = new();
	private readonly List<Light> m_switchedOffLights = new();
	private readonly Vector4[] m_slots = new Vector4[k_maxUnits];
	private readonly Vector4[] m_floorUnits = new Vector4[k_maxUnits];
	private readonly float[] m_reveals = new float[k_maxUnits];
	private readonly float[] m_centers = new float[k_maxUnits];
	private MaterialPropertyBlock m_beamProperties;
	private RenderTexture m_realTexture;
	private RenderTexture m_hologramTexture;
	private RenderTexture m_decorTexture;
	private SphericalHarmonicsL2 m_ambientProbe;
	private float m_readyTime = -1f;

	public int UnitCount => m_units.Count;
	public bool IsReady => m_readyTime >= 0f;
	public float ReadyTime => m_readyTime;

	private void Awake ()
	{
		transform.SetParent(null, false);
		transform.SetPositionAndRotation(m_position, Quaternion.identity);
		DontDestroyOnLoad(gameObject);
		m_camera.enabled = false;
		m_floor.forceRenderingOff = true;
		m_beamTemplate.gameObject.SetActive(false);
		SetLightsEnabled(false);
	}

	private void OnDestroy ()
	{
		Hide();
	}

	private void LateUpdate ()
	{
		if (m_realTexture == null)
			return;

		UpdateStage();
		Render();
	}

	public Texture Show ( IReadOnlyList<EntitySavedData> _squad, Vector2Int _pixelSize )
	{
		Hide();

		Material hologram = GhostEntity.DefaultHologram;
		foreach (EntitySavedData data in _squad)
		{
			if (data != null && m_units.Count < k_maxUnits)
				m_units.Add(Spawn(data, hologram));
		}

		m_realTexture = CreateTexture(_pixelSize, m_msaaSamples, "SquadStageReal");
		m_hologramTexture = CreateTexture(_pixelSize, 1, "SquadStageHologram");
		m_decorTexture = CreateTexture(_pixelSize, 1, "SquadStageDecor");
		m_camera.aspect = (float)_pixelSize.x / _pixelSize.y;
		Shader.SetGlobalTexture(s_hologramTextureID, m_hologramTexture);
		Shader.SetGlobalTexture(s_decorTextureID, m_decorTexture);

		Arrange();
		m_readyTime = -1f;
		SetProgress(0f);

		m_ambientProbe = AmbientOverride.CreateFlatProbe(m_ambientColor);
		RefreshSceneLights();
		SceneManager.sceneLoaded += OnSceneLoaded;

		UpdateStage();
		Render();
		return m_realTexture;
	}

	public void SetProgress ( float _progress )
	{
		int count = m_units.Count;
		float duration = count > 0 ? 1f / (count - (count - 1) * m_revealOverlap) : 1f;
		bool isEveryUnitDone = true;
		for (int i = 0; i < count; i++)
		{
			Unit unit = m_units[i];
			float raw = _progress >= 1f ? 1f : Mathf.Clamp01((_progress - i * duration * (1f - m_revealOverlap)) / duration);
			unit.reveal = raw * raw * (3f - 2f * raw);
			if (raw >= 1f && unit.doneTime < 0f)
				unit.doneTime = Time.unscaledTime;
			isEveryUnitDone &= raw >= 1f;
		}

		if (_progress >= 1f && isEveryUnitDone && m_readyTime < 0f)
			m_readyTime = Time.unscaledTime;

		foreach (Unit unit in m_units)
			unit.entity.Skin.SetPresentation(!IsReady, 1f - unit.reveal);
	}

	public string GetUnitName ( int _index )
	{
		return m_units[_index].name;
	}

	public float GetReveal ( int _index )
	{
		return m_units[_index].reveal;
	}

	public Vector2 GetLabelAnchor ( int _index )
	{
		Vector3 position = m_units[_index].entity.transform.position;
		return m_camera.WorldToViewportPoint(position + new Vector3(0f, 0f, -m_padRadius - .1f));
	}

	public void Hide ()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;

		foreach (Unit unit in m_units)
		{
			if (unit.entity != null)
				Destroy(unit.entity.gameObject);
			if (unit.beam != null)
				Destroy(unit.beam.gameObject);
		}

		m_units.Clear();
		m_sceneLights.Clear();
		m_readyTime = -1f;
		if (m_camera != null)
			m_camera.targetTexture = null;
		ReleaseTexture(ref m_realTexture);
		ReleaseTexture(ref m_hologramTexture);
		ReleaseTexture(ref m_decorTexture);
	}

	private Unit Spawn ( EntitySavedData _data, Material _hologram )
	{
		Entity entity = Instantiate(_data.FrameData != null ? _data.FrameData.prefab : GameAssets.current.game.defaultEntity, m_unitsRoot);
		entity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, m_unitYaw, 0f));
		entity.InitHangarMode(_data);
		entity.UI.HideUI();
		Bounds poseBounds = entity.Skin.GetPoseBounds(m_poseMargin);
		entity.Skin.SetPresentation(true, 1f);

		MeshRenderer beam = Instantiate(m_beamTemplate, m_unitsRoot);
		beam.gameObject.SetActive(true);

		m_rendererBuffer.Clear();
		entity.Skin.CollectSkinRenderers(m_rendererBuffer);
		return new Unit(entity, _data.name, beam, poseBounds, m_rendererBuffer, _hologram);
	}

	private void Arrange ()
	{
		float firstX = -m_unitSpacing * (m_units.Count - 1) * .5f;
		for (int i = 0; i < m_units.Count; i++)
		{
			Vector3 position = new(firstX + m_unitSpacing * i, 0f, 0f);
			m_units[i].entity.transform.localPosition = position;
			m_units[i].beam.transform.localPosition = position;
		}

		float halfRow = m_units.Count > 0 ? -firstX + m_padRadius : m_padRadius;
		Vector3 center = m_unitsRoot.position;
		Shader.SetGlobalVector(s_floorID, new Vector4(center.x, center.z, halfRow + m_floorMargin.x, m_floorMargin.y));

		if (m_units.Count == 0)
			return;

		Bounds bounds = m_units[0].WorldBounds;
		foreach (Unit unit in m_units)
		{
			Vector3 feet = unit.entity.transform.position;
			bounds.Encapsulate(unit.WorldBounds);
			bounds.Encapsulate(feet + new Vector3(-m_padRadius, 0f, -m_padRadius));
			bounds.Encapsulate(feet + new Vector3(m_padRadius, 0f, m_padRadius));
		}

		Quaternion rotation = Quaternion.Euler(m_cameraPitch, 0f, 0f);
		float tangent = Mathf.Tan(m_camera.fieldOfView * .5f * Mathf.Deg2Rad);
		float window = Mathf.Max(.2f, 1f - m_labelBand - m_bannerBand);
		float distance = Mathf.Max(bounds.extents.y / (tangent * window), bounds.extents.x / (tangent * m_camera.aspect)) * m_framingMargin + bounds.extents.z;
		Vector3 offset = new(0f, (m_bannerBand - m_labelBand) * distance * tangent, -distance);
		m_camera.transform.SetPositionAndRotation(bounds.center + rotation * offset, rotation);
	}

	private void UpdateStage ()
	{
		float now = Time.unscaledTime;
		float readyFlash = IsReady ? Mathf.Clamp01(1f - (now - m_readyTime) / m_readyFlashDuration) : 0f;
		m_beamProperties ??= new MaterialPropertyBlock();

		for (int i = 0; i < m_units.Count; i++)
		{
			Unit unit = m_units[i];
			Vector3 feet = unit.entity.transform.position;
			float height = unit.Height;
			float line = Mathf.Lerp(-m_lineMargin * height, height * (1f + m_lineMargin), unit.reveal);
			Bounds bounds = unit.WorldBounds;
			Vector3 middle = new(bounds.center.x, feet.y + height * .5f, bounds.center.z);
			float bottom = m_camera.WorldToViewportPoint(feet).y;
			float top = m_camera.WorldToViewportPoint(feet + Vector3.up * height).y;
			m_centers[i] = m_camera.WorldToViewportPoint(middle).x;
			m_slots[i] = new Vector4(0f, m_centers[i], m_camera.WorldToViewportPoint(feet + Vector3.up * line).y, top - bottom);
			m_reveals[i] = unit.reveal;

			float activity = Mathf.Clamp01(unit.reveal * 12f) * Mathf.Clamp01((1f - unit.reveal) * 12f);
			unit.beam.transform.localScale = new Vector3(m_padRadius * 2f, Mathf.Max(line, .001f) * .5f, m_padRadius * 2f);
			unit.beam.transform.position = feet + Vector3.up * (Mathf.Max(line, .001f) * .5f);
			m_beamProperties.SetFloat(s_intensityID, activity);
			unit.beam.SetPropertyBlock(m_beamProperties);
			unit.beam.enabled = activity > 0f;

			float ownFlash = unit.doneTime >= 0f ? Mathf.Clamp01(1f - (now - unit.doneTime) / m_unitFlashDuration) : 0f;
			m_floorUnits[i] = new Vector4(feet.x, feet.z, 1f, Mathf.Max(ownFlash, readyFlash));
		}

		for (int i = 0; i < k_maxUnits; i++)
		{
			if (i < m_units.Count)
			{
				m_slots[i].x = i == 0 ? -1f : (m_centers[i - 1] + m_centers[i]) * .5f;
				continue;
			}

			m_slots[i] = new Vector4(2f, 2f, 0f, 0f);
			m_reveals[i] = 0f;
			m_floorUnits[i] = Vector4.zero;
		}

		Shader.SetGlobalVectorArray(s_slotsID, m_slots);
		Shader.SetGlobalFloatArray(s_revealID, m_reveals);
		Shader.SetGlobalVectorArray(s_unitsID, m_floorUnits);
	}

	private void Render ()
	{
		foreach (Unit unit in m_units)
			unit.SetVisible(false);
		m_floor.forceRenderingOff = false;
		m_camera.targetTexture = m_decorTexture;
		m_camera.Render();
		m_floor.forceRenderingOff = true;
		foreach (Unit unit in m_units)
			unit.SetVisible(true);

		foreach (Light light in m_sceneLights)
		{
			if (light != null && light.enabled)
			{
				light.enabled = false;
				m_switchedOffLights.Add(light);
			}
		}

		SetLightsEnabled(true);
		SphericalHarmonicsL2 sceneAmbient = RenderSettings.ambientProbe;
		RenderSettings.ambientProbe = m_ambientProbe;

		RenderLook(m_realTexture, false);
		RenderLook(m_hologramTexture, true);

		RenderSettings.ambientProbe = sceneAmbient;
		SetLightsEnabled(false);
		foreach (Light light in m_switchedOffLights)
		{
			if (light != null)
				light.enabled = true;
		}

		m_switchedOffLights.Clear();
	}

	private void RenderLook ( RenderTexture _target, bool _isHologram )
	{
		foreach (Unit unit in m_units)
			unit.SetHologram(_isHologram);

		m_camera.targetTexture = _target;
		m_camera.Render();
	}

	private void SetLightsEnabled ( bool _isEnabled )
	{
		foreach (Light light in m_lights)
			light.enabled = _isEnabled;
	}

	private void RefreshSceneLights ()
	{
		m_sceneLights.Clear();
		foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
		{
			if (!light.transform.IsChildOf(transform))
				m_sceneLights.Add(light);
		}
	}

	private void OnSceneLoaded ( Scene _scene, LoadSceneMode _mode )
	{
		RefreshSceneLights();
	}

	private static RenderTexture CreateTexture ( Vector2Int _size, int _msaaSamples, string _name )
	{
		RenderTextureDescriptor descriptor = new(_size.x, _size.y, RenderTextureFormat.ARGB32, 24)
		{
			sRGB = true,
			msaaSamples = Mathf.Clamp(Mathf.ClosestPowerOfTwo(_msaaSamples), 1, 8)
		};

		RenderTexture texture = RenderTexture.GetTemporary(descriptor);
		texture.name = _name;
		return texture;
	}

	private static void ReleaseTexture ( ref RenderTexture _texture )
	{
		if (_texture != null)
			RenderTexture.ReleaseTemporary(_texture);
		_texture = null;
	}
}
