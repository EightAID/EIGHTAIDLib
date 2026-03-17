using UnityEngine;

/// <summary>
/// DontDestroyOnLoad 付きシングルトン基底クラス。
/// シーン跨ぎで維持したいマネージャー系（SoundController等）に使う。
/// </summary>
public class PersistentSingletonMonoBehaviour<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}
