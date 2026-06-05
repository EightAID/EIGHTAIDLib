#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System.Collections.Generic;

/// <summary>
/// デバッグコマンドモジュールを一括登録するための小さな補助クラスです。
/// EIGHTAIDLib は「登録の仕組み」だけを持ち、ゲーム固有のコマンドは
/// 各プロジェクトの IDebugCommandModule 実装に閉じ込めます。
/// </summary>
public static class DebugCommandModuleRegistry
{
    private static readonly HashSet<string> RegisteredModuleKeys = new HashSet<string>();

    /// <summary>
    /// 指定したモジュールを、型名をキーにして一度だけ登録します。
    /// 同じ型のモジュールを何度渡しても、Register は一回だけ実行されます。
    /// </summary>
    public static void RegisterOnce(IDebugCommandModule module)
    {
        if (module == null)
        {
            return;
        }

        string key = module.GetType().FullName;
        if (string.IsNullOrWhiteSpace(key) || !RegisteredModuleKeys.Add(key))
        {
            return;
        }

        module.Register();
    }

    /// <summary>
    /// 複数モジュールを順番に登録します。カテゴリの表示順を意識したい場合は、
    /// 呼び出し側で並び順を決めて渡してください。
    /// </summary>
    public static void RegisterAll(params IDebugCommandModule[] modules)
    {
        if (modules == null)
        {
            return;
        }

        for (int i = 0; i < modules.Length; i++)
        {
            RegisterOnce(modules[i]);
        }
    }
}
#endif
