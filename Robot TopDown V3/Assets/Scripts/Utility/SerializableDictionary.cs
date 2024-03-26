using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
	[SerializeField]
	private List<TKey> keys = new List<TKey>();

	[SerializeField]
	private List<TValue> values = new List<TValue>();

	public SerializableDictionary ()
	{
	}

	public SerializableDictionary ( SerializableDictionary<TKey, TValue> _copy )
	{
		foreach (KeyValuePair<TKey, TValue> kvp in _copy)
		{
			Add(kvp.Key, kvp.Value);
		}
	}

	// save the dictionary to lists
	public void OnBeforeSerialize ()
	{
		keys.Clear();
		values.Clear();
		foreach (KeyValuePair<TKey, TValue> pair in this)
		{
			keys.Add(pair.Key);
			values.Add(pair.Value);
		}
	}

	// load dictionary from lists
	public void OnAfterDeserialize ()
	{
		this.Clear();

		if (keys.Count != values.Count)
		{
			throw new System.Exception(string.Format(this.GetType().Name + ": {0} keys and {1} values after deserialization. Something may not be serializable. ({2}, {3})", keys.Count, values.Count, typeof(TKey).ToString(), typeof(TValue).ToString()));
		}

		for (int i = 0; i < keys.Count; i++)
			this.Add(keys[i], values[i]);
	}
}
