using UnityEngine;

public class Item : MonoBehaviour
{

    private AItemData m_data;
    public AItemData Data => m_data;

    private AItemLinkedData m_linkedData;
    public AItemLinkedData LinkedData => m_linkedData;

    private Entity m_invocator;
    private Tile m_currentPosition;
    public Tile CurrentPosition => m_currentPosition;

    public void Init( AItemData _itemData, AItemLinkedData _linkedData, Entity _invocatorEntity, Tile _position )
	{
        m_data = _itemData;
        m_linkedData = _linkedData;
        m_invocator = _invocatorEntity;
        m_currentPosition = _position;
    }

    public void OnTileEnter(Entity _enteringEntity )
	{
        m_data.OnWalkThrough(_enteringEntity, m_linkedData, this, null);
    }

}
