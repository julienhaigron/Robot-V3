using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitRewardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private Image m_mainIcon;
    [SerializeField] private Image m_subIcon;

	public void Init(UnitPreset _unit)
	{
        m_nameTMP.text = _unit.displayName;
        m_mainIcon.sprite = _unit.icon;
        m_subIcon.sprite = GameAssets.current.ui.corporationsIcons[_unit.GetSavedData().GetDominentFaction(out float percentage)];
    }

    public void Show ()
    {
        gameObject.SetActive(true);
    }

    public void Hide ()
    {
        gameObject.SetActive(false);
    }


}
