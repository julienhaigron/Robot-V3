using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Sirenix.OdinInspector;
using System.Linq;

public class HubManager : Singleton<HubManager>
{
	public enum Building
	{
		Overview,
		Hangar,
		Recycler,
		RepairStation,
		PsyShop,
		CommandoShop,
		PaladinShop,
		Arena,
	}

	[Serializable]
	public class BuildingView
	{
		public Building building;
		public string meshNamePrefix;
		public Transform cameraPoint;
	}

	[Title("Depedencies")]
	[SerializeField] private GameObject m_squadEntitiesParent;
	[SerializeField] private Transform m_hangarCameraPosition;

	[Title("Buildings camera")]
	[SerializeField] private BuildingView[] m_buildingViews = GetDefaultBuildingViews();
	[SerializeField] private float m_buildingFramingMargin = 1.15f;
	[SerializeField] private float m_cameraMoveDuration = .7f;
	[SerializeField] private float m_cameraMaxMoveDuration = 1.8f;
	[SerializeField] private float m_cameraSpeed = 3f;

	[Title("Camera path")]
	[SerializeField] private string m_moonMeshName = "Moon";
	[SerializeField] private string m_entranceMeshNamePrefix = "Entry-Ring";
	[SerializeField] private float m_entranceOuterOffset = .45f;
	[SerializeField] private float m_entranceInnerOffset = .3f;
	[SerializeField] private float m_moonAvoidanceFactor = 1.6f;
	[SerializeField] private float m_interiorAxisSpan = .6f;

	[Title("Hangar")]
	[SerializeField] private string m_hangarSceneName = "HangarScene";

	[Title("Parameters")]
	[SerializeField] private float m_unitSpacing = 1.5f;

	private SerializableDictionary<EntitySavedData, Entity> m_squadEntities = new();
	private bool m_isHangarShown;
	private bool m_isLoadingHangar;
	private HangarEnvironment m_hangar;
	private readonly List<GameObject> m_hiddenHubRoots = new();
	private readonly Dictionary<string, Bounds> m_meshBounds = new();

	private static BuildingView[] GetDefaultBuildingViews ()
	{
		return new BuildingView[]
		{
			new() { building = Building.Hangar, meshNamePrefix = "Hangar" },
			new() { building = Building.Recycler, meshNamePrefix = "Recycle" },
			new() { building = Building.RepairStation, meshNamePrefix = "Repar-Station" },
			new() { building = Building.PsyShop, meshNamePrefix = "Psy-QG" },
			new() { building = Building.CommandoShop, meshNamePrefix = "Commando-QG" },
			new() { building = Building.PaladinShop, meshNamePrefix = "Paladin-QG" },
			new() { building = Building.Arena, meshNamePrefix = "Arene" },
		};
	}

	public override void Awake ()
	{
		base.Awake();
		UIManager.onFocusedWindowChanged += OnFocusedWindowChanged;
		ShopPanel.onShopInitialized += OnShopInitialized;
	}

	private void OnDestroy ()
	{
		UIManager.onFocusedWindowChanged -= OnFocusedWindowChanged;
		ShopPanel.onShopInitialized -= OnShopInitialized;
	}

	#region Buildings camera

	private void OnFocusedWindowChanged ()
	{
		Building? building = GetBuildingOf(UIManager.Instance.currentPanel);
		if (building.HasValue)
			FocusBuilding(building.Value);
	}

	private void OnShopInitialized ( ShopPanel _shop )
	{
		if (UIManager.Instance.currentPanel == _shop)
			FocusBuilding(GetShopBuilding(_shop.CurrentFaction));
	}

	private static Building? GetBuildingOf ( AUIPanel _panel )
	{
		return _panel switch
		{
			SoloHubPanel => Building.Overview,
			RecyclePanel => Building.Recycler,
			RepairStationPanel => Building.RepairStation,
			SelectMissionPanel => Building.Arena,
			ShopPanel shop => GetShopBuilding(shop.CurrentFaction),
			_ => null,
		};
	}

	private static Building GetShopBuilding ( EntityEquipmentData.EntityFaction _faction )
	{
		return _faction switch
		{
			EntityEquipmentData.EntityFaction.Psy => Building.PsyShop,
			EntityEquipmentData.EntityFaction.Commando => Building.CommandoShop,
			EntityEquipmentData.EntityFaction.Paladin => Building.PaladinShop,
			_ => Building.Overview,
		};
	}

	public void FocusBuilding ( Building _building )
	{
		if (m_isHangarShown)
			return;

		PlaceCameraOn(_building, m_cameraMoveDuration);
	}

