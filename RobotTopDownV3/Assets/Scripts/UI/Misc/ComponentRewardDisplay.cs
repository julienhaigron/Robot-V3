using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ComponentRewardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private TextMeshProUGUI m_priceTMP;
    [SerializeField] private Image m_mainIcon;
    [SerializeField] private Image m_subIcon;
    [SerializeField] private GameObject m_selectedGO;
    [SerializeField] private BaseButton m_btn;

    private bool m_isSelected = false;
    public bool IsSelected => m_isSelected;

    private bool m_isVisible = false;
    public bool IsVisible => m_isVisible;
    private EntityEquipmentData m_component;
    public EntityEquipmentData Component => m_component;
    private System.Action m_onSelected;

    public void Init(EntityEquipmentData _component, System.Action _onSelected, bool _isLockedOnSelected = false )
	{
        m_component = _component;
        m_nameTMP.text = _component.displayName;
        m_mainIcon.sprite = _component.icon;
        System.Tuple<CurrencyType, ulong> price = _component.GetPrice();
        m_priceTMP.text = price.Item2.ToString();
        //m_subIcon.sprite = 
        if (_isLockedOnSelected)
        {
            SetIsSelected(true);
            m_btn.SetInteractability(false);
        }
        else
        {
            SetIsSelected(false);
            m_onSelected = _onSelected;
            m_btn.onClick = OnClick;
        }
    }

    private void OnClick ()
	{
        SetIsSelected(!m_isSelected);
        m_onSelected?.Invoke();
    }

    public void SetIsSelected ( bool _isSelected )
    {
        m_isSelected = _isSelected;
        m_selectedGO.SetActive(m_isSelected);
    }

    public void Show ()
    {
        m_isVisible = true;
        gameObject.SetActive(true);
    }

    public void Hide ()
    {
        m_isVisible = false;
        m_isSelected = false;
        gameObject.SetActive(false);
    }


}
