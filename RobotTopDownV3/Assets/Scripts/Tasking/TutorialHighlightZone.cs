using UnityEngine;

public class TutorialHighlightZone : MonoBehaviour
{
	public System.Action onInteract;

	[SerializeField] private GameObject m_highlight;
	[SerializeField] private string m_id;
	public string ID => m_id;

	private void Start ()
	{
		FTUEManager.Instance.AddTutorialHighlightZone(this);
	}

	public void Show ()
	{
		m_highlight.SetActive(true);
	}

	public void Hide ()
	{
		m_highlight.SetActive(false);
	}
}
