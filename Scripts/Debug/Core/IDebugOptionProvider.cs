#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System.Collections.Generic;

public interface IDebugOptionProvider
{
    string ProviderId { get; }
    IReadOnlyList<DebugOption> GetOptions(DebugCommandContext context);
}
#endif
