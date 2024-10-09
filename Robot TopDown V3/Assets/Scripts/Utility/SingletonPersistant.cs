using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonPersistant<T> : Singleton<T> where T : SingletonPersistant<T>
{
    public override void Awake ()
    {
        base.Awake();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif
        if (Instance == this)
        {
            DontDestroyOnLoad(transform.gameObject);
        }
    }
}