	private void PlaceCameraOn ( Building _building, float _minDuration )
	{
		if ((_building == Building.Overview || !TryGetBuildingCameraPose(_building, out Vector3 position, out Quaternion rotation))
			&& !CameraManager.Instance.TryGetHubCameraPose(out position, out rotation))
			return;

		if (_minDuration <= 0f)
		{
			CameraManager.Instance.PlaceCameraAt(position, rotation);
			return;
		}

		List<Vector3> path = BuildCameraPath(CameraManager.Instance.Camera.transform.position, position);
		float length = 0f;
		for (int i = 1; i < path.Count; i++)
			length += Vector3.Distance(path[i - 1], path[i]);

		float duration = m_cameraSpeed > 0f ? Mathf.Clamp(length / m_cameraSpeed, _minDuration, Mathf.Max(_minDuration, m_cameraMaxMoveDuration)) : _minDuration;
		CameraManager.Instance.MoveCameraAlong(path, rotation, duration);
	}

	private List<Vector3> BuildCameraPath ( Vector3 _start, Vector3 _end )
	{
		List<Vector3> waypoints = new() { _start };
		if (TryGetMeshBounds(m_moonMeshName, out Bounds moonBounds) && TryGetMeshBounds(m_entranceMeshNamePrefix, out Bounds entranceBounds))
		{
			MoonShape moon = new(moonBounds, entranceBounds, m_entranceOuterOffset, m_entranceInnerOffset);
			bool isStartInside = moon.IsInside(_start), isEndInside = moon.IsInside(_end);

			if (isStartInside && isEndInside)
				AddInteriorWaypoints(waypoints, moon, _start, _end);
			else if (!isStartInside && !isEndInside)
				AddAvoidanceWaypoints(waypoints, moon, _start, _end, 0);
			else if (isStartInside)
			{
				AddInteriorWaypoints(waypoints, moon, _start, moon.innerEntrance);
				AddWaypoint(waypoints, moon.innerEntrance);
				AddWaypoint(waypoints, moon.outerEntrance);
				AddAvoidanceWaypoints(waypoints, moon, moon.outerEntrance, _end, 0);
			}
			else
			{
				AddAvoidanceWaypoints(waypoints, moon, _start, moon.outerEntrance, 0);
				AddWaypoint(waypoints, moon.outerEntrance);
				AddWaypoint(waypoints, moon.innerEntrance);
				AddInteriorWaypoints(waypoints, moon, moon.innerEntrance, _end);
			}
		}

		AddWaypoint(waypoints, _end);
		return SampleCatmullRom(waypoints, 12);
	}

	private readonly struct MoonShape
	{
		public readonly Vector3 center;
		public readonly float radius;
		public readonly Vector3 axis;
		public readonly Vector3 outerEntrance;
		public readonly Vector3 innerEntrance;

		public MoonShape ( Bounds _moon, Bounds _entrance, float _outerOffset, float _innerOffset )
		{
			center = _moon.center;
			radius = Mathf.Max(_moon.extents.x, Mathf.Max(_moon.extents.y, _moon.extents.z));
			Vector3 toEntrance = _entrance.center - center;
			axis = toEntrance.sqrMagnitude > 1e-8f ? toEntrance.normalized : Vector3.forward;
			outerEntrance = _entrance.center + axis * _outerOffset;
			innerEntrance = _entrance.center - axis * _innerOffset;
		}

		public bool IsInside ( Vector3 _point ) => (_point - center).sqrMagnitude < radius * radius;
		public float Axial ( Vector3 _point ) => Vector3.Dot(_point - center, axis);
		public Vector3 OnAxis ( Vector3 _point ) => center + axis * Axial(_point);
	}

	private void AddInteriorWaypoints ( List<Vector3> _waypoints, MoonShape _moon, Vector3 _from, Vector3 _to )
	{
		if (Mathf.Abs(_moon.Axial(_from) - _moon.Axial(_to)) <= m_interiorAxisSpan)
			return;

		AddWaypoint(_waypoints, _moon.OnAxis(_from));
		AddWaypoint(_waypoints, _moon.OnAxis(_to));
	}

	private void AddAvoidanceWaypoints ( List<Vector3> _waypoints, MoonShape _moon, Vector3 _from, Vector3 _to, int _depth )
	{
		Vector3 segment = _to - _from;
		float progress = segment.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(_moon.center - _from, segment) / segment.sqrMagnitude) : 0f;
		Vector3 offset = _from + segment * progress - _moon.center;
		if (offset.magnitude >= _moon.radius * 1.08f || _depth > 2)
			return;

		if (offset.sqrMagnitude < 1e-8f)
			offset = Mathf.Abs(segment.normalized.y) < .9f ? new Vector3(segment.z, 0f, -segment.x) : Vector3.right;

