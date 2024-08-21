using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostEntity : MonoBehaviour
{

    private Entity m_linkedEntity;



    public void Init(Entity _linkedEntity )
	{
		m_linkedEntity = _linkedEntity;

		//TODO : take linkedEntity visual
	}

	public bool DisplayAction(int _actionPos )
	{
		if (TurnManager.Instance.RecordedActions == null || !TurnManager.Instance.RecordedActions.ContainsKey(m_linkedEntity) || TurnManager.Instance.RecordedActions[m_linkedEntity].Count <= _actionPos)
			return false;

		TurnManager.RecordedAction displayedAction = TurnManager.Instance.RecordedActions[m_linkedEntity].ToArray()[_actionPos];
		transform.position = displayedAction.action.positionAtActionStart.transform.position - new Vector3(0, -.5f, 0); //ground offset

		//put ghost at tile he would be at beginning of action
		//draw green arrow if movement to tile
		//draw other arrow in case of other interaction possible


		return true;
	}

	public void Hide ()
	{

	}

}
