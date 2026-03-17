using UnityEngine;

public static class DebugLog
{
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(string message)
    {
        Debug.Log(message);
    }
}
