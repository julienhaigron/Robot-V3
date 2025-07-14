using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LobbySettingsPopup : AUIPopup
{
    [SerializeField] private TMP_InputField m_lobbyNameInput;
    [SerializeField] private TMP_InputField m_portInput;
    [SerializeField] private BaseButton m_closeBtn;

    public string LobbyName => string.IsNullOrWhiteSpace(m_lobbyNameInput.text)
        ? $"Lobby #{Random.Range(100, 999)}"
        : m_lobbyNameInput.text;

    public ushort Port => ushort.TryParse(m_portInput.text, out ushort port) ? port : ( ushort)7777;

	private void Awake ()
	{
        m_closeBtn.onClick += OnClickClose;

    }

    private void OnClickClose ()
	{
        Close();
	}

}
