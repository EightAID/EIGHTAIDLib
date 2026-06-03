#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System;

public sealed class DebugArgumentDefinition
{
    public string Key { get; }
    public string Label { get; }
    public DebugArgumentKind Kind { get; }
    public Type ValueType { get; }
    public object DefaultValue { get; }
    public object MinValue { get; }
    public object MaxValue { get; }
    public bool IsOptional { get; }
    public string OptionProviderId { get; }

    private DebugArgumentDefinition(
        string key,
        string label,
        DebugArgumentKind kind,
        Type valueType,
        object defaultValue,
        object minValue,
        object maxValue,
        bool isOptional,
        string optionProviderId = "")
    {
        Key = key;
        Label = label;
        Kind = kind;
        ValueType = valueType;
        DefaultValue = defaultValue;
        MinValue = minValue;
        MaxValue = maxValue;
        IsOptional = isOptional;
        OptionProviderId = optionProviderId ?? string.Empty;
    }

    public static DebugArgumentDefinition Bool(string key, string label, bool defaultValue = false, bool optional = false)
    {
        return new DebugArgumentDefinition(key, label, DebugArgumentKind.Bool, typeof(bool), defaultValue, null, null, optional);
    }

    public static DebugArgumentDefinition Int(string key, string label, int defaultValue = 0, int? min = null, int? max = null, bool optional = false)
    {
        return new DebugArgumentDefinition(key, label, DebugArgumentKind.Int, typeof(int), defaultValue, min, max, optional);
    }

    public static DebugArgumentDefinition Float(string key, string label, float defaultValue = 0f, float? min = null, float? max = null, bool optional = false)
    {
        return new DebugArgumentDefinition(key, label, DebugArgumentKind.Float, typeof(float), defaultValue, min, max, optional);
    }

    public static DebugArgumentDefinition String(string key, string label, string defaultValue = "", bool optional = false)
    {
        return new DebugArgumentDefinition(key, label, DebugArgumentKind.String, typeof(string), defaultValue ?? string.Empty, null, null, optional);
    }

    public static DebugArgumentDefinition Enum<TEnum>(string key, string label, TEnum defaultValue = default, bool optional = false) where TEnum : struct, Enum
    {
        return new DebugArgumentDefinition(key, label, DebugArgumentKind.Enum, typeof(TEnum), defaultValue, null, null, optional);
    }

    public static DebugArgumentDefinition Option(string key, string label, string providerId, string defaultValue = "", bool optional = false)
    {
        return new DebugArgumentDefinition(key, label, DebugArgumentKind.Option, typeof(string), defaultValue ?? string.Empty, null, null, optional, providerId);
    }
}
#endif
