using UnityEngine;

/// <summary>
/// シングルトン設計をする基底クラス
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            // すでに存在していればそのインスタンスを返す
            if (!_instance)
            {
                // インスタンスが存在しない場合は自動生成する
                var singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (!_instance)//インスタンスがないなら
        {
            _instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
