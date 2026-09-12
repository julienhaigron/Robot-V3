using UnityEngine;
using System.Collections.Generic;

public class TutorialHighlightZone : MonoBehaviour
{
	private static readonly List<TutorialHighlightZone> s_activeHighlightZones = new();
	public static IReadOnlyList<TutorialHighlightZone> ActiveHighlightZones => s_activeHighlightZones;

	public System.Action onInteract;

	[SerializeField] private GameObject m_highlight;
	[SerializeField] private string m_id;
	public string ID => m_id;

	[SerializeField] private BaseButton m_linkedButton;
	public BaseButton LinkedButton => m_linkedButton;
	[SerializeField] private BaseButton m_ownButton;
	public BaseButton OwnButton => m_ownButton;
	[SerializeField] private MPUIKIT.MPImage m_image;

	public BaseButton UsedButton => m_linkedButton != null ? m_linkedButton : m_ownButton;
	public bool IsUsingOwnButton => m_linkedButton == null;

	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;

	private void Awake ()
	{
		if(!string.IsNullOrEmpty(m_id))
			FTUEManager.Instance.AddTutorialHighlightZone(this);
		RefreshRaycastTarget();

		if (!m_isVisible)
			m_highlight.SetActive(false);
	}

	private void OnDestroy ()
	{
		s_activeHighlightZones.Remove(this);
	}

	public void SetId (string _id)
	{
		if (string.Equals(m_id, _id))
			return;

		FTUEManager.Instance.RemoveTutorialHighlightZone(this);
		m_id = _id;
		FTUEManager.Instance.AddTutorialHighlightZone(this);
	}

	public static void HideAllActive ()
	{
		for (int i = s_activeHighlightZones.Count - 1; i >= 0; i--)
		{
			if (s_activeHighlightZones[i] != null)
				s_activeHighlightZones[i].Hide();
			else
				s_activeHighlightZones.RemoveAt(i);
		}
	}

	public void Show ()
	{
		if (!s_activeHighlightZones.Contains(this))
			s_activeHighlightZones.Add(this);

		m_isVisible = true;
		RefreshRaycastTarget();
		m_highlight.SetActive(true);
	}

	public void Hide ()
	{
		m_highlight.SetActive(false);
		s_activeHighlightZones.Remove(this);
		m_isVisible = false;
	}

	private void RefreshRaycastTarget ()
	{
		if (m_image != null)
			m_image.raycastTarget = IsUsingOwnButton;
	}
}
