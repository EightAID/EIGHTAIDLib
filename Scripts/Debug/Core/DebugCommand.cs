#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public sealed class DebugCommand
{
    private readonly Func<DebugCommandContext, UniTask<DebugCommandResult>> _executeAsync;

    private DebugCommand(
        string id,
        string label,
        string category,
        string description,
        IReadOnlyList<DebugArgumentDefinition> arguments,
        bool closePanelAfterExecute,
        Func<DebugCommandContext, bool> isVisible,
        Func<DebugCommandContext, string> unavailableReason,
        Func<DebugCommandContext, UniTask<DebugCommandResult>> executeAsync)
    {
        Id = id;
        Label = label;
        Category = category;
        Description = description;
        Arguments = arguments;
        ClosePanelAfterExecute = closePanelAfterExecute;
        IsVisiblePredicate = isVisible;
        UnavailableReasonProvider = unavailableReason;
        _executeAsync = executeAsync;
    }

    public string Id { get; }
    public string Label { get; }
    public string Category { get; }
    public string Description { get; }
    public IReadOnlyList<DebugArgumentDefinition> Arguments { get; }
    public bool ClosePanelAfterExecute { get; }
    public Func<DebugCommandContext, bool> IsVisiblePredicate { get; }
    public Func<DebugCommandContext, string> UnavailableReasonProvider { get; }

    public bool IsVisible(DebugCommandContext context)
    {
        return IsVisiblePredicate == null || IsVisiblePredicate(context);
    }

    public string GetUnavailableReason(DebugCommandContext context)
    {
        return UnavailableReasonProvider?.Invoke(context) ?? string.Empty;
    }

    public bool IsAvailable(DebugCommandContext context)
    {
        return string.IsNullOrWhiteSpace(GetUnavailableReason(context));
    }

    public UniTask<DebugCommandResult> ExecuteAsync(DebugCommandContext context)
    {
        if (_executeAsync == null)
        {
            return UniTask.FromResult(DebugCommandResult.Failure("Command has no executor."));
        }

        string unavailableReason = GetUnavailableReason(context);
        if (!string.IsNullOrWhiteSpace(unavailableReason))
        {
            return UniTask.FromResult(DebugCommandResult.Failure(unavailableReason));
        }

        return _executeAsync(context);
    }

    public static Builder Create(string id, string label)
    {
        return new Builder(id, label);
    }

    /// <summary>
    /// RuntimeDebugPanel に表示するコマンドを組み立てるための Builder です。
    /// 引数定義、カテゴリ、実行可否、実行処理を宣言的につなげて登録できます。
    /// </summary>
    public sealed class Builder
    {
        private readonly string _id;
        private readonly string _label;
        private readonly List<DebugArgumentDefinition> _arguments = new List<DebugArgumentDefinition>();
        private string _category = "General";
        private string _description = string.Empty;
        private bool _closePanelAfterExecute = true;
        private Func<DebugCommandContext, bool> _isVisible;
        private Func<DebugCommandContext, string> _unavailableReason;
        private Func<DebugCommandContext, UniTask<DebugCommandResult>> _executeAsync;

        public Builder(string id, string label)
        {
            _id = id;
            _label = label;
        }

        public Builder Category(string category)
        {
            _category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            return this;
        }

        public Builder Description(string description)
        {
            _description = description ?? string.Empty;
            return this;
        }

        public Builder ClosePanelAfterExecute(bool close)
        {
            _closePanelAfterExecute = close;
            return this;
        }

        public Builder VisibleWhen(Func<DebugCommandContext, bool> predicate)
        {
            _isVisible = predicate;
            return this;
        }

        public Builder UnavailableWhen(Func<DebugCommandContext, string> reasonProvider)
        {
            _unavailableReason = reasonProvider;
            return this;
        }

        public Builder ArgBool(string key, string label, bool defaultValue = false, bool optional = false)
        {
            _arguments.Add(DebugArgumentDefinition.Bool(key, label, defaultValue, optional));
            return this;
        }

        public Builder ArgInt(string key, string label, int defaultValue = 0, int? min = null, int? max = null, bool optional = false)
        {
            _arguments.Add(DebugArgumentDefinition.Int(key, label, defaultValue, min, max, optional));
            return this;
        }

        public Builder ArgFloat(string key, string label, float defaultValue = 0f, float? min = null, float? max = null, bool optional = false)
        {
            _arguments.Add(DebugArgumentDefinition.Float(key, label, defaultValue, min, max, optional));
            return this;
        }

        public Builder ArgString(string key, string label, string defaultValue = "", bool optional = false)
        {
            _arguments.Add(DebugArgumentDefinition.String(key, label, defaultValue, optional));
            return this;
        }

        public Builder ArgEnum<TEnum>(string key, string label, TEnum defaultValue = default, bool optional = false) where TEnum : struct, Enum
        {
            _arguments.Add(DebugArgumentDefinition.Enum(key, label, defaultValue, optional));
            return this;
        }

        public Builder ArgOption(string key, string label, string providerId, string defaultValue = "", bool optional = false)
        {
            _arguments.Add(DebugArgumentDefinition.Option(key, label, providerId, defaultValue, optional));
            return this;
        }

        public Builder Run(Func<DebugCommandContext, DebugCommandResult> execute)
        {
            _executeAsync = context => UniTask.FromResult(execute(context));
            return this;
        }

        public Builder Run(Action<DebugCommandContext> execute)
        {
            _executeAsync = context =>
            {
                execute(context);
                return UniTask.FromResult(DebugCommandResult.Success());
            };
            return this;
        }

        public Builder RunAsync(Func<DebugCommandContext, UniTask<DebugCommandResult>> executeAsync)
        {
            _executeAsync = executeAsync;
            return this;
        }

        public DebugCommand Build()
        {
            return new DebugCommand(
                _id,
                _label,
                _category,
                _description,
                _arguments.ToArray(),
                _closePanelAfterExecute,
                _isVisible,
                _unavailableReason,
                _executeAsync);
        }

        public static implicit operator DebugCommand(Builder builder)
        {
            return builder.Build();
        }
    }
}
#endif
