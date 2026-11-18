using UnityEngine;

public class Item : MonoBehaviour
{

    private AItemData m_data;
    public AItemData Data => m_data;

    private AItemLinkedData m_linkedData;
    public AItemLinkedData LinkedData => m_linkedData;

    public static System.Action<Item> onAnyItemPlaced;

    private Entity m_invocator;
    public Entity Invocator => m_invocator;
    private Tile m_currentPosition;
    public Tile CurrentPosition => m_currentPosition;

    private bool m_isPlaced;
    public bool IsPlaced => m_isPlaced;

    private int m_id;
    public int ID => m_id;

    public void Init( int _id, AItemData _itemData, AItemLinkedData _linkedData, Entity _invocatorEntity, Tile _position )
	{
        m_id = _id;
        m_data = _itemData;
        m_linkedData = _linkedData;
        m_invocator = _invocatorEntity;
        m_currentPosition = _position;
    }

    public void SetPlaced ()
    {
        m_isPlaced = true;
        onAnyItemPlaced?.Invoke(this);
    }

    public void MoveTo ( Tile _tile )
    {
        m_currentPosition = _tile;
        transform.position = _tile.transform.position;
    }

    public void OnTileEnter(Entity _enteringEntity, bool _isFromTeleportation )
	{
        m_data.OnWalkThrough(_enteringEntity, m_linkedData, this, _isFromTeleportation);
    }

    public void Cancel ()
	{
        Destroy(gameObject);
	}

    public void Kill ()
    {
        Destroy(gameObject);
    }

}
