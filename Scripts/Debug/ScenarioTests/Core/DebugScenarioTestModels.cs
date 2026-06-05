#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebugScenarioTestStepKind
{
    Command,
    Assert,
    Wait,
    Note,
}

public enum DebugScenarioTestStatus
{
    NotRun,
    Running,
    Passed,
    Failed,
    Error,
    Skipped,
}

public enum DebugScenarioArgumentKind
{
    String,
    Int,
    Float,
    Bool,
}

/// <summary>
/// テストステップからデバッグコマンドへ渡す引数です。
/// 値はInspectorやコード登録で扱いやすいよう文字列で保持し、実行直前に型変換します。
/// </summary>
[Serializable]
public sealed class DebugScenarioArgument
{
    [SerializeField] private string key;
    [SerializeField] private DebugScenarioArgumentKind kind = DebugScenarioArgumentKind.String;
    [SerializeField] private string value;

    public string Key
    {
        get => key;
        set => key = value;
    }

    public DebugScenarioArgumentKind Kind
    {
        get => kind;
        set => kind = value;
    }

    public string Value
    {
        get => value;
        set => this.value = value;
    }

    public object ToTypedValue()
    {
        switch (kind)
        {
            case DebugScenarioArgumentKind.Int:
                return int.TryParse(value, out int intValue) ? intValue : 0;
            case DebugScenarioArgumentKind.Float:
                return float.TryParse(value, out float floatValue) ? floatValue : 0f;
            case DebugScenarioArgumentKind.Bool:
                return bool.TryParse(value, out bool boolValue) && boolValue;
            default:
                return value ?? string.Empty;
        }
    }
}

/// <summary>
/// シナリオテスト内の1手順です。
/// commandId は DebugCommandRegistry に登録されたコマンドIDを指し、テスト基盤はゲーム固有処理を直接呼びません。
/// </summary>
[Serializable]
public sealed class DebugScenarioTestStep
{
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private DebugScenarioTestStepKind kind = DebugScenarioTestStepKind.Command;
    [SerializeField] private string commandId;
    [SerializeField] private List<DebugScenarioArgument> arguments = new List<DebugScenarioArgument>();
    [SerializeField] private float timeoutSeconds = 5f;
    [SerializeField] private bool stopOnFailure = true;
    [SerializeField] private string expectedSummary;

    public string DisplayName
    {
        get => displayName;
        set => displayName = value;
    }

    public string Description
    {
        get => description;
        set => description = value;
    }

    public DebugScenarioTestStepKind Kind
    {
        get => kind;
        set => kind = value;
    }

    public string CommandId
    {
        get => commandId;
        set => commandId = value;
    }

    public List<DebugScenarioArgument> Arguments => arguments;

    public float TimeoutSeconds
    {
        get => timeoutSeconds;
        set => timeoutSeconds = value;
    }

    public bool StopOnFailure
    {
        get => stopOnFailure;
        set => stopOnFailure = value;
    }

    public string ExpectedSummary
    {
        get => expectedSummary;
        set => expectedSummary = value;
    }
}

/// <summary>
/// デバッグシナリオテストの定義アセットです。
/// コードから ScriptableObject.CreateInstance で作って登録することも、Resources/DebugTests 配下の asset として置くこともできます。
/// </summary>
[CreateAssetMenu(menuName = "Debug/Scenario Test Case", fileName = "DebugScenarioTestCase")]
public sealed class DebugScenarioTestCase : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string category;
    [TextArea(2, 4)]
    [SerializeField] private string summary;
    [TextArea(2, 5)]
    [SerializeField] private string purpose;
    [TextArea(2, 5)]
    [SerializeField] private string preconditions;
    [TextArea(2, 5)]
    [SerializeField] private string expectedResult;
    [TextArea(2, 5)]
    [SerializeField] private string failureInvestigationHint;
    [SerializeField] private string requiredScene;
    [SerializeField] private List<DebugScenarioTestStep> steps = new List<DebugScenarioTestStep>();

    public string Id { get => id; set => id = value; }
    public string DisplayName { get => displayName; set => displayName = value; }
    public string Category { get => category; set => category = value; }
    public string Summary { get => summary; set => summary = value; }
    public string Purpose { get => purpose; set => purpose = value; }
    public string Preconditions { get => preconditions; set => preconditions = value; }
    public string ExpectedResult { get => expectedResult; set => expectedResult = value; }
    public string FailureInvestigationHint { get => failureInvestigationHint; set => failureInvestigationHint = value; }
    public string RequiredScene { get => requiredScene; set => requiredScene = value; }
    public List<DebugScenarioTestStep> Steps => steps;
}

/// <summary>
/// 1ステップ分の実行結果です。UIはこの情報だけを見て成功/失敗表示を作ります。
/// </summary>
public sealed class DebugScenarioTestStepResult
{
    public DebugScenarioTestStatus Status { get; set; }
    public string StepName { get; set; }
    public string CommandId { get; set; }
    public string Message { get; set; }
    public string Expected { get; set; }
    public string Actual { get; set; }
    public float DurationSeconds { get; set; }
}

/// <summary>
/// 1テストケース分の実行結果です。
/// 失敗時は Message に「どこで止まったか」を入れ、詳細は StepResults に残します。
/// </summary>
public sealed class DebugScenarioTestRunResult
{
    public DebugScenarioTestStatus Status { get; set; } = DebugScenarioTestStatus.NotRun;
    public string TestId { get; set; }
    public string TestName { get; set; }
    public string Message { get; set; }
    public float DurationSeconds { get; set; }
    public List<DebugScenarioTestStepResult> StepResults { get; } = new List<DebugScenarioTestStepResult>();
}
#endif
