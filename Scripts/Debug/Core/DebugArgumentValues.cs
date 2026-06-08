#if UNITY_EDITOR || DAISHOU_TEST_BUILD
using System;
using System.Collections.Generic;

public sealed class DebugArgumentValues
{
    private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.Ordinal);

    public void Set(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _values[key] = value;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (_values.TryGetValue(key, out object raw))
        {
            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            try
            {
                if (typeof(T).IsEnum && raw is string enumString)
                {
                    value = (T)Enum.Parse(typeof(T), enumString);
                    return true;
                }

                value = (T)Convert.ChangeType(raw, typeof(T));
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public T Get<T>(string key)
    {
        return TryGet(key, out T value) ? value : default;
    }

    public string GetString(string key)
    {
        return TryGet(key, out string value) ? value : string.Empty;
    }
}
#endif
