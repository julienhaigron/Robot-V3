using UnityEngine;
using Sirenix.OdinInspector;
using System;

public abstract class ScriptableEnum<TEnum> : ScriptableObject, IScriptableEnum where TEnum : struct, Enum
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

	public string GetEnumName ()
	{
		return enumID.ToString();
	}

	private void OnValidate ()
	{
		if (Enum.TryParse<TEnum>(name, true, out var parsed))
			enumID = parsed;
	}
#endif
}

public interface IScriptableEnum
{
	string GetEnumName ();
}