using TMPro;
using UnityEngine;

public class LastSeenMarker : MonoBehaviour
{
	[SerializeField] private GhostEntity m_ghost;
	[SerializeField] private TMP_Text m_label;

	private int m_lastSeenRound;
	private Tile m_tile;
	public Tile Tile => m_tile;

	public void Init ( Entity _entity )
	{
		m_ghost.Init(_entity);
	}

	public void Show ( Tile _tile, int _orientation, int _lastSeenRound )
	{
		m_tile = _tile;
		m_lastSeenRound = _lastSeenRound;
		m_ghost.ShowAtPositionAndOrientation(_tile, _orientation);
		RefreshLabel();
	}

	public void Hide ()
	{
		m_tile = null;
		m_ghost.Hide();
	}

	public void RefreshLabel ()
	{
		int rounds = TurnManager.Instance.RoundCount - m_lastSeenRound;
		m_label.text = rounds <= 0 ? LocalizationManager.Instance.Get(LocalizationKey.fow_last_seen_now)
			: rounds == 1 ? LocalizationManager.Instance.Get(LocalizationKey.fow_last_seen_round)
			: string.Format(LocalizationManager.Instance.Get(LocalizationKey.fow_last_seen_rounds), rounds);
	}
}
