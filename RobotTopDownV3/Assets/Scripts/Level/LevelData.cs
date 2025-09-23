using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Level", menuName = "ScriptableObject/Level")]
public class LevelData : ScriptableObject
{
    public Sprite icon;
    public string title;

    public GridData map;
    public List<EntityData> enemies;
}
