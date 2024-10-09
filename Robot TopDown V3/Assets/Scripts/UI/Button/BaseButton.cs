using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BaseButton : MonoBehaviour
{
    public System.Action onClick;

    [SerializeField] private Button m_button;
    public Button Button => m_button;

    private bool m_isVisible = false;

    void Start()
    {
        m_button.onClick.AddListener(OnClick);
    }

    private void OnDestroy ()
    {
        m_button.onClick.RemoveListener(OnClick);
    }

    public void SetVisible ( bool _isVisible, bool _isInstant )
    {
        if (m_isVisible == _isVisible)
            return;

        m_isVisible = _isVisible;

        if (_isInstant)
            transform.localScale = _isVisible ? Vector3.one : Vector3.zero;
        else
            transform.DOScale(_isVisible ? 1f : 0f, 1f);
    }

    private void OnClick ()
    {
        onClick?.Invoke();
    }
}
