#if UNITY_EDITOR || DAISHOU_TEST_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// デバッグシナリオテストの中央レジストリです。
/// 汎用パネルはこのクラスだけを参照し、ゲーム固有のテスト定義やアセット配置には依存しません。
/// </summary>
public static class DebugScenarioTestRegistry
{
    private const string DefaultResourceFolder = "DebugTests";

    private static readonly Dictionary<string, DebugScenarioTestCase> TestsById =
        new Dictionary<string, DebugScenarioTestCase>(StringComparer.Ordinal);

    private static readonly Dictionary<string, Color> CategoryColors =
        new Dictionary<string, Color>(StringComparer.Ordinal);

    private static readonly HashSet<Type> RegisteredModuleTypes = new HashSet<Type>();
    private static bool _includeResourceTests = true;

    /// <summary>
    /// 同じモジュールを二重登録しないように登録します。
    /// DebugCommandModule と同じ感覚で、プロジェクト側の Bootstrap から呼び出してください。
    /// </summary>
    public static void RegisterOnce(IDebugScenarioTestModule module)
    {
        if (module == null)
        {
            return;
        }

        Type type = module.GetType();
        if (!RegisteredModuleTypes.Add(type))
        {
            return;
        }

        module.Register();
    }

    /// <summary>
    /// コードまたはScriptableObjectで作ったテストケースを登録します。
    /// 同じIDのテストが登録された場合は後勝ちです。
    /// </summary>
    public static void Register(DebugScenarioTestCase testCase)
    {
        if (testCase == null || string.IsNullOrWhiteSpace(testCase.Id))
        {
            return;
        }

        TestsById[testCase.Id] = testCase;
    }

    public static void RegisterRange(IEnumerable<DebugScenarioTestCase> testCases)
    {
        if (testCases == null)
        {
            return;
        }

        foreach (DebugScenarioTestCase testCase in testCases)
        {
            Register(testCase);
        }
    }

    /// <summary>
    /// Resources/DebugTests 配下の ScriptableObject テストを読み込むかどうかを指定します。
    /// コード登録だけで運用したいプロジェクトは false にできます。
    /// </summary>
    public static void SetIncludeResourceTests(bool include)
    {
        _includeResourceTests = include;
    }

    /// <summary>
    /// カテゴリ名に対応する色を登録します。
    /// 完全一致に加えて、カテゴリ文字列にキーが含まれる場合も利用されます。
    /// </summary>
    public static void SetCategoryColor(string categoryKey, Color color)
    {
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            return;
        }

        CategoryColors[categoryKey] = color;
    }

    public static Color ResolveCategoryColor(string category, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return fallback;
        }

        if (CategoryColors.TryGetValue(category, out Color exact))
        {
            return exact;
        }

        foreach (KeyValuePair<string, Color> pair in CategoryColors)
        {
            if (category.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return pair.Value;
            }
        }

        return fallback;
    }

    public static IReadOnlyList<DebugScenarioTestCase> GetTests()
    {
        IEnumerable<DebugScenarioTestCase> tests = TestsById.Values;
        if (_includeResourceTests)
        {
            tests = tests.Concat(Resources.LoadAll<DebugScenarioTestCase>(DefaultResourceFolder));
        }

        return tests
            .Where(test => test != null)
            .GroupBy(test => test.Id, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(test => test.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(test => test.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Play中の再登録やテスト開発時に使うキャッシュクリアです。
    /// 登録済みモジュール情報も消すため、次のBootstrapで再登録されます。
    /// </summary>
    public static void Clear()
    {
        TestsById.Clear();
        CategoryColors.Clear();
        RegisteredModuleTypes.Clear();
        _includeResourceTests = true;
    }
}
#endif
