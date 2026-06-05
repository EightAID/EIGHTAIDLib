# Debug Scenario Test Panel 仕様

`DebugScenarioTestPanel` は、デバッグコマンドを順番に実行してゲーム状態を検証するための汎用テストパネルです。

この基盤は `EIGHTAIDLib` 側に置き、各ゲームプロジェクトは「テストケース」と「テストで使うデバッグコマンド」だけを登録します。

## 目的

- デバッグコマンドを組み合わせてシナリオテストを作る
- テストを日本語の説明つきで一覧表示する
- 単体実行と一括実行を行う
- 成功、失敗、エラーをパネル上で分かりやすく表示する
- ゲーム固有の Controller や SaveData に汎用UIを依存させない

## 配置

汎用基盤:

```text
Assets/Scripts/EIGHTAIDLib/Scripts/Debug/ScenarioTests/Core/
Assets/Scripts/EIGHTAIDLib/Scripts/Debug/ScenarioTests/UI/
```

プロジェクト固有実装:

```text
Assets/Scripts/Debug/Commands/
Assets/Scripts/Debug/Tests/
```

## 基本構造

```text
DebugScenarioTestCase
  テストケース本体。表示名、説明、前提条件、期待結果、ステップを持つ。

DebugScenarioTestStep
  1つの手順。commandId と arguments を持ち、DebugCommandRegistry 経由で実行される。

DebugScenarioTestRunner
  テストケースを上から順に実行し、結果を返す。

DebugScenarioTestRegistry
  テストケースとカテゴリ色を登録する中央レジストリ。

DebugScenarioTestPanel
  Registry の内容を表示し、単体実行・一括実行を行う汎用UI。

IDebugScenarioTestModule
  プロジェクト固有のテスト登録口。
```

## 実行キー

初期設定では `F2` でテストパネルを開閉します。

通常の `RuntimeDebugPanel` とは同時表示しません。

- `F1`: デバッグコマンドパネルを開く。テストパネルは閉じる。
- `F2`: テストパネルを開く。デバッグコマンドパネルは閉じる。

## プロジェクト側の登録例

プロジェクト側に `IDebugScenarioTestModule` を実装します。

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using UnityEngine;

public sealed class GameDebugScenarioTestModule : IDebugScenarioTestModule
{
    public void Register()
    {
        DebugScenarioTestRegistry.SetCategoryColor("バトル", new Color(0.95f, 0.45f, 0.42f));
        DebugScenarioTestRegistry.Register(CreatePlayerDamageTest());
    }

    private static DebugScenarioTestCase CreatePlayerDamageTest()
    {
        DebugScenarioTestCase test = ScriptableObject.CreateInstance<DebugScenarioTestCase>();
        test.Id = "battle.player_damage_basic";
        test.DisplayName = "プレイヤーが4ダメージを受けるとHPが4減る";
        test.Category = "バトル / プレイヤーHP";
        test.Summary = "HP 20 のプレイヤーへ 4 ダメージを与え、HP が 16 になることを確認します。";
        test.Purpose = "基本ダメージ処理を確認します。";
        test.Preconditions = "GameSceneで実行してください。";
        test.ExpectedResult = "HPが16になります。";

        test.Steps.Add(Step("HPを20に設定する", "player.set_hp", ArgInt("hp", 20)));
        test.Steps.Add(Step("4ダメージを与える", "player.damage", ArgInt("amount", 4)));
        test.Steps.Add(Step("HPが16であることを確認する", "assert.player_hp", ArgInt("expected", 16)));
        return test;
    }

    private static DebugScenarioTestStep Step(string label, string commandId, params DebugScenarioArgument[] args)
    {
        var step = new DebugScenarioTestStep
        {
            DisplayName = label,
            CommandId = commandId,
            Kind = DebugScenarioTestStepKind.Command,
            StopOnFailure = true,
        };
        step.Arguments.AddRange(args);
        return step;
    }

    private static DebugScenarioArgument ArgInt(string key, int value)
    {
        return new DebugScenarioArgument
        {
            Key = key,
            Kind = DebugScenarioArgumentKind.Int,
            Value = value.ToString(),
        };
    }
}
#endif
```

登録は既存のデバッグモジュールなどから行います。

```csharp
DebugScenarioTestRegistry.RegisterOnce(new GameDebugScenarioTestModule());
```

## ScriptableObjectでの登録

コードではなくアセットとしてテストケースを作ることもできます。

```text
Resources/DebugTests/*.asset
```

`DebugScenarioTestRegistry` は初期状態で `Resources/DebugTests` を読み込みます。

コード登録だけにしたい場合:

```csharp
DebugScenarioTestRegistry.SetIncludeResourceTests(false);
```

## コマンド設計

テストステップは `DebugCommandRegistry` に登録済みのコマンドを呼びます。

推奨カテゴリ:

```text
setup.*
  前提状態を作る

act.*
  実際のゲーム処理を起こす

assert.*
  期待値を検証する

wait.*
  非同期処理や演出を待つ

dump.*
  失敗調査用の状態を出す
```

例:

```text
player.set_hp
player.damage
assert.player_hp
wait.seconds
dump.test_context
```

## 表示ルール

ステータス:

```text
-     未実行
...   実行中
✓     成功
✕     失敗
!     エラー
SKIP  スキップ
```

カテゴリ色は `DebugScenarioTestRegistry.SetCategoryColor()` で上書きできます。

```csharp
DebugScenarioTestRegistry.SetCategoryColor("カード", new Color(0.38f, 0.82f, 0.58f));
```

完全一致しない場合も、カテゴリ文字列にキーが含まれていれば適用されます。

例:

```text
キー: カード
カテゴリ: カード / ゾーン
```

## 実装時の注意

- 汎用基盤からゲーム固有クラスを参照しない
- ゲーム固有の検証は `assert.*` コマンドとしてプロジェクト側に置く
- 破壊的なテストはカテゴリ名や説明に明記する
- テストケースの失敗理由には、期待値と実際値を入れる
- 本番ビルドに入れたくない処理は `UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG` で囲む

## 大正少女での実装例

このプロジェクトでは以下にゲーム固有登録を置いています。

```text
Assets/Scripts/Debug/Tests/DebugScenarioTestCatalog.cs
```

ゲーム固有のテスト用コマンドは以下です。

```text
Assets/Scripts/Debug/Commands/TestDebugCommands.cs
```

汎用パネルはこれらを直接参照せず、`DebugScenarioTestRegistry` と `DebugCommandRegistry` だけを見ます。
