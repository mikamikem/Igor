#if UNITY_4_5 || UNITY_4_6 || UNITY_5_0
#define ISERIALIZATION_DEFINED
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SerializableDictionary<T, U>
#if ISERIALIZATION_DEFINED
    : ISerializationCallbackReceiver
#endif
{
    Dictionary<T,U>  _dictionary = new Dictionary<T,U>();
    
#if ISERIALIZATION_DEFINED
    List<T> _keys = new List<T>();
    List<U> _values = new List<U>();

	public void OnBeforeSerialize()
	{
		_keys.Clear();
		_values.Clear();
		foreach(var kvp in _dictionary)
		{
			_keys.Add(kvp.Key);
			_values.Add(kvp.Value);
		}
	}

    public void OnAfterDeserialize()
    {
        _dictionary = new Dictionary<T, U>();
        for(int i = 0; i != Mathf.Min(_keys.Count, _values.Count); i++)
            _dictionary.Add(_keys[i], _values[i]);
    }
#endif

    public U this[T t]
    {
        get { return _dictionary[t]; }
        set { _dictionary[t] = value; }
    }

    public bool ContainsKey(T t) { return _dictionary.ContainsKey(t); }

    public bool ContainsValue(U u) { return _dictionary.ContainsValue(u); }

    public void Add(T inKey, U inValue)
    {
        if(!_dictionary.ContainsKey(inKey))
            _dictionary.Add(inKey, inValue);
    }
}
