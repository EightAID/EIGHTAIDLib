#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
/// <summary>
/// プロジェクト固有のシナリオテストを登録するためのモジュールです。
/// EIGHTAIDLib 側はゲーム固有クラスを知らないため、各プロジェクトはこの口を通して
/// DebugScenarioTestRegistry へテストケースやカテゴリ色を登録します。
/// </summary>
public interface IDebugScenarioTestModule
{
    void Register();
}
#endif
