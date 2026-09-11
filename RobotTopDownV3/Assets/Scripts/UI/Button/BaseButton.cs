using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class BaseButton : MonoBehaviour, IPointerEnterHandler
{
    public System.Action onClick;

    [SerializeField] protected TMPro.TMP_Text m_label;
    [SerializeField] protected Button m_button;
    [SerializeField] protected Image m_image;
    public Button Button => m_button;
    public Image Image => m_image;
    public TMPro.TMP_Text Label => m_label;

    public void SetLabel ( string _text )
    {
        if (m_label == null)
            return;

        m_label.gameObject.SetActive(!string.IsNullOrEmpty(_text));
        m_label.text = _text;
    }

    [Header("Sounds")]
    [SerializeField] protected bool m_useDefaultSfx = true;
    [SerializeField] protected SfxId m_clickSfxOverride = SfxId.None;
    [SerializeField] protected SfxId m_hoverSfxOverride = SfxId.None;

    protected bool m_isVisible = false;
    public bool IsVisible => m_isVisible;

    private void Start()
    {
        m_isVisible = gameObject.activeSelf;
        m_button.onClick.AddListener(OnClick);
    }

    private void OnDestroy ()
    {
        m_button.onClick.RemoveListener(OnClick);
    }

    protected virtual void OnClick ()
    {
        PlayClickSfx();
        onClick?.Invoke();
    }

    public virtual void OnPointerEnter ( PointerEventData eventData )
    {
        if (m_button != null && !m_button.interactable)
            return;

        SoundManager.PlayUISafe(m_hoverSfxOverride, _config => _config.buttonHover, m_useDefaultSfx);
    }

    protected void PlayClickSfx ()
    {
        SoundManager.PlayUISafe(m_clickSfxOverride, _config => _config.buttonClick, m_useDefaultSfx);
    }

    public virtual void SetVisible ( bool _isVisible, bool _isInstant )
    {
        if (!_isInstant && m_isVisible == _isVisible)
            return;

        m_isVisible = _isVisible;

        if (_isInstant)
            gameObject.SetActive(_isVisible);
		else
		{
            if (_isVisible)
                gameObject.SetActive(_isVisible);
            transform.DOScale(_isVisible ? 1f : 0f, 1f).OnComplete(() =>
            {
                gameObject.SetActive(_isVisible);
            });
		}
    }

    public virtual void SetInteractability(bool _isInteractable )
	{
        m_button.interactable = _isInteractable;
    }
}
