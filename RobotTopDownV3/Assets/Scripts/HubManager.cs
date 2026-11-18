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
		public float axisOffset;
		public float fieldOfView;
		public Transform cameraPoint;
	}

	[Title("Depedencies")]
	[SerializeField] private GameObject m_squadEntitiesParent;
	[SerializeField] private Transform m_hangarCameraPosition;

	[Title("Buildings camera")]
	[SerializeField] private BuildingView[] m_buildingViews = GetDefaultBuildingViews();
	[SerializeField] private float m_buildingFramingMargin = 1.15f;
	[SerializeField] private float m_cameraMoveDuration = 1.75f;
	[SerializeField] private float m_cameraMaxMoveDuration = 4.5f;
	[SerializeField] private float m_cameraSpeed = 1.2f;

	[Title("Station axis")]
	[SerializeField] private string m_moonMeshName = "Moon";
	[SerializeField] private string m_entranceMeshNamePrefix = "Entry-Ring";
	[SerializeField] private float m_entranceOuterOffset = .45f;

	[Title("Hangar")]
	[SerializeField] private string m_hangarSceneName = "HangarScene";

	[Title("Parameters")]
	[SerializeField] private float m_unitSpacing = 1.5f;

	private SerializableDictionary<EntitySavedData, Entity> m_squadEntities = new();
	private bool m_isHangarShown;
	public bool IsHangarShown => m_isHangarShown;
	public bool IsHangarReady => m_isHangarShown && !m_isLoadingHangar;
	private bool m_isLoadingHangar;
	private bool m_isCameraHeld;
	private HangarEnvironment m_hangar;
	private readonly List<GameObject> m_hiddenHubRoots = new();
	private readonly Dictionary<string, Bounds> m_meshBounds = new();

	private static BuildingView[] GetDefaultBuildingViews ()
	{
		return new BuildingView[]
		{
			new() { building = Building.Hangar, meshNamePrefix = "Hangar", axisOffset = .3f, fieldOfView = 50f },
			new() { building = Building.Recycler, meshNamePrefix = "Recycle", axisOffset = .45f },
			new() { building = Building.RepairStation, meshNamePrefix = "Repar-Station", axisOffset = 2.55f },
			new() { building = Building.PsyShop, meshNamePrefix = "Psy-QG", axisOffset = .42f },
			new() { building = Building.CommandoShop, meshNamePrefix = "Commando-QG", axisOffset = .5f },
			new() { building = Building.PaladinShop, meshNamePrefix = "Paladin-QG", axisOffset = .46f },
			new() { building = Building.Arena, meshNamePrefix = "Arene", axisOffset = 2.67f },
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
			SelectMissionPanel or MissionPanel or TournamentPanel => Building.Arena,
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
		if (m_isHangarShown || m_isCameraHeld)
			return;

		PlaceCameraOn(_building, m_cameraMoveDuration);
	}

	public void SetCameraHeld ( bool _isHeld )
	{
		m_isCameraHeld = _isHeld;
	}

	public float FocusCurrentBuilding ()
	{
		if (m_isHangarShown)
			return 0f;

		return PlaceCameraOn(GetBuildingOf(UIManager.Instance.currentPanel) ?? Building.Overview, m_cameraMoveDuration);
	}

	public void PlaceCameraOnBuilding ( Building _building )
	{
		if (!m_isHangarShown)
			PlaceCameraOn(_building, 0f);
	}

	public float DiveToBuilding ( Building _building, float _duration )
	{
		if (m_isHangarShown || !TryGetBuildingCameraPose(_building, out Vector3 position, out Quaternion rotation, out float fieldOfView))
			return 0f;

		CameraManager.Instance.MoveCameraAlong(BuildCameraPath(CameraManager.Instance.Camera.transform.position, position), rotation, fieldOfView, _duration);
		return _duration;
	}

	public void PushIntoHangar ( float _distance, float _duration )
	{
		if (!m_isHangarShown || m_hangar == null || m_hangar.CameraPoint == null)
			return;

		Transform point = m_hangar.CameraPoint;
		Vector3 start = point.position - point.forward * _distance;
		CameraManager.Instance.PlaceCameraAt(start, point.rotation);
		CameraManager.Instance.MoveCameraAlong(new[] { start, point.position }, point.rotation, CameraManager.Instance.DefaultFieldOfView, _duration);
	}

	public float PullBackInHangar ( float _distance, float _duration )
	{
		if (!m_isHangarShown || m_hangar == null || m_hangar.CameraPoint == null)
			return 0f;

		Transform point = m_hangar.CameraPoint;
		Vector3 end = point.position - point.forward * _distance;
		CameraManager.Instance.MoveCameraAlong(new[] { CameraManager.Instance.Camera.transform.position, end }, point.rotation, CameraManager.Instance.DefaultFieldOfView, _duration);
		return _duration;
	}

	private float PlaceCameraOn ( Building _building, float _minDuration )
	{
		float fieldOfView = CameraManager.Instance.DefaultFieldOfView;
		if ((_building == Building.Overview || !TryGetBuildingCameraPose(_building, out Vector3 position, out Quaternion rotation, out fieldOfView))
			&& !CameraManager.Instance.TryGetHubCameraPose(out position, out rotation))
			return 0f;

		if (_minDuration <= 0f)
		{
			CameraManager.Instance.PlaceCameraAt(position, rotation, fieldOfView);
			return 0f;
		}

		List<Vector3> path = BuildCameraPath(CameraManager.Instance.Camera.transform.position, position);
		float length = 0f;
		for (int i = 1; i < path.Count; i++)
			length += Vector3.Distance(path[i - 1], path[i]);

		float duration = m_cameraSpeed > 0f ? Mathf.Clamp(length / m_cameraSpeed, _minDuration, Mathf.Max(_minDuration, m_cameraMaxMoveDuration)) : _minDuration;
		CameraManager.Instance.MoveCameraAlong(path, rotation, fieldOfView, duration);
		return duration;
	}

	private List<Vector3> BuildCameraPath ( Vector3 _start, Vector3 _end )
	{
		List<Vector3> waypoints = new() { _start };
		if (TryGetStationAxis(out StationAxis station) && !station.IsClearOutside(_start, _end))
		{
			AddWaypoint(waypoints, station.GetAnchor(_start));
			AddWaypoint(waypoints, station.GetAnchor(_end));
		}

		AddWaypoint(waypoints, _end);
		return SampleCatmullRom(waypoints, 12);
	}

	private bool TryGetStationAxis ( out StationAxis _station )
	{
		_station = default;
		if (!TryGetMeshBounds(m_moonMeshName, out Bounds moonBounds) || !TryGetMeshBounds(m_entranceMeshNamePrefix, out Bounds entranceBounds))
			return false;

		_station = new StationAxis(moonBounds, entranceBounds, m_entranceOuterOffset);
		return true;
	}

	private readonly struct StationAxis
	{
		private const float k_onAxisTolerance = .02f;
		private const float k_moonClearance = 1.08f;

		public readonly Vector3 center;
		public readonly float radius;
		public readonly Vector3 axis;
		private readonly float m_entranceAnchor;

		public StationAxis ( Bounds _moon, Bounds _entrance, float _outerOffset )
		{
			center = _moon.center;
			radius = Mathf.Max(_moon.extents.x, Mathf.Max(_moon.extents.y, _moon.extents.z));
			Vector3 toEntrance = _entrance.center - center;
			axis = toEntrance.sqrMagnitude > 1e-8f ? toEntrance.normalized : Vector3.forward;
			m_entranceAnchor = Vector3.Dot(toEntrance, axis) + _outerOffset;
		}

		public bool IsInside ( Vector3 _point ) => (_point - center).sqrMagnitude < radius * radius;
		public float Axial ( Vector3 _point ) => Vector3.Dot(_point - center, axis);
		public Vector3 At ( float _axial ) => center + axis * _axial;
		public Vector3 OnAxis ( Vector3 _point ) => At(Axial(_point));

		public Vector3 GetAnchor ( Vector3 _point )
		{
			Vector3 onAxis = OnAxis(_point);
			if (IsInside(_point) || (_point - onAxis).magnitude < radius * k_onAxisTolerance)
				return onAxis;

			return At(Axial(_point) >= 0f ? m_entranceAnchor : -m_entranceAnchor);
		}

		public bool IsClearOutside ( Vector3 _from, Vector3 _to )
		{
			if (IsInside(_from) || IsInside(_to))
				return false;

			Vector3 segment = _to - _from;
			float progress = segment.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(center - _from, segment) / segment.sqrMagnitude) : 0f;
			return (_from + segment * progress - center).magnitude >= radius * k_moonClearance;
		}
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

	private bool TryGetBuildingCameraPose ( Building _building, out Vector3 _position, out Quaternion _rotation, out float _fieldOfView )
	{
		_position = default;
		_rotation = default;
		_fieldOfView = CameraManager.Instance.DefaultFieldOfView;

		BuildingView view = m_buildingViews?.FirstOrDefault(buildingView => buildingView != null && buildingView.building == _building);
		if (view == null)
			return false;

		if (view.cameraPoint != null)
		{
			_position = view.cameraPoint.position;
			_rotation = view.cameraPoint.rotation;
			if (view.fieldOfView > 0f)
				_fieldOfView = view.fieldOfView;
			return true;
		}

		if (!TryGetMeshBounds(view.meshNamePrefix, out Bounds bounds) || !TryGetStationAxis(out StationAxis station))
			return false;

		_position = station.At(station.Axial(bounds.center) + view.axisOffset);
		Vector3 toBuilding = bounds.center - _position;
		if (toBuilding.sqrMagnitude < 1e-8f)
			return false;

		Vector3 up = Vector3.Cross(toBuilding.normalized, Vector3.up).sqrMagnitude < 1e-6f ? station.axis : Vector3.up;

		_rotation = Quaternion.LookRotation(toBuilding, up);
		_fieldOfView = view.fieldOfView > 0f
			? view.fieldOfView
			: CameraManager.Instance.GetFieldOfViewToFit(bounds.extents.magnitude * m_buildingFramingMargin, toBuilding.magnitude);
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
