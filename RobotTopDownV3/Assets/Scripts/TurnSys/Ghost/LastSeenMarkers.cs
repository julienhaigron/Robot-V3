using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LastSeenMarkers : Singleton<LastSeenMarkers>
{
	[SerializeField] private LastSeenMarker m_markerPrefab;

	private readonly Dictionary<Entity, Tile> m_lastSeenTiles = new();
	private readonly Dictionary<Entity, LastSeenMarker> m_markers = new();

	private void OnEnable ()
	{
		TurnManager.onStartInputPhase += RefreshLabels;
		LocalizationManager.onLanguageChanged += RefreshLabels;
		SceneManager.activeSceneChanged += OnActiveSceneChanged;
	}

	private void OnDisable ()
	{
		TurnManager.onStartInputPhase -= RefreshLabels;
		LocalizationManager.onLanguageChanged -= RefreshLabels;
		SceneManager.activeSceneChanged -= OnActiveSceneChanged;
	}

	public void OnVisibilityChanged ( Entity _entity, bool _wasVisible )
	{
		if (_entity.Equipment.IsDead)
			return;

		if (_entity.IsVisible)
		{
			m_lastSeenTiles[_entity] = _entity.Displacement.Coordinates.GetTile();
			HideMarkerOf(_entity);
		}
		else if (_wasVisible && m_lastSeenTiles.TryGetValue(_entity, out Tile lastSeenTile))
		{
			GetMarkerOf(_entity).Show(lastSeenTile, _entity.Displacement.CurrentOrientation, TurnManager.Instance.RoundCount);
		}
	}

	public void Forget ( Entity _entity )
	{
		m_lastSeenTiles.Remove(_entity);
		HideMarkerOf(_entity);
	}

	public void ForgetMarkersOn ( Tile _tile )
	{
		foreach (LastSeenMarker marker in m_markers.Values)
		{
			if (marker.Tile == _tile)
				marker.Hide();
		}
	}

	private LastSeenMarker GetMarkerOf ( Entity _entity )
	{
		if (!m_markers.TryGetValue(_entity, out LastSeenMarker marker))
		{
			marker = Instantiate(m_markerPrefab, transform);
			marker.Init(_entity);
			m_markers.Add(_entity, marker);
		}

		return marker;
	}

	private void HideMarkerOf ( Entity _entity )
	{
		if (m_markers.TryGetValue(_entity, out LastSeenMarker marker) && marker.Tile != null)
			marker.Hide();
	}

	private void OnActiveSceneChanged ( Scene _previous, Scene _next )
	{
		foreach (LastSeenMarker marker in m_markers.Values)
			Destroy(marker.gameObject);

		m_markers.Clear();
		m_lastSeenTiles.Clear();
	}

	private void RefreshLabels ()
	{
		foreach (LastSeenMarker marker in m_markers.Values)
		{
			if (marker.Tile != null)
				marker.RefreshLabel();
		}
	}
}
