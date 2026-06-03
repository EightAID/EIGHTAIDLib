#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
public sealed class DebugOption
{
    public DebugOption(string id, string label, string description = "")
    {
        Id = id ?? string.Empty;
        Label = string.IsNullOrWhiteSpace(label) ? Id : label;
        Description = description ?? string.Empty;
    }

    public string Id { get; }
    public string Label { get; }
    public string Description { get; }

    public string DisplayText => string.IsNullOrWhiteSpace(Description)
        ? Label
        : $"{Label} - {Description}";
}
#endif
