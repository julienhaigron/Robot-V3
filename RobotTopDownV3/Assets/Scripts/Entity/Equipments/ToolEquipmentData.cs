using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ToolData", menuName = "ScriptableObject/Equipment/ToolData", order = 1)]
public class ToolEquipmentData : EntityEquipmentData
{
    public Tool prefab;

    //stat
    public int range = 0;

    //animation
    public string attackAnimationSuccessId;
    public string attackAnimationFailureId;
    public bool isTwoHanded = false;
}
