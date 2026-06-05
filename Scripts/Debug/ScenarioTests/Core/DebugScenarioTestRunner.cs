#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class DebugScenarioTestRunner
{
    /// <summary>
    /// テストケースを先頭から順に実行します。
    /// ステップが失敗し stopOnFailure が true の場合、その時点でテストケース全体を失敗として終了します。
    /// </summary>
    public async UniTask<DebugScenarioTestRunResult> RunAsync(DebugScenarioTestCase testCase)
    {
        var runResult = new DebugScenarioTestRunResult
        {
            TestId = testCase != null ? testCase.Id : string.Empty,
            TestName = testCase != null ? testCase.DisplayName : string.Empty,
            Status = DebugScenarioTestStatus.Running,
        };

        if (testCase == null)
        {
            runResult.Status = DebugScenarioTestStatus.Error;
            runResult.Message = "テストケースが指定されていません。";
            return runResult;
        }

        float startedAt = Time.realtimeSinceStartup;
        try
        {
            for (int i = 0; i < testCase.Steps.Count; i++)
            {
                DebugScenarioTestStep step = testCase.Steps[i];
                DebugScenarioTestStepResult stepResult = await RunStepAsync(step);
                runResult.StepResults.Add(stepResult);

                if ((stepResult.Status == DebugScenarioTestStatus.Failed ||
                     stepResult.Status == DebugScenarioTestStatus.Error) &&
                    (step == null || step.StopOnFailure))
                {
                    runResult.Status = stepResult.Status;
                    runResult.FailureKind = stepResult.FailureKind;
                    runResult.FailedStepIndex = i;
                    runResult.Message = stepResult.Message;
                    runResult.FailureInstruction = BuildFailureInstruction(testCase, step, stepResult, i);
                    runResult.DurationSeconds = Time.realtimeSinceStartup - startedAt;
                    return runResult;
                }
            }

            runResult.Status = DebugScenarioTestStatus.Passed;
            runResult.FailureKind = DebugScenarioTestFailureKind.None;
            runResult.FailedStepIndex = -1;
            runResult.Message = "すべてのステップが成功しました。";
        }
        catch (Exception ex)
        {
            runResult.Status = DebugScenarioTestStatus.Error;
            runResult.FailureKind = DebugScenarioTestFailureKind.Exception;
            runResult.Message = ex.Message;
            runResult.FailureInstruction = $"テスト実行中に例外が発生しました。TestId={runResult.TestId}, Error={ex.Message}";
        }

        runResult.DurationSeconds = Time.realtimeSinceStartup - startedAt;
        return runResult;
    }

    private static async UniTask<DebugScenarioTestStepResult> RunStepAsync(DebugScenarioTestStep step)
    {
        float startedAt = Time.realtimeSinceStartup;
        var result = new DebugScenarioTestStepResult
        {
            Status = DebugScenarioTestStatus.Running,
            StepName = step != null ? step.DisplayName : string.Empty,
            CommandId = step != null ? step.CommandId : string.Empty,
            Expected = step != null ? step.ExpectedSummary : string.Empty,
        };

        if (step == null)
        {
            result.Status = DebugScenarioTestStatus.Error;
            result.FailureKind = DebugScenarioTestFailureKind.TestCaseDefinition;
            result.Message = "空のステップです。";
            return result;
        }

        if (step.Kind == DebugScenarioTestStepKind.Note)
        {
            result.Status = DebugScenarioTestStatus.Passed;
            result.Message = string.IsNullOrWhiteSpace(step.Description) ? "メモを確認しました。" : step.Description;
            result.DurationSeconds = Time.realtimeSinceStartup - startedAt;
            return result;
        }

        // 実際の状態変更や検証はすべて DebugCommand 経由で行います。
        // これにより、テスト基盤は特定ゲームの Controller や SaveData に依存しません。
        if (!DebugCommandRegistry.TryGetCommand(step.CommandId, out DebugCommand command))
        {
            result.Status = DebugScenarioTestStatus.Error;
            result.FailureKind = DebugScenarioTestFailureKind.Infrastructure;
            result.Message = $"コマンドが見つかりません: {step.CommandId}";
            result.DurationSeconds = Time.realtimeSinceStartup - startedAt;
            return result;
        }

        DebugArgumentValues args = BuildArguments(step.Arguments);
        DebugCommandResult commandResult = await command.ExecuteAsync(new DebugCommandContext(args));
        result.Status = commandResult.IsSuccess ? DebugScenarioTestStatus.Passed : DebugScenarioTestStatus.Failed;
        result.FailureKind = commandResult.IsSuccess
            ? DebugScenarioTestFailureKind.None
            : ClassifyFailure(step, commandResult.Message);
        result.Message = string.IsNullOrWhiteSpace(commandResult.Message)
            ? commandResult.IsSuccess ? "成功しました。" : "失敗しました。"
            : commandResult.Message;
        ExtractExpectedActual(result.Message, out string expected, out string actual);
        if (!string.IsNullOrWhiteSpace(expected))
        {
            result.Expected = expected;
        }

        result.Actual = actual;
        result.DurationSeconds = Time.realtimeSinceStartup - startedAt;
        return result;
    }

    private static DebugArgumentValues BuildArguments(IEnumerable<DebugScenarioArgument> arguments)
    {
        var values = new DebugArgumentValues();
        if (arguments == null)
        {
            return values;
        }

        foreach (DebugScenarioArgument argument in arguments)
        {
            if (argument == null || string.IsNullOrWhiteSpace(argument.Key))
            {
                continue;
            }

            values.Set(argument.Key, argument.ToTypedValue());
        }

        return values;
    }

    private static DebugScenarioTestFailureKind ClassifyFailure(DebugScenarioTestStep step, string message)
    {
        if (step == null || string.IsNullOrWhiteSpace(step.CommandId))
        {
            return DebugScenarioTestFailureKind.TestCaseDefinition;
        }

        if (!string.IsNullOrWhiteSpace(message) &&
            (message.Contains("見つかりません") || message.Contains("読み込めません") || message.Contains("ありません")))
        {
            return DebugScenarioTestFailureKind.Precondition;
        }

        return step.Kind == DebugScenarioTestStepKind.Assert || step.CommandId.StartsWith("assert.", StringComparison.Ordinal)
            ? DebugScenarioTestFailureKind.Assertion
            : DebugScenarioTestFailureKind.Precondition;
    }

    private static void ExtractExpectedActual(string message, out string expected, out string actual)
    {
        expected = string.Empty;
        actual = string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        const string expectedKey = "Expected:";
        const string actualKey = "Actual:";
        int expectedIndex = message.IndexOf(expectedKey, StringComparison.OrdinalIgnoreCase);
        int actualIndex = message.IndexOf(actualKey, StringComparison.OrdinalIgnoreCase);
        if (expectedIndex >= 0)
        {
            int start = expectedIndex + expectedKey.Length;
            int end = actualIndex > start ? actualIndex : message.Length;
            expected = message.Substring(start, end - start).Trim(' ', ',', '。');
        }

        if (actualIndex >= 0)
        {
            int start = actualIndex + actualKey.Length;
            actual = message.Substring(start).Trim();
        }
    }

    private static string BuildFailureInstruction(DebugScenarioTestCase testCase, DebugScenarioTestStep step, DebugScenarioTestStepResult result, int stepIndex)
    {
        string failureKind = result.FailureKind.ToString();
        string expected = string.IsNullOrWhiteSpace(result.Expected)
            ? step != null ? step.ExpectedSummary : string.Empty
            : result.Expected;
        string stepName = step != null ? step.DisplayName : "<null>";
        string commandId = step != null ? step.CommandId : string.Empty;
        string actual = string.IsNullOrWhiteSpace(result.Actual) ? "未取得" : result.Actual;
        return
            "以下のデバッグシナリオテストが失敗しています。実装不具合だけでなく、テストケースの前提・期待値・対象シーンが正しいかも確認してください。\n" +
            $"TestId: {testCase.Id}\n" +
            $"TestName: {testCase.DisplayName}\n" +
            $"Category: {testCase.Category}\n" +
            $"FailureKind: {failureKind}\n" +
            $"FailedStep: {stepIndex + 1}. {stepName}\n" +
            $"Command: {commandId}\n" +
            $"Expected: {expected}\n" +
            $"Actual: {actual}\n" +
            $"Message: {result.Message}\n" +
            $"TestExpectedResult: {testCase.ExpectedResult}\n" +
            $"Preconditions: {testCase.Preconditions}\n" +
            $"Hint: {testCase.FailureInvestigationHint}";
    }
}
#endif
