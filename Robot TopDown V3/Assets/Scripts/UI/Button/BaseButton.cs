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
}
