using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutoConsole : MonoBehaviour
{
    private List<DialogueData> m_allDialogs = new();
    public List<DialogueData> AllDialogs => m_allDialogs;

    public void Init ()
	{
        

        Show(false);
    }

    public void Show (bool _isInstant)
	{
        gameObject.SetActive(true);
	}
    
    public void Hide ( bool _isInstant )
	{
        gameObject.SetActive(false);
	}
}
