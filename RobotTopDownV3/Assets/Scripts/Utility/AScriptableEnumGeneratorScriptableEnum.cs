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
#endif

	//Both must stay outside the guard: GetEnumName is the only member of IScriptableEnum, and subclasses
	//override OnValidate - a player build would have neither to satisfy.
	public string GetEnumName ()
	{
		return enumID.ToString();
	}

	protected virtual void OnValidate ()
	{
		if (Enum.TryParse<TEnum>(name, true, out var parsed))
			enumID = parsed;

	}
}

public interface IScriptableEnum
{
	string GetEnumName ();
}