using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ShieldData", menuName = "ScriptableObject/Equipment/ShieldData", order = 1)]
public class ShieldEquipmentData : ToolEquipmentData
{
    public int hp = 10;
    [Range(1, 3), Tooltip("1 = devant, 2 = devant + droite, 3 = devant + les deux flancs.")]
    public int coveredSides = 1;
    [Min(0), Tooltip("0 = toujours levé. Sinon le bouclier démarre éteint : une action ActivateEnergyShield le lève pour ce nombre de ticks, PV pleins à chaque activation, même après une casse.")]
    public int activeDuration = 0;
}
