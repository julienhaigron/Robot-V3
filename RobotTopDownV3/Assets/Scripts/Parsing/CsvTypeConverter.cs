using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CsvTypeConverter
{
    public static object Convert ( string _rawValue, Type _targetType )
    {
        #region Base Vars
        if (string.Equals(_rawValue, "-"))
            return default;
        else if (_targetType == typeof(string))
            return _rawValue;
        else if (_targetType == typeof(int))
            return int.Parse(_rawValue);
        else if (_targetType == typeof(float))
            return float.Parse(_rawValue);
        else if (_targetType == typeof(bool))
		{
            if (!string.IsNullOrEmpty(_rawValue) && string.Equals(_rawValue, "TRUE"))
                return true;
            else if (!string.IsNullOrEmpty(_rawValue) && string.Equals(_rawValue, "FALSE"))
                return false;
            else
                return bool.Parse(_rawValue);
		}
        else if (_targetType == typeof(Vector2Int))
		{
            Vector2Int value = new(0, 0);
            if(string.IsNullOrEmpty(_rawValue))
                return value;
            string[] splits = _rawValue.Split("-");
            if (splits.Length >= 1)
                value.x = int.Parse(splits[0]);
            if(splits.Length >= 2)
                value.x = int.Parse(splits[1]);

            return value;
		}
        #endregion

        #region Scriptables
        else if (_targetType == typeof(FrameEquipmentData))
        {
            if (TryGetFrameComponent(_rawValue, out FrameEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(ReactorEquipmentData))
        {
            if (TryGetReactorComponent(_rawValue, out ReactorEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(BrainEquipmentData))
        {
            if (TryGetBrainComponent(_rawValue, out BrainEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(NeuronalMembraneEquipmentData))
        {
            if (TryGetNeuronalMembraneComponent(_rawValue, out NeuronalMembraneEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(WeaponEquipmentData))
        {
            if (TryGetWeaponComponent(_rawValue, out WeaponEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(ToolEquipmentData))
        {
            if (TryGetToolComponent(_rawValue, out ToolEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(ArmorEquipmentData))
        {
            if (TryGetArmorComponent(_rawValue, out ArmorEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(OccultorEquipmentData))
        {
            if (TryGetOccultorComponent(_rawValue, out OccultorEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(ChipsetEquipmentData))
        {
            if (TryGetChipsetComponent(_rawValue, out ChipsetEquipmentData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(AItemData))
        {
            if (TryGetItemData(_rawValue, out AItemData data))
                return data;
            else
                return null;
        }
        else if (_targetType == typeof(EntityEquipmentData[]))
        {
            List<EntityEquipmentData> equipments = new();
            string[] rawValues = _rawValue.Replace(" ", "").Split(",");
            foreach (string raw in rawValues)
            {
                if (TryGetEquipmentComponent(_rawValue, out EntityEquipmentData data))
                    equipments.Add(data);
            }
            return equipments.ToArray();
        }
        #endregion

        #region Dictionaries
        else if (_targetType == typeof(SerializableDictionary<WeaponEquipmentData.DamageType, int>))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            string[] elems = _rawValue.Replace(" ", "").Split(",");

            SerializableDictionary<WeaponEquipmentData.DamageType, int> returnedValue = new();
            foreach (string elem in elems)
			{
                string[] rawValues = elem.Replace("[", "").Replace("]", "").Split(";");
                if (rawValues.Length < 2)
                    continue;
                returnedValue.Add((WeaponEquipmentData.DamageType)Convert(rawValues[0], typeof(WeaponEquipmentData.DamageType)), (int)Convert(rawValues[1], typeof(int)));
			}

            return returnedValue;
        }
        #endregion

        #region Class

        else if (_targetType == typeof(EntityEquipmentData.SecondaryStat))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            EntityEquipmentData.SecondaryStat stat = new();
            string[] vars = _rawValue.Replace(" ", "").Split(";");
            if (vars.Length < 2)
                return null;

            stat.type = (EntityEquipmentData.SecondaryStat.StatType)Convert(vars[0], typeof(EntityEquipmentData.SecondaryStat.StatType));
            stat.value = (float)Convert(vars[1], typeof(float));

            return stat;
        }
        else if (_targetType == typeof(EntityEquipmentData.SecondaryStat[]))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            List<EntityEquipmentData.SecondaryStat> stats = new();
            string[] rawStats = _rawValue.Split(",");
            foreach (string rawStat in rawStats)
                stats.Add((EntityEquipmentData.SecondaryStat)Convert(rawStat.Replace("[", "").Replace("]", ""), typeof(EntityEquipmentData.SecondaryStat)));

            return stats.ToArray();
        }

        else if (_targetType == typeof(ChipsetEquipmentData.ConditionalStatBonus))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            ChipsetEquipmentData.ConditionalStatBonus stat = new();
            string[] vars = _rawValue.Replace(" ", "").Split(";");
            if (vars.Length < 3)
                return null;

            stat.bonus = new()
            {
                type = (EntityEquipmentData.SecondaryStat.StatType)Convert(vars[0], typeof(EntityEquipmentData.SecondaryStat.StatType)),
                value = (float)Convert(vars[1], typeof(float))
            };
            stat.conditionType = (Condition.ConditionType)Convert(vars[2], typeof(Condition.ConditionType));

            return stat;
        }
        else if (_targetType == typeof(ChipsetEquipmentData.ConditionalStatBonus[]))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            List<ChipsetEquipmentData.ConditionalStatBonus> stats = new();
            string[] rawStats = _rawValue.Split(",");
            foreach (string rawStat in rawStats)
                stats.Add((ChipsetEquipmentData.ConditionalStatBonus)Convert(rawStat.Replace("[", "").Replace("]", ""), typeof(ChipsetEquipmentData.ConditionalStatBonus)));

            return stats.ToArray();
        }

        else if (_targetType == typeof(AEntityPassiveEffect.PassiveEffectContainer))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            string[] vars = _rawValue.Replace(" ", "").Split(";");
            if (vars.Length < 3)
                return null;

            AEntityPassiveEffect.PassiveEffectContainer passiveEffect = new()
            {
                enumID = (EntityPassiveEffectEnumID)Convert(vars[0], typeof(EntityPassiveEffectEnumID)),
                conditionType = (Condition.ConditionType)Convert(vars[1], typeof(Condition.ConditionType)),
                targetType = (AEntityPassiveEffect.TargetType)Convert(vars[2], typeof(AEntityPassiveEffect.TargetType)),
                effectRange = vars.Length == 4 ? (Vector2Int)Convert(vars[2], typeof(Vector2Int)) : new(0,0)
            };

            return passiveEffect;
        }
        else if (_targetType == typeof(AEntityPassiveEffect.PassiveEffectContainer[]))
        {
            if (string.IsNullOrEmpty(_rawValue))
                return null;
            List<AEntityPassiveEffect.PassiveEffectContainer> stats = new();
            string[] rawStats = _rawValue.Split(",");
            foreach (string rawStat in rawStats)
                stats.Add((AEntityPassiveEffect.PassiveEffectContainer)Convert(rawStat.Replace("[", "").Replace("]", ""), typeof(AEntityPassiveEffect.PassiveEffectContainer)));

            return stats.ToArray();
        }


        #endregion

        #region Enums
        else if (_targetType == typeof(EntityActionEnumID[]))
        {
            string[] raw = _rawValue.Split(",");

            List<EntityActionEnumID> result = new();
            foreach (string item in raw)
            {
                if (!Enum.TryParse(typeof(EntityActionEnumID), item, out object rawResult))
                    Debug.Log("Missing action value: " + item);
                else
                    result.Add((EntityActionEnumID)rawResult);
            }

            return result.ToArray();
        }
        else if (_targetType == typeof(Entity.EntityState[]))
        {
            string[] raw = _rawValue.Split(",");

            List<Entity.EntityState> result = new();
            foreach (string item in raw)
            {
                result.Add((Entity.EntityState)Enum.Parse(typeof(Entity.EntityState), item));
            }

            return result.ToArray();
        }
        else if (_targetType == typeof(WeaponEquipmentData.DamageType[]))
        {
            string[] raw = _rawValue.Split(",");

            List<WeaponEquipmentData.DamageType> result = new();
            foreach (string item in raw)
            {
                result.Add((WeaponEquipmentData.DamageType)Enum.Parse(typeof(WeaponEquipmentData.DamageType), item));
            }

            return result.ToArray();
        }
        else if (_targetType.IsEnum)
            return Enum.Parse(_targetType, _rawValue);
		#endregion

		Debug.Log("Unsupported CSV type: " + _targetType.Name);
        return null;
    }

	private static bool TryGetFrameComponent( this string _input, out FrameEquipmentData _data )
	{
		string[] guids = AssetDatabase.FindAssets("t:FrameEquipmentData");

		foreach (string guid in guids)
		{
			_data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(FrameEquipmentData)) as FrameEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
		}

		_data = null;
		return false;
	}

    private static bool TryGetReactorComponent ( this string _input, out ReactorEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:ReactorEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(ReactorEquipmentData)) as ReactorEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetBrainComponent ( this string _input, out BrainEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:BrainEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(BrainEquipmentData)) as BrainEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetNeuronalMembraneComponent ( this string _input, out NeuronalMembraneEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:NeuronalMembraneEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(NeuronalMembraneEquipmentData)) as NeuronalMembraneEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetWeaponComponent ( this string _input, out WeaponEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(WeaponEquipmentData)) as WeaponEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetToolComponent ( this string _input, out ToolEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:ToolEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(ToolEquipmentData)) as ToolEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetArmorComponent ( this string _input, out ArmorEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:ArmorEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(ArmorEquipmentData)) as ArmorEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetOccultorComponent ( this string _input, out OccultorEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:OccultorEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(OccultorEquipmentData)) as OccultorEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetChipsetComponent ( this string _input, out ChipsetEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:ChipsetEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(ChipsetEquipmentData)) as ChipsetEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }

    private static bool TryGetEquipmentComponent ( this string _input, out EntityEquipmentData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:EntityEquipmentData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(EntityEquipmentData)) as EntityEquipmentData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }
    
    private static bool TryGetItemData ( this string _input, out AItemData _data )
    {
        string[] guids = AssetDatabase.FindAssets("t:AItemData");

        foreach (string guid in guids)
        {
            _data = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(AItemData)) as AItemData;

            if (_data != null && string.Equals(_data.name, _input))
                return true;
        }

        _data = null;
        return false;
    }
}