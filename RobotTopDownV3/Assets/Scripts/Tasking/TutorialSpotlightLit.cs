using UnityEngine;

public class TutorialSpotlightLit : MonoBehaviour
{
	private void OnEnable ()
	{
		TutorialSpotlight.AddLit((RectTransform)transform);
	}

	private void OnDisable ()
	{
		TutorialSpotlight.RemoveLit((RectTransform)transform);
	}
}
