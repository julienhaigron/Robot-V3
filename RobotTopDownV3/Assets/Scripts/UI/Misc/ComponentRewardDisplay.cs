using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ComponentRewardDisplay : ARewardDisplay
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private TextMeshProUGUI m_priceTMP;
    [SerializeField] private Image m_mainIcon;
    [SerializeField] private Image m_subIcon;

    private EntityEquipmentData m_component;
    public EntityEquipmentData Component => m_component;

    public void Init ( EntityEquipmentData _component, System.Action _onSelected, bool _isLockedOnSelected = false )
	{
        m_component = _component;
        m_nameTMP.text = GetDisplayedName(_component);
        m_mainIcon.sprite = _component.icon;
        m_subIcon.sprite = GameAssets.current.ui.corporationsIcons[_component.faction];
        m_priceTMP.text = MissionData.GetPointValueOf(_component).ToString();
        InitSelection(_onSelected, _isLockedOnSelected);
    }

    protected virtual string GetDisplayedName ( EntityEquipmentData _component )
	{
        return _component.GetLocalizedName();
	}

    public static void InitAll ( ComponentRewardDisplay[] _displays, System.Collections.Generic.List<EntityEquipmentData> _components, System.Action _onSelected, bool _isSelected )
	{
        for (int i = 0; i < _displays.Length; i++)
		{
            if (i >= _components.Count)
			{
                _displays[i].Hide();
                continue;
			}

            _displays[i].Show();
            _displays[i].Init(_components[i], _onSelected);
            _displays[i].SetIsSelected(_isSelected);
		}
	}
}
