# EIGHTAIDLib 構造・利用ガイド

この資料は EIGHTAIDLib サブモジュール自身の構造と、ゲームプロジェクトから安全に利用するための境界を示します。本体固有のゲームフロー、Scene、保存形式、カード仕様はこのサブモジュールに入れません。

## 責務

| 領域 | 主な場所 | 提供するもの |
| --- | --- | --- |
| Audio | `Scripts/Audio/`、`Scripts/AudioSync/` | `SoundControllerBase`、音声同期の基盤 |
| UI | `Scripts/UI/` | `NavigationScope`、会話表示、UI部品・選択補助 |
| Input | `Scripts/InputSystem/` | 入力種別・デバイス判定の共通型 |
| Effect | `Scripts/Effect/` | 揺れ、UI遷移、ポストプロセス補助 |
| Utility | `Scripts/Utilty/` | Singleton、CSV、保存基底、ログ、拡張 |
| Localization | `Scripts/Localization/` | ローカライズの共通サービスとBinder |
| Debug | `Scripts/Debug/` | デバッグコマンドとシナリオテスト基盤 |
| Analytics | `Scripts/Analytics/` | 展示ログの共通記録 |
| StoryGraph | `StoryGraph/` | ノードグラフのRuntimeとEditor。詳細は`StoryGraph/README.md` |
| Editor | `Editor/` | 再利用可能なUnity Editorツール |

## 本体との境界

- 本体は `Assets/Scripts/`、ライブラリは `Assets/Scripts/EIGHTAIDLib/` に置く。
- ライブラリはゲーム固有のController、Scene名、ScriptableObject、保存項目、アセットパスを参照しない。
- 本体はライブラリの公開APIを利用し、ライブラリ内部へゲーム固有の分岐を追加しない。
- `SoundControllerBase`のような基盤を拡張する場合、ゲーム固有のID・保存・演出判断は派生側へ置く。
- UnityのRuntimeコードとEditorコードを混在させない。

## よく使う入口

- UIモーダル・選択範囲: `Scripts/UI/NavigationScope.cs`
- サウンド基盤: `Scripts/Audio/SoundControllerBase.cs`
- UI演出: `Scripts/Effect/UIEffectTransitionUtility.cs`、`TransformShakeUtility.cs`
- ローカライズ: `Scripts/Localization/README.md`
- デバッグコマンド: `Scripts/Debug/Core/DebugCommandRegistry.cs`
- シナリオテスト: `Scripts/Debug/ScenarioTests/DebugScenarioTestPanelSpec.md`

## 変更時チェック

1. 本当に複数プロジェクトで再利用でき、ゲーム固有状態を持たないか確認する。
2. 公開APIを変更する場合、利用側プロジェクトを検索し、破壊的変更なら移行方法をREADMEまたは専用資料へ記載する。
3. Unity APIを使うRuntimeコードはEditor依存を持たないことを確認する。
4. `NavigationScope`、Audio、Debugなど既存利用者の多い型は、利用側の回帰確認を行う。
5. クラスの責務、公開API、フォルダ構成、利用手順が変わる場合はこの資料と該当領域のREADMEを同じ変更で更新する。

## サブモジュールの運用

- サブモジュール内の変更は、サブモジュール側でコミットしてから、本体リポジトリには参照コミットの更新として反映する。
- 本体とライブラリにまたがる変更は、両方の資料を更新し、どちらが責務を持つかを明記する。
- このサブモジュールに既存の未コミット変更がある場合、それらを無関係な整理作業に含めない。
