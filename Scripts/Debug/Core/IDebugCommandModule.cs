#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
/// <summary>
/// デバッグコマンドのまとまりを表す登録単位です。
/// プロジェクト側は機能カテゴリごとにこのインターフェイスを実装し、
/// 起動時に DebugCommandModuleRegistry へ渡すだけでコマンドを追加できます。
/// </summary>
public interface IDebugCommandModule
{
    /// <summary>
    /// モジュール内のコマンドと、必要な OptionProvider を登録します。
    /// このメソッドは複数回呼ばれても安全な実装にしてください。
    /// </summary>
    void Register();
}
#endif
