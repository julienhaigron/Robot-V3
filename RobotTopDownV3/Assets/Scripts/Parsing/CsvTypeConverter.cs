using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CsvTypeConverter
{
    public static object Convert ( string value, Type targetType )
    {
        if (targetType == typeof(string))
            return value;
        else if (targetType == typeof(int))
            return int.Parse(value);
        else if (targetType == typeof(float))
            return float.Parse(value);
        else if (targetType == typeof(bool))
            return bool.Parse(value);

        else if(targetType == typeof(EntityActionEnumID[]))
		{
            string[] raw = value.Split(",");

            List<EntityActionEnumID> result = new();
            foreach (string item in raw)
            {
                result.Add((EntityActionEnumID) Enum.Parse(typeof(EntityActionEnumID), item));
            }

            return result.ToArray();
        }
        else if(targetType == typeof(Entity.EntityState[]))
		{
            string[] raw = value.Split(",");

            List<Entity.EntityState> result = new();
            foreach (string item in raw)
            {
                result.Add((Entity.EntityState) Enum.Parse(typeof(Entity.EntityState), item));
            }

            return result.ToArray();
        }
        else if (targetType.IsEnum)
            return Enum.Parse(targetType, value);

        Debug.Log("Unsupported CSV type" + targetType.Name);
        return null;
    }


    /*private static bool TryGetStructureUpgrade ( this string _input, out StructureUpgrade _upgrade )
    {
        string[] guids = AssetDatabase.FindAssets("t:StructureUpgrade");

        foreach (string guid in guids)
        {
            _upgrade = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(StructureUpgrade)) as StructureUpgrade;

            if (_upgrade.saveKey == _input)
                return true;
        }

        _upgrade = null;
        return false;
    }*/
}