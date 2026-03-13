using UnityEngine;
using Sirenix.OdinInspector;
using System;

public abstract class ScriptableEnum<TEnum> : ScriptableObject where TEnum : struct, Enum
{
    [ReadOnly]
    public TEnum enumID;

#if UNITY_EDITOR
	[Button]
	private void RefreshAllEnum ()
	{
		ScriptableEnumAutoEditor.GenerateAllEnums();
		if (Enum.TryParse<TEnum>(name, true, out var parsed))
		{
			enumID = parsed;
		}
	}

	private void OnValidate ()
	{
		if (Enum.TryParse<TEnum>(name, true, out var parsed))
			enumID = parsed;
	}
#endif
}