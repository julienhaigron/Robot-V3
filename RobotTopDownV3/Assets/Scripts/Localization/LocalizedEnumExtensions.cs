using System.Collections.Generic;
using System.Text;

/// <summary>
/// Traduit les enums affiches en UI via la LocalizationDatabase.
/// La cle est deduite du nom de la valeur : StatType.VisualCamo -> "stat/visual_camo".
/// </summary>
public static class LocalizedEnumExtensions
{
	private static readonly Dictionary<EntityEquipmentData.SecondaryStat.StatType, string> m_statKeys = new();
	private static readonly Dictionary<NeuronalMembraneEquipmentData.VisionTypes, string> m_visionKeys = new();

	public static string GetLocalizedTitle ( this EntityEquipmentData.SecondaryStat.StatType _type )
	{
		if (!m_statKeys.TryGetValue(_type, out string key))
		{
			key = "stat/" + ToSnakeCase(_type.ToString());
			m_statKeys[_type] = key;
		}

		return LocalizationManager.Instance.Get(key);
	}

	public static string GetLocalizedTitle ( this NeuronalMembraneEquipmentData.VisionTypes _type )
	{
		if (!m_visionKeys.TryGetValue(_type, out string key))
		{
			key = "vision/" + ToSnakeCase(_type.ToString());
			m_visionKeys[_type] = key;
		}

		return LocalizationManager.Instance.Get(key);
	}

	private static string ToSnakeCase ( string _name )
	{
		StringBuilder sb = new StringBuilder(_name.Length + 8);

		for (int i = 0; i < _name.Length; i++)
		{
			if (i > 0 && char.IsUpper(_name[i]) && !char.IsUpper(_name[i - 1]))
				sb.Append('_');

			sb.Append(char.ToLowerInvariant(_name[i]));
		}

		return sb.ToString();
	}
}
