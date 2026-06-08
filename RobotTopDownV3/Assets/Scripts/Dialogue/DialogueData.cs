using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueData", menuName = "ScriptableObject/DialogueData")]
public class DialogueData : ScriptableObject
{
	public List<Line> lines = new();

	[System.Serializable]
    public class Line
	{
		public string characterName;
		public Sprite characterSprite;
		public bool isSpriteOnLeft;
		public string sentence;
	}
}
