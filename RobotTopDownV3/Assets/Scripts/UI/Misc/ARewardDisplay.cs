using UnityEngine;

public abstract class ARewardDisplay : MonoBehaviour
{
	[SerializeField] private GameObject m_selectedGO;
	[SerializeField] private BaseButton m_btn;

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;
	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;
	private System.Action m_onSelected;

	protected void InitSelection ( System.Action _onSelected, bool _isLockedOnSelected )
	{
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
			m_btn.SetInteractability(true);
		}
	}

	public void SetInteractable ( bool _isInteractable )
	{
		if (!_isInteractable)
		{
			m_onSelected = null;
			m_btn.onClick = null;
		}

		m_btn.SetInteractability(_isInteractable);
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
