using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ImportedData
{
	public string sheetName;
	public Dictionary<string, string> data { get; private set; }

	public ImportedData ( Dictionary<string, string> _data, string _sheetName )
	{
		data = _data;
		sheetName = _sheetName;
	}

#if UNITY_EDITOR

	public T GetValue<T> ( string _id )
	{
		T value = default;
		if (data.TryGetValue(_id, out string raw))
		{
			object converted = CsvTypeConverter.Convert(raw, typeof(T));
			value = (T)converted;
		}

		return value;
	}

	public bool TryGetValue<T> ( string _id, out T _data )
	{
		_data = GetValue<T>(_id);
		return _data != null;
	}

	/*public T[] GetValues<T> ( string _id )
	{
		List<T> data = new();
		if (TryGetValues(_id, out string[] _raw))
		{
			foreach (string item in _raw)
			{
				object converted = CsvTypeConverter.Convert(item, data.GetType());
				data.Add((T)converted);
			}
		}

		return data.ToArray();
	}*/
#endif
}