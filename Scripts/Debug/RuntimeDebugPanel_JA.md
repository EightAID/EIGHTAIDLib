# Runtime Debug Panel 仕様

EIGHTAIDLib の `Scripts/Debug` は、実行中の Unity ゲームへ重ねて使う汎用デバッグパネルです。
EIGHTAIDLib 側は「コマンドを登録して実行するための共通 UI」と「コマンド定義 API」だけを担当します。
カード、セーブ、バトルなどのゲーム固有処理は、各プロジェクト側のコマンドモジュールへ分離してください。

## 基本構成

- `RuntimeDebugPanel`
  - F1 で開閉する実行時デバッグ UI です。
  - コマンド検索、カテゴリ絞り込み、引数入力、実行結果ログを扱います。
- `DebugCommand`
  - コマンド ID、表示名、カテゴリ、説明、引数、実行処理を Builder 形式で定義します。
- `DebugCommandRegistry`
  - 登録済みコマンドを保持します。同じ ID を登録すると後勝ちで上書きします。
- `IDebugOptionProvider`
  - Asset やセーブスロットなど、選択肢つき引数の候補を供給します。
- `IDebugCommandModule`
  - プロジェクト側のコマンド群をカテゴリ単位でまとめる登録単位です。
- `DebugCommandModuleRegistry`
  - モジュールを一度だけ登録するための補助クラスです。

## コマンドモジュール

プロジェクト側に次のようなモジュールを作り、起動時の Bootstrap から登録します。

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
public sealed class SampleDebugCommandModule : IDebugCommandModule
{
    public void Register()
    {
        DebugCommandRegistry.Register(
            DebugCommand
                .Create("sample.say_hello", "Helloを出力")
                .Category("サンプル")
                .Description("Consoleへ確認用ログを出します。")
                .Run(_ =>
                {
                    UnityEngine.Debug.Log("[DebugCommand] Hello");
                    return DebugCommandResult.Success("Helloを出力しました。");
                }));
    }
}
#endif
```

Bootstrap 側では次のように登録します。

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
DebugCommandModuleRegistry.RegisterOnce(new SampleDebugCommandModule());
#endif
```

## 引数と選択肢

数値、文字列、bool、enum は `ArgInt` / `ArgString` / `ArgBool` / `ArgEnum` で追加できます。
Asset やセーブスロットのように候補から選びたいものは、`IDebugOptionProvider` を実装して
`DebugOptionProviderRegistry.Register` へ登録し、コマンド側で `ArgOption` を使います。

```csharp
DebugCommand
    .Create("sample.use_asset", "Assetを使う")
    .Category("サンプル")
    .ArgOption("asset", "対象Asset", SampleAssetOptionProvider.Id)
    .Run(context =>
    {
        string assetPath = context.Args.GetString("asset");
        return DebugCommandResult.Success(assetPath);
    });
```

## カテゴリ設計

カテゴリ名はプロジェクト側で自由に決められます。おすすめは、実行対象で分けることです。
例: `シーン`, `セーブ`, `プレイヤー`, `カード`, `バトル`, `マップ`, `表示/検証`, `危険操作`。

削除やリセットなど取り返しにくい操作は `危険操作` に集め、説明文に影響範囲を書いてください。

## 責務の境界

EIGHTAIDLib 側に入れてよいもの:

- デバッグパネル UI
- コマンド定義 API
- モジュール登録 API
- 引数 UI と OptionProvider の共通仕組み
- 検索、カテゴリ、履歴、ログなどの汎用機能

プロジェクト側に置くもの:

- ゲーム固有の SceneList
- カード、敵、セーブ、マップ、会話などの型に依存する処理
- AssetDatabase 検索条件
- 進行状態やランタイム状態を変更する実行処理

## DaishouShoujo での利用例

DaishouShoujo では、プロジェクト側の `GameDebugCommandModule` から次のコマンド群を登録しています。

- シーン
- 進行
- セーブ
- プレイヤー
- カード
- 会話
- マップ
- バトル
- 表示/検証
- システム

バトル検証では `battle.play_normal` と `battle.play_boss` を使い、
通常戦闘は `EnemyDefinition`、BOSS 戦闘は `BossBattleDefinition` を選択して GameScene を直接起動します。
