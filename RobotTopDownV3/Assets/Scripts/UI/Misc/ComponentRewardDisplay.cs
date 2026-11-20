using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ComponentRewardDisplay : ARewardDisplay
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private TextMeshProUGUI m_priceTMP;
    [SerializeField] private Image m_mainIcon;

    private EntityEquipmentData m_component;
    public EntityEquipmentData Component => m_component;
    private bool m_isBlueprint = false;
    public bool IsBlueprint => m_isBlueprint;

    public void Init ( EntityEquipmentData _component, System.Action _onSelected, bool _isLockedOnSelected = false, bool _isBlueprint = false )
	{
        m_component = _component;
        m_isBlueprint = _isBlueprint;
        m_nameTMP.text = _isBlueprint
            ? string.Format(LocalizationManager.Instance.Get(LocalizationKey.reward_blueprint), _component.GetLocalizedName())
            : _component.GetLocalizedName();
        m_mainIcon.sprite = _component.icon;
        m_priceTMP.text = _component.IsMainComponent() ? "3" : "2";
        InitSelection(_onSelected, _isLockedOnSelected);
    }
}
