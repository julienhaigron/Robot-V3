using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_title;
    [SerializeField] private Image m_iconImg;
    [SerializeField] private Image[] m_armsImgs;
    [SerializeField] private BaseButton m_openEntityConfigBtn;

    public void Init ( EntitySavedData _unitData )
    {
        m_title.text = _unitData.name;
        m_iconImg.sprite = GameAssets.current.equipments[_unitData.frameID].icon;
		for (int i = 0; i < _unitData.armsIds.Length; i++)
		{
            m_armsImgs[i].sprite = GameAssets.current.equipments[_unitData.armsIds[i].value].icon;
		}
        m_openEntityConfigBtn.onClick += OnClickOpenEntityConfigBtn;
    }

    private void OnClickOpenEntityConfigBtn ()
    {
        UIManager.Instance.OpenPanel<EntityConfigPanel>();
    }
}
