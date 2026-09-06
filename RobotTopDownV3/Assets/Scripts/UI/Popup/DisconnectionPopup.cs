using UnityEngine;
using TMPro;

public class DisconnectionPopup : AUIPopup
{
	[Header("General")]
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;

	private int m_disconnectedPlayerIndex;
	private bool m_isLocalDisconnection;
	private float m_remainingTime;
	private bool m_isCountingDown;

	public void Init ( int _disconnectedPlayerIndex, float _waitDuration )
	{
		m_disconnectedPlayerIndex = _disconnectedPlayerIndex;
		m_isLocalDisconnection = false;

		StartCountdown(_waitDuration);
	}

	public void InitForLocalDisconnection ( float _waitDuration )
	{
		m_isLocalDisconnection = true;

		StartCountdown(_waitDuration);
	}

	private void StartCountdown ( float _waitDuration )
	{
		m_remainingTime = _waitDuration;
		m_isCountingDown = true;

		RefreshText();
	}

	//The pause runs on timeScale 0, so the countdown reads unscaled time.
	private void Update ()
	{
		if (!m_isCountingDown)
			return;

		m_remainingTime = Mathf.Max(0f, m_remainingTime - Time.unscaledDeltaTime);
		m_isCountingDown = m_remainingTime > 0f;

		RefreshText();
	}

	private void RefreshText ()
	{
		string cause = m_isLocalDisconnection
			? "Connection lost"
			: "Player " + (m_disconnectedPlayerIndex + 1) + " disconnected";

		m_descriptionTMP.text = cause + "\nRemaining waiting time " + Mathf.CeilToInt(m_remainingTime) + "s";
	}
}
