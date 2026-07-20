using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ShieldData", menuName = "ScriptableObject/Equipment/ShieldData", order = 1)]
public class ShieldEquipmentData : ToolEquipmentData
{
    public int hp = 10;
}
