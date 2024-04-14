using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class RobotEntity : MonoBehaviour
{
    public Action onSelect;
    public Action onDeselect;

    [Title("Depedencies")]
    [SerializeField] private RobotDisplacementPlugin m_displacement;
    public RobotDisplacementPlugin Displacement => m_displacement;

    [SerializeField] private RobotEquipmentPlugin m_equipment;
    public RobotEquipmentPlugin Equipment => m_equipment;


    public void Select ()
	{
        onSelect?.Invoke();
    }

    public void Deselect ()
    {
        onDeselect?.Invoke();
    }

}
