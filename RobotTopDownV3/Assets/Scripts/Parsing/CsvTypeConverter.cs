using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CsvTypeConverter
{
    public static object Convert ( string value, Type targetType )
    {
		#region Base Vars
		if (string.Equals(value, "-"))
            return null;
        else if (targetType == typeof(string))
            return value;
        else if (targetType == typeof(int))
            return int.Parse(value);
        else if (targetType == typeof(float))
            return float.Parse(value);
        else if (targetType == typeof(bool))
            return bool.Parse(value);
        #endregion

        #region Scriptables
        else if (targetType == typeof(FrameEquipmentData))
		{
            if (TryGetFrameComponent(value, out FrameEquipmentData data))
                return data;
            else
                return null;
		}
        else if (targetType == typeof(ReactorEquipmentData))
        {
            if (TryGetReactorComponent(value, out ReactorEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(BrainEquipmentData))
        {
            if (TryGetBrainComponent(value, out BrainEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(NeuronalMembraneEquipmentData))
        {
            if (TryGetNeuronalMembraneComponent(value, out NeuronalMembraneEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(WeaponEquipmentData))
        {
            if (TryGetWeaponComponent(value, out WeaponEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(ToolEquipmentData))
        {
            if (TryGetToolComponent(value, out ToolEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(ArmorEquipmentData))
        {
            if (TryGetArmorComponent(value, out ArmorEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(OccultorEquipmentData))
        {
            if (TryGetOccultorComponent(value, out OccultorEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(ChipsetEquipmentData))
        {
            if (TryGetChipsetComponent(value, out ChipsetEquipmentData data))
                return data;
            else
                return null;
        }
        else if (targetType == typeof(EntityEquipmentData[]))
        {
            List<EntityEquipmentData> equipments = new();
            string[] rawValues = value.Replace(" ", "").Split(",");
            foreach(string raw in rawValues)
			{
                if (TryGetEquipmentComponent(value, out EntityEquipmentData data))
                    equipments.Add(data);
            }
            return equipments.ToArray();
        }
		#endregion

		#region Enums
		else if (targetType == typeof(EntityActionEnumID[]))
        {
            string[] raw = value.Split(",");

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
        else if (targetType == typeof(Entity.EntityState[]))
        {
            string[] raw = value.Split(",");

            List<Entity.EntityState> result = new();
            foreach (string item in raw)
            {
                result.Add((Entity.EntityState)Enum.Parse(typeof(Entity.EntityState), item));
            }

            return result.ToArray();
        }
        else if (targetType.IsEnum)
            return Enum.Parse(targetType, value);
		#endregion

		Debug.Log("Unsupported CSV type: " + targetType.Name);
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
        string[] guids = AssetDatabase.FindAssets("t:BrainEquipmentData");

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
}