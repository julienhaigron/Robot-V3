using UnityEngine;

public class ItemUI : MonoBehaviour
{
	[SerializeField] private Canvas m_canvas;
	[SerializeField] private HealthBar m_healthBar;
	[SerializeField] private TargetMarker m_targetMarker;

	private Item m_item;

	public void Init ( Item _item )
	{
		m_item = _item;
		gameObject.SetActive(_item.Data.isTargetable);
		if (!_item.Data.isTargetable)
			return;

		m_healthBar.Init(_item.Invocator != null && _item.Invocator.IsAlliedTo(GameManager.Instance.PlayerID), _item.Health, _item.Data.maxHealth);
		m_targetMarker.Set(TargetMarker.State.None, true);
	}

	public void SetHealth ( int _health )
	{
		m_healthBar.SetHealth(_health, m_item.Data.maxHealth);
	}

	public void SetTargetMarker ( TargetMarker.State _state )
	{
		m_targetMarker.Set(_state);
	}

	private void LateUpdate ()
	{
		bool isShown = m_item != null && m_item.IsPlaced && m_item.IsShownToLocalPlayer;
		if (m_canvas.enabled != isShown)
			m_canvas.enabled = isShown;
	}
}
