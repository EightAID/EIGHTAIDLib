#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using UnityEngine;

public static class DebugAvailability
{
    public static bool IsEnabled
    {
        get
        {
#if UNITY_EDITOR
            return true;
#else
            BuildConfig config = BuildConfig.Instance;
            return config != null &&
                   (config.enableDebugShortcuts || config.buildType != BuildConfig.BuildType.Production);
#endif
        }
    }
}
#endif
