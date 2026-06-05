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
            foreach (DebugScenarioTestStep step in testCase.Steps)
            {
                DebugScenarioTestStepResult stepResult = await RunStepAsync(step);
                runResult.StepResults.Add(stepResult);

                if ((stepResult.Status == DebugScenarioTestStatus.Failed ||
                     stepResult.Status == DebugScenarioTestStatus.Error) &&
                    (step == null || step.StopOnFailure))
                {
                    runResult.Status = stepResult.Status;
                    runResult.Message = stepResult.Message;
                    runResult.DurationSeconds = Time.realtimeSinceStartup - startedAt;
                    return runResult;
                }
            }

            runResult.Status = DebugScenarioTestStatus.Passed;
            runResult.Message = "すべてのステップが成功しました。";
        }
        catch (Exception ex)
        {
            runResult.Status = DebugScenarioTestStatus.Error;
            runResult.Message = ex.Message;
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
            result.Message = $"コマンドが見つかりません: {step.CommandId}";
            result.DurationSeconds = Time.realtimeSinceStartup - startedAt;
            return result;
        }

        DebugArgumentValues args = BuildArguments(step.Arguments);
        DebugCommandResult commandResult = await command.ExecuteAsync(new DebugCommandContext(args));
        result.Status = commandResult.IsSuccess ? DebugScenarioTestStatus.Passed : DebugScenarioTestStatus.Failed;
        result.Message = string.IsNullOrWhiteSpace(commandResult.Message)
            ? commandResult.IsSuccess ? "成功しました。" : "失敗しました。"
            : commandResult.Message;
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
}
#endif
