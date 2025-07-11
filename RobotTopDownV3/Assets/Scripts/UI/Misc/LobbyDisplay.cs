using UnityEngine;

public class LobbyDisplay : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI m_title;
    [SerializeField] private BaseButton m_joinBtn;

    private System.Net.IPEndPoint m_endpoint;

    public void Setup ( string lobbyName, System.Action onJoin, System.Net.IPEndPoint endpoint )
    {
        m_title.text = lobbyName;
        m_joinBtn.onClick += onJoin;
        m_endpoint = endpoint;
    }

    public bool Matches ( System.Net.IPEndPoint endpoint )
    {
        return m_endpoint.Address.Equals(endpoint.Address) && m_endpoint.Port == endpoint.Port;
    }
}
