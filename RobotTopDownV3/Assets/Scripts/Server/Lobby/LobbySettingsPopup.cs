using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LobbySettingsPopup : AUIPopup
{
    [SerializeField] private TMP_InputField m_lobbyNameInput;
    [SerializeField] private TMP_InputField m_portInput;
    [SerializeField] private BaseButton m_closeBtn;

	private void Awake ()
	{
        m_closeBtn.onClick += OnClickClose;

    }

    private void OnClickClose ()
	{
        Close();
	}

}
