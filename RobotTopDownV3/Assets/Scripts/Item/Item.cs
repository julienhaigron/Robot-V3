using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemUI m_ui;
    public ItemUI UI => m_ui;

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

    private int m_health;
    private int m_registeredHealth;
    public int Health => m_health;

    public void Init( int _id, AItemData _itemData, AItemLinkedData _linkedData, Entity _invocatorEntity, Tile _position )
	{
        m_id = _id;
        m_data = _itemData;
        m_linkedData = _linkedData;
        m_invocator = _invocatorEntity;
        m_currentPosition = _position;
        m_health = _itemData.maxHealth;
        m_registeredHealth = _itemData.maxHealth;

        if (m_ui != null)
            m_ui.Init(this);
    }

    public void SetPlaced ()
    {
        if (!m_isPlaced && m_linkedData != null)
            m_linkedData.OnPlannedItemPlaced();

        m_isPlaced = true;
        onAnyItemPlaced?.Invoke(this);
    }

    public bool IsTargetableBy ( int _ownerID, bool _isThisTurn )
    {
        return m_isPlaced && m_data.isTargetable && (_isThisTurn ? m_registeredHealth : m_health) > 0
            && m_invocator != null && !m_invocator.IsAlliedTo(_ownerID);
    }

    public bool IsShownToLocalPlayer => (m_invocator != null && m_invocator.IsAlliedTo(GameManager.Instance.PlayerID))
        || (m_currentPosition != null && m_currentPosition.IsVisible);

    public static bool TryGetTargetableOn ( Tile _tile, out Item _item )
    {
        _item = _tile == null ? null : _tile.GetItem(true);
        return _item != null && _item.IsPlaced && _item.Data.isTargetable && _item.Health > 0;
    }

    public void RegisterDamage ( Dictionary<WeaponEquipmentData.DamageType, int> _damages )
    {
        m_registeredHealth -= Sum(_damages);
    }

    public void TakeDamage ( Dictionary<WeaponEquipmentData.DamageType, int> _damages )
    {
        if (!m_data.isTargetable || m_health <= 0)
            return;

        m_health = Mathf.Max(0, m_health - Sum(_damages));
        m_registeredHealth = Mathf.Min(m_registeredHealth, m_health);
        if (m_ui != null)
            m_ui.SetHealth(m_health);

        if (m_health > 0)
            return;

        LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_item_destroyed), m_data.GetLocalizedName()), LogConsole.LogEventType.Death);
        if (IsShownToLocalPlayer && GameAssets.current.game.deathVFXPoolData != null)
            GameAssets.current.game.deathVFXPoolData.Get(transform.position, Quaternion.identity);
        Kill();
    }

    private static int Sum ( Dictionary<WeaponEquipmentData.DamageType, int> _damages )
    {
        int total = 0;
        foreach (int amount in _damages.Values)
            total += amount;
        return total;
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