		Vector3 around = _moon.center + offset.normalized * (_moon.radius * m_moonAvoidanceFactor);
		AddAvoidanceWaypoints(_waypoints, _moon, _from, around, _depth + 1);
		AddWaypoint(_waypoints, around);
		AddAvoidanceWaypoints(_waypoints, _moon, around, _to, _depth + 1);
	}

	private static void AddWaypoint ( List<Vector3> _waypoints, Vector3 _point )
	{
		if (_waypoints.Count == 0 || Vector3.Distance(_waypoints[^1], _point) > .01f)
			_waypoints.Add(_point);
	}

	private static List<Vector3> SampleCatmullRom ( List<Vector3> _points, int _samplesPerSegment )
	{
		if (_points.Count < 3)
			return _points;

		List<Vector3> samples = new();
		List<Vector3> padded = new(_points.Count + 2) { 2f * _points[0] - _points[1] };
		padded.AddRange(_points);
		padded.Add(2f * _points[^1] - _points[^2]);

		for (int i = 1; i < padded.Count - 2; i++)
		{
			Vector3 p0 = padded[i - 1], p1 = padded[i], p2 = padded[i + 1], p3 = padded[i + 2];
			float t0 = 0f;
			float t1 = t0 + Mathf.Sqrt(Mathf.Max(Vector3.Distance(p0, p1), 1e-5f));
			float t2 = t1 + Mathf.Sqrt(Mathf.Max(Vector3.Distance(p1, p2), 1e-5f));
			float t3 = t2 + Mathf.Sqrt(Mathf.Max(Vector3.Distance(p2, p3), 1e-5f));

			for (int sample = 0; sample < _samplesPerSegment; sample++)
			{
				float t = Mathf.Lerp(t1, t2, sample / (float)_samplesPerSegment);
				Vector3 a1 = Remap(p0, p1, t0, t1, t), a2 = Remap(p1, p2, t1, t2, t), a3 = Remap(p2, p3, t2, t3, t);
				Vector3 b1 = Remap(a1, a2, t0, t2, t), b2 = Remap(a2, a3, t1, t3, t);
				samples.Add(Remap(b1, b2, t1, t2, t));
			}
		}

		samples.Add(_points[^1]);
		return samples;
	}

	private static Vector3 Remap ( Vector3 _a, Vector3 _b, float _ta, float _tb, float _t )
	{
		return _a * ((_tb - _t) / (_tb - _ta)) + _b * ((_t - _ta) / (_tb - _ta));
	}

	private bool TryGetBuildingCameraPose ( Building _building, out Vector3 _position, out Quaternion _rotation )
	{
		_position = default;
		_rotation = default;

		BuildingView view = m_buildingViews?.FirstOrDefault(buildingView => buildingView != null && buildingView.building == _building);
		if (view == null)
			return false;

		if (view.cameraPoint != null)
		{
			_position = view.cameraPoint.position;
			_rotation = view.cameraPoint.rotation;
			return true;
		}

		if (!TryGetMeshBounds(view.meshNamePrefix, out Bounds bounds))
			return false;

		_rotation = CameraManager.Instance.HubCameraRotation;
		float distance = CameraManager.Instance.GetDistanceToFit(bounds.extents.magnitude) * m_buildingFramingMargin;
		_position = bounds.center - _rotation * Vector3.forward * distance;
		return true;
	}

	private bool TryGetMeshBounds ( string _meshNamePrefix, out Bounds _bounds )
	{
		_bounds = default;
		if (string.IsNullOrEmpty(_meshNamePrefix))
			return false;

		if (m_meshBounds.TryGetValue(_meshNamePrefix, out _bounds))
			return true;

		bool hasBounds = false;

		foreach (GameObject root in gameObject.scene.GetRootGameObjects())
		{
			if (transform.IsChildOf(root.transform))
				continue;

			foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
			{
				if (!renderer.name.StartsWith(_meshNamePrefix, StringComparison.OrdinalIgnoreCase))
					continue;

				if (hasBounds)
					_bounds.Encapsulate(renderer.bounds);
				else
					_bounds = renderer.bounds;
				hasBounds = true;
			}
		}

		if (hasBounds)
			m_meshBounds[_meshNamePrefix] = _bounds;
		else
			Debug.LogWarning("HubManager: no renderer named " + _meshNamePrefix + "* in " + gameObject.scene.name, gameObject);

		return hasBounds;
	}

	#endregion

	#region Hangar

	public void ShowHangar ()
	{
		m_isHangarShown = true;

		if (m_hangar != null)
		{
			EnterHangar();
			return;
		}

		if (m_isLoadingHangar)
			return;

		if (string.IsNullOrEmpty(m_hangarSceneName) || !Application.CanStreamedLevelBeLoaded(m_hangarSceneName))
		{
			CameraManager.Instance.TeleportCameraTo(m_hangarCameraPosition);
			RefreshSquadEntities();
			return;
		}

		m_isLoadingHangar = true;
		SceneManager.LoadSceneAsync(m_hangarSceneName, LoadSceneMode.Additive).completed += _ => OnHangarSceneLoaded();
	}

	private void OnHangarSceneLoaded ()
	{
		m_isLoadingHangar = false;
		if (this == null)
			return;

		Scene hangarScene = SceneManager.GetSceneByName(m_hangarSceneName);
		if (hangarScene.IsValid())
		{
			foreach (GameObject root in hangarScene.GetRootGameObjects())
			{
				if (root.TryGetComponent(out m_hangar))
					break;
			}
		}

		if (m_hangar == null)
		{
			Debug.LogError("HubManager: " + m_hangarSceneName + " has no HangarEnvironment at its root", gameObject);
			if (m_isHangarShown)
			{
				CameraManager.Instance.TeleportCameraTo(m_hangarCameraPosition);
				RefreshSquadEntities();
			}
			return;
		}

		if (m_isHangarShown)
			EnterHangar();
		else
			m_hangar.SetShown(false);
	}

	private void EnterHangar ()
	{
		SetHubVisualsHidden(true);
		m_hangar.SetShown(true);
		CameraManager.Instance.PlaceCameraAt(m_hangar.CameraPoint);
		RefreshSquadEntities();
	}

	public void HideHangar ()
	{
		m_isHangarShown = false;

		foreach (Entity entity in m_squadEntities.Values)
		{
			if (entity != null)
				Destroy(entity.gameObject);
		}
		m_squadEntities.Clear();

		if (m_hangar != null)
			m_hangar.SetShown(false);
		SetHubVisualsHidden(false);

		PlaceCameraOn(GetBuildingOf(UIManager.Instance.currentPanel) ?? Building.Overview, 0f);
	}

	private void SetHubVisualsHidden ( bool _isHidden )
	{
		if (!_isHidden)
		{
			foreach (GameObject root in m_hiddenHubRoots)
			{
				if (root != null)
					root.SetActive(true);
			}
			m_hiddenHubRoots.Clear();
			return;
		}

		foreach (GameObject root in gameObject.scene.GetRootGameObjects())
		{
			if (!root.activeSelf || transform.IsChildOf(root.transform) || root.GetComponentInChildren<HubTonemapping>(true) != null)
				continue;

			root.SetActive(false);
			m_hiddenHubRoots.Add(root);
		}
	}

	private Transform SquadParent => m_hangar != null ? m_hangar.SquadParent : m_squadEntitiesParent.transform;

	#endregion

	public void SelectEntity(Entity _selectedEntity )
	{
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(_selectedEntity.Data, false);

		//RefreshEntitiesPosition();
	}

	public void RefreshSquadEntities ()
	{
		if (!m_isHangarShown || m_isLoadingHangar)
			return;

		List<EntitySavedData> squadData = GameDatas.current.currentPlayerSave.GetSquadEntitiesData();
		foreach (EntitySavedData entityData in m_squadEntities.Keys.ToArray())
		{
			if (!squadData.Contains(entityData))
			{
				Destroy(m_squadEntities[entityData].gameObject);
				m_squadEntities.Remove(entityData);
			}
		}

		for(int i = 0; i < squadData.Count; i++)
		{
			if(!m_squadEntities.ContainsKey(squadData[i]))
				AddEntity(squadData[i]);

			//entity.RefreshHangarVisual ?
		}

		RefreshEntitiesPosition();
	}

	public void AddEntity (EntitySavedData _newEntity)
	{
		Entity entity = Instantiate(_newEntity.FrameData != null ? _newEntity.FrameData.prefab : GameAssets.current.game.defaultEntity, SquadParent);
		m_squadEntities.Add(_newEntity, entity);
		entity.transform.rotation = SquadParent.rotation * Quaternion.Euler(0f, 180f, 0f);
		entity.InitHangarMode(_newEntity);

		RefreshEntitiesPosition();
	}

	public void RefreshEntitiesPosition ()
	{
		List<EntitySavedData> keys = m_squadEntities.Keys.ToList();
		float firstOffset = m_hangar != null ? -m_unitSpacing * (keys.Count - 1) * .5f : 0f;
		for (int i = 0; i < keys.Count; i++)
		{
			m_squadEntities[keys[i]].transform.localPosition = new Vector3(firstOffset + m_unitSpacing * i, 0f, 0f);
		}
	}

}
