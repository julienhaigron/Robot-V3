using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RobotEntity : MonoBehaviour
{
    [Title("Depedencies")]
    [SerializeField] private RobotDisplacementPlugin m_displacement;
    public RobotDisplacementPlugin Displacement => m_displacement;

}
