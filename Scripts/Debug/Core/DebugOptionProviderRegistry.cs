#if UNITY_EDITOR || DAISHOU_TEST_BUILD
using System;
using System.Collections.Generic;

public static class DebugOptionProviderRegistry
{
    private static readonly Dictionary<string, IDebugOptionProvider> Providers = new Dictionary<string, IDebugOptionProvider>(StringComparer.Ordinal);

    public static void Register(IDebugOptionProvider provider)
    {
        if (provider == null || string.IsNullOrWhiteSpace(provider.ProviderId))
        {
            return;
        }

        Providers[provider.ProviderId] = provider;
    }

    public static IReadOnlyList<DebugOption> GetOptions(string providerId, DebugCommandContext context)
    {
        if (string.IsNullOrWhiteSpace(providerId) || !Providers.TryGetValue(providerId, out IDebugOptionProvider provider))
        {
            return Array.Empty<DebugOption>();
        }

        return provider.GetOptions(context) ?? Array.Empty<DebugOption>();
    }
}
#endif
