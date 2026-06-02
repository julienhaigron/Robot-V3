using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SfxDatabase", menuName = "Audio/Sfx Database")]
public class SfxDatabase : ScriptableObject
{
	[SerializeField]
	private List<SfxData> m_sounds = new();

	public IReadOnlyList<SfxData> Sounds => m_sounds;

#if UNITY_EDITOR
	public List<SfxData> EditorSounds => m_sounds;
#endif

}