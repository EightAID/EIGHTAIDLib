#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System.Collections.Generic;

public interface IDebugOptionProvider
{
    string ProviderId { get; }
    IReadOnlyList<DebugOption> GetOptions(DebugCommandContext context);
}
#endif
