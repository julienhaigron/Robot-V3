using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BaseButton : MonoBehaviour
{
    public System.Action onClick;

    [SerializeField] protected Button m_button;
    public Button Button => m_button;

    protected bool m_isVisible = false;

    private void Start()
    {
        m_button.onClick.AddListener(OnClick);
    }

    private void OnDestroy ()
    {
        m_button.onClick.RemoveListener(OnClick);
    }

    protected virtual void OnClick ()
    {
        onClick?.Invoke();
    }

    public virtual void SetVisible ( bool _isVisible, bool _isInstant )
    {
        if (m_isVisible == _isVisible)
            return;

        m_isVisible = _isVisible;

        if (_isInstant)
            transform.localScale = _isVisible ? Vector3.one : Vector3.zero;
        else
            transform.DOScale(_isVisible ? 1f : 0f, 1f);
    }

    public virtual void SetInteractability(bool _isInteractable )
	{
        m_button.interactable = _isInteractable;
    }
}
