using UnityEngine;

public class TutorialHighlightZone : MonoBehaviour
{
	public static TutorialHighlightZone currentActiveHighlightZone;
	public System.Action onInteract;

	[SerializeField] private GameObject m_highlight;
	[SerializeField] private string m_id;
	public string ID => m_id;

	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;

	private void Awake ()
	{
		FTUEManager.Instance.AddTutorialHighlightZone(this);
	}

	public void Show ()
	{
		if (currentActiveHighlightZone != null)
			currentActiveHighlightZone.Hide();

		currentActiveHighlightZone = this;
		m_isVisible = true;
		m_highlight.SetActive(true);
	}

	public void Hide ()
	{
		m_highlight.SetActive(false);
		currentActiveHighlightZone = null;
		m_isVisible = false;
	}
}
