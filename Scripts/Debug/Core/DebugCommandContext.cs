#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DebugCommandContext
{
    public DebugCommandContext(DebugArgumentValues args)
    {
        Args = args ?? new DebugArgumentValues();
    }

    public DebugArgumentValues Args { get; }

    public Scene ActiveScene => SceneManager.GetActiveScene();

    public T Find<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>();
    }
}
#endif
