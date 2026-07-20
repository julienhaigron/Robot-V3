using System;

public static class CsvTypeConverter
{
    public static object Convert ( string value, Type targetType )
    {
        if (targetType == typeof(string))
            return value;

        if (targetType == typeof(int))
            return int.Parse(value);

        if (targetType == typeof(float))
            return float.Parse(value);

        if (targetType == typeof(bool))
            return bool.Parse(value);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value);

        throw new Exception(
            $"Unsupported CSV type : {targetType.Name}");
    }
}