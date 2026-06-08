#if UNITY_EDITOR || DAISHOU_TEST_BUILD
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
