using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionDisplayOnTile : PoolElement
{
    [SerializeField] private MeshRenderer[] m_renderers;

	private TurnManager.RecordedAction m_recordedAction;
	public TurnManager.RecordedAction RecordedAction => m_recordedAction;
	public Tile OriginTile => GridManager.Instance.Tiles[m_recordedAction.action.supposedPositionAtActionStartID];

	public void Init (TurnManager.RecordedAction _recordedAction)
	{
		m_recordedAction = _recordedAction;
		SetMaterial(GameAssets.current.ui.entityStateMaterials[_recordedAction.entityState]);
	}

    public void SetMaterial(Material _mat)
	{
		foreach(MeshRenderer rd in m_renderers)
		{
			rd.material = _mat;
		}
	}
}
