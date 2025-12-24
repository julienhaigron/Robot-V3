using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    public static T Instance 
    { 
        get 
        {
#if UNITY_EDITOR
            if(!Application.isPlaying)
                return FindAnyObjectByType<T>();
#endif
                return instance; 
        } 
    }

    public virtual void Awake ()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif
        if (instance == null)
        {
            instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
