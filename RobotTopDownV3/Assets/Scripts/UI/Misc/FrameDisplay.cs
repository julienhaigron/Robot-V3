using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FrameDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_title;
    [SerializeField] private BaseButton m_createBtn;
    [SerializeField] private Image m_iconImg;

    public void Init ( FrameEquipmentData _frameData, System.Action _onClickAction )
    {
		m_title.text = _frameData.name;
		m_iconImg.sprite = _frameData.icon;
        m_createBtn.onClick += _onClickAction;

    }
}
