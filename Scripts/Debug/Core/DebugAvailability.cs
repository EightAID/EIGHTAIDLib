#if UNITY_EDITOR || DAISHOU_TEST_BUILD
using UnityEngine;

public static class DebugAvailability
{
    public static bool IsEnabled
    {
        get
        {
#if UNITY_EDITOR
            return true;
#elif DAISHOU_TEST_BUILD
            BuildConfig config = BuildConfig.Instance;
            return config != null &&
                   config.enableDebugShortcuts &&
                   config.buildType == BuildConfig.BuildType.Test;
#else
            return false;
#endif
        }
    }
}
#endif
