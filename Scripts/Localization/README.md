# EIGHTAIDLib Localization

EIGHTAIDLib のローカライズ基盤は、複数プロジェクトで使い回せる「言語状態」と「UI 文言解決」だけを担当します。
会話データ、キャラクター名、ゲーム固有の演出は各プロジェクト側に残してください。

## 役割分担

EIGHTAIDLib 側:

- 現在言語の保持
- PlayerPrefs への保存
- 言語変更イベント
- Resources 上の CSV 読み込み
- UI テキストの key -> 翻訳文解決
- TextMeshProUGUI への即時反映
- Unity エディタ上部の言語切り替え

プロジェクト側:

- CSV の配置と列ルール
- 会話、キャラクター名、ショップ文言などの専用解決
- 文字化け、演出差分、ゲームルールに依存する後処理

## CSV 形式

標準では以下の Resources パスを読みます。

```text
Resources/Localize/TextDatas/UITextLocalization.csv
```

標準の列は次の通りです。

```csv
ja,en
はじめる,Start
設定,Settings
```

別パスや別列を使う場合は、初回参照前に設定してください。

```csharp
using EightAID.EIGHTAIDLib.Localization;

EALocalizationService.ConfigureUiTable(
    uiCsvResourcePath: "Localize/TextDatas/UITextLocalization",
    sourceColumnIndex: 0,
    englishColumnIndex: 1,
    languagePrefKey: "localization.language");
```

## UI への使い方

TextMeshProUGUI と同じ GameObject に `EALocalizedText` を付けます。
コンポーネントは最初の表示文字列を元テキストとして保持し、現在言語に合わせて表示を更新します。

スクリプトから文言を入れる場合は、直接 `text.text` を書き換える代わりに次を使います。

```csharp
using EightAID.EIGHTAIDLib.Localization;

EALocalizedTextBinder.ApplyText(titleText, "設定");
```

## 言語切り替え

```csharp
using EightAID.EIGHTAIDLib.Localization;

EALocalizationService.SetLanguage(EALocalizationLanguage.English);
```

`SetLanguage` は PlayerPrefs に保存し、`LanguageChanged` を通知します。
`EALocalizedText` はこの通知を受けて自動で再描画します。

## プロジェクト固有の後処理

文字化け演出など、翻訳後にゲーム固有処理を挟みたい場合は `EALocalizedText` を継承します。

```csharp
using EightAID.EIGHTAIDLib.Localization;

public class GameLocalizedText : EALocalizedText
{
    protected override string PostProcessResolvedText(string resolvedText)
    {
        return MyTextEffect.ApplyIfNeeded(resolvedText);
    }
}
```

DaishouShoujo では既存 Prefab 互換のため、`UITextLocalize` がこの継承ラッパーになっています。
