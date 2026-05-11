using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EntityStatusDisplay : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_icon;

    private bool m_isActive = false;
    public bool IsActive => m_isActive;

    private EntityStatusEnumID m_statusID;
    public EntityStatusEnumID StatusID => m_statusID;

    public void SetStatus(EntityStatusEnumID _statusID)
	{
        m_statusID = _statusID;

        m_icon.sprite = GameAssets.current.game.entityStatus[_statusID].icon;
        m_isActive = true;
        gameObject.SetActive(true);
	}

    public void Hide ()
	{
        m_isActive = false;
        gameObject.SetActive(false);
    }
}
