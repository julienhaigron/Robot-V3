using UnityEditor;
using UnityEngine;

public static class EditorAudioPreview
{
	private static AudioSource m_previewSource;

	public static void Play ( AudioClip _clip, float _volume = 1f )
	{
		if (_clip == null)
			return;

		Stop();

		// AudioUtil interne UnityEditor
		var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

		var method = audioUtilType.GetMethod(
			"PlayPreviewClip",
			System.Reflection.BindingFlags.Static |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic
		);

		method?.Invoke(null, new object[] { _clip, 0, false });
	}

	public static void Stop ()
	{
		var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

		var method = audioUtilType.GetMethod(
			"StopAllPreviewClips",
			System.Reflection.BindingFlags.Static |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic
		);

		method?.Invoke(null, null);
	}

	public static bool IsPlaying ()
	{
		var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

		var method = audioUtilType.GetMethod(
			"IsPreviewClipPlaying",
			System.Reflection.BindingFlags.Static |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic
		);

		if (method == null)
			return false;

		return (bool)method.Invoke(null, null);
	}
}