using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotPlugin : MonoBehaviour
{
    [SerializeField] private RobotEntity m_linkedEntity;

	private void Reset ()
	{
		m_linkedEntity = GetComponent<RobotEntity>();
	}
}
