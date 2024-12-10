using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_text;
    
    public void SetHealth(int _current, int _max )
	{
        m_text.text = _current + "/" + _max;
	}
}
