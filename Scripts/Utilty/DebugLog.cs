using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    public static class DebugLog
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Log(string message) => Debug.Log(message);

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Warning(string message) => Debug.LogWarning(message);

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Error(string message) => Debug.LogError(message);
    }
}
