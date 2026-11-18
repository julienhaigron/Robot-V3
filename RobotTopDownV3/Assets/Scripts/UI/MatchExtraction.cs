using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class MatchExtraction : MonoBehaviour, ILoadingScreen
{
	private static readonly string[] s_hologramFadeProperties = { "_Opacity", "_RimOpacity", "_ScanlineOpacity", "_OccludedOpacity" };

	[Title("Extraction")]
	[SerializeField] private float m_riseDuration = 1.8f;
	[SerializeField] private float m_riseFramingMargin = 1.1f;
	[SerializeField, Range(0f, 1f)] private float m_dematerializeEnd = .7f;

	[Title("Arrival")]
	[SerializeField] private float m_doorsHoldDuration = .25f;
	[SerializeField, Range(0f, 1f)] private float m_hubShownAt = .75f;

	private Material m_hologram;

	public void Show ( Action _onCovered )
	{
		StartCoroutine(PlayExtraction(_onCovered));
	}

	public void SetProgress ( float _progress )
	{
		PanelTransition transition = UIManager.Instance.PanelTransition;
		if (transition != null)
			transition.SetDoorsProgress(_progress);
	}

	public void Hide ( Action _onHidden )
	{
		StartCoroutine(PlayArrival(_onHidden));
	}

	private IEnumerator PlayExtraction ( Action _onCovered )
	{
		PanelTransition transition = UIManager.Instance.PanelTransition;
		if (transition != null)
			transition.BlockInput(true);

		List<Entity> squad = MatchBriefing.GetPlayerSquad();
		Material source = GameAssets.current.game.baseGhost != null ? GameAssets.current.game.baseGhost.HologramMaterial : null;
		m_hologram = source != null ? new Material(source) : null;
		if (m_hologram != null)
		{
			foreach (Entity entity in squad)
				entity.Skin.SetForcedLook(m_hologram);

			foreach (string property in s_hologramFadeProperties)
			{
				if (m_hologram.HasProperty(property))
					m_hologram.DOFloat(0f, property, m_riseDuration * m_dematerializeEnd).SetUpdate(true);
			}
		}

		Transform camera = CameraManager.Instance.Camera.transform;
		if (CameraManager.Instance.TryGetGridOverviewPosition(camera.forward, m_riseFramingMargin, out Vector3 overview))
			CameraManager.Instance.MoveCameraAlong(new[] { camera.position, overview }, camera.rotation, CameraManager.Instance.DefaultFieldOfView, m_riseDuration);

		float doorsDuration = transition != null ? transition.DoorsCloseDuration : 0f;
		yield return Wait(Mathf.Max(m_riseDuration * m_dematerializeEnd, m_riseDuration - doorsDuration));

		foreach (Entity entity in squad)
		{
			if (entity != null)
				entity.Skin.Hide();
		}

		if (transition != null)
			yield return transition.CloseDoors(LocalizationManager.Instance.Get(LocalizationKey.briefing_return_station));

		_onCovered?.Invoke();

		if (m_hologram != null)
		{
			DOTween.Kill(m_hologram);
			Destroy(m_hologram);
			m_hologram = null;
		}
	}

	private IEnumerator PlayArrival ( Action _onHidden )
	{
		PanelTransition transition = UIManager.Instance.PanelTransition;
		if (transition != null)
			transition.SetDoorsProgress(1f);

		yield return Wait(m_doorsHoldDuration);

		HubManager hub = HubManager.Instance;
		if (hub != null)
		{
			hub.SetCameraHeld(true);
			hub.PlaceCameraOnBuilding(HubManager.Building.Arena);
		}

		if (transition != null)
			StartCoroutine(transition.OpenDoors());

		float glideDuration = hub != null ? hub.FocusCurrentBuilding() : 0f;
		yield return Wait(glideDuration * m_hubShownAt);

		_onHidden?.Invoke();
		yield return Wait(glideDuration * (1f - m_hubShownAt));

		if (hub != null)
			hub.SetCameraHeld(false);
	}

	private static IEnumerator Wait ( float _duration )
	{
		for (float elapsed = 0f; elapsed < _duration; elapsed += Time.unscaledDeltaTime)
			yield return null;
	}
}
