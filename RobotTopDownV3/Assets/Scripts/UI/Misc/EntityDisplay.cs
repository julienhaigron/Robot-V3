using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntityDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_title;
    [SerializeField] private Image m_iconImg;
    [SerializeField] private Image[] m_armsImgs;
    [SerializeField] private BaseButton m_openEntityConfigBtn;

    private EntitySavedData m_unitData;

    public void Init ( EntitySavedData _unitData )
    {
        m_unitData = _unitData;
        m_title.text = _unitData.name;
        if (_unitData.frame != null && _unitData.frame.TryGetData(out EntityEquipmentData frameData))
            m_iconImg.sprite = frameData.icon;

        if (_unitData.arms != null)
		{
            for (int i = 0; i < _unitData.arms.Length && i < m_armsImgs.Length; i++)
		    {
                if (_unitData.arms[i] != null && _unitData.arms[i].TryGetData(out EntityEquipmentData armData))
                    m_armsImgs[i].sprite = armData.icon;
		    }
		}
        m_openEntityConfigBtn.onClick += OnClickOpenEntityConfigBtn;
    }

    private void OnClickOpenEntityConfigBtn ()
    {
        UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_unitData, false);
    }
}
