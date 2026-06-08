using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SfxData
{
    public string Id;
    public SfxCategory Category;
    public AudioClip Clip;

    [Range(0f, 1f)]
    public float Volume = 1f;

    [Range(0.5f, 2f)]
    public float Pitch = 1f;
}

public enum SfxCategory
{
    UI,
    Combat,
    Character,
    Environment,
    Ambience,
    Misc
}