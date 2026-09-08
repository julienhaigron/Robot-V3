using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LocalizedDataExtensions
{
	private static readonly Dictionary<Object, string> m_keys = new();

	public static string GetLocalizedName ( this EntityActionData _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "action"), _data.displayName);
	}

	public static string GetLocalizedName ( this EntityEquipmentData _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "component"), _data.displayName);
	}

	public static string GetLocalizedName ( this AEntityStatus _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "status"), _data.name);
	}

	public static string GetLocalizedDescription ( this AEntityStatus _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "status") + "_desc", _data.description);
	}

	public static string GetLocalizedName ( this AEntityPassiveEffect _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "effect"), _data.displayName);
	}

	public static string GetLocalizedName ( this MissionData _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "mission"), _data.missionName);
	}

	public static string GetLocalizedName ( this UpgradeAsset _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "upgrade"), _data.displayName);
	}

	public static string GetLocalizedName ( this UnitPreset _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "unit"), _data.displayName);
	}

	public static string GetLocalizedName ( this Currency _data )
	{
		return _data == null ? "" : LocalizationManager.Instance.Get(KeyOf(_data, "currency"), _data.displayName);
	}

	private static string KeyOf ( Object _asset, string _section )
	{
		if (m_keys.TryGetValue(_asset, out string key))
			return key;

		key = _section + "/" + Sanitize(_asset.name);
		m_keys[_asset] = key;
		return key;
	}

	private static string Sanitize ( string _name )
	{
		StringBuilder builder = new StringBuilder(_name.Length);

		for (int i = 0; i < _name.Length; i++)
		{
			char character = _name[i];
			builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_');
		}

		return builder.ToString();
	}
}
