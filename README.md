# EIGHTAIDLib

`EIGHTAIDLib` は、Unity プロジェクト間で再利用することを前提にした共通ライブラリです。
ランタイム用の基盤コードと Editor 拡張をまとめ、タイトル固有の実装とは分離して管理することを目的としています。

## 概要

ライブラリは主に次の領域で構成されています。

- `EightAID.EIGHTAIDLib.Audio`
- `EightAID.EIGHTAIDLib.UI`
- `EightAID.EIGHTAIDLib.Input`
- `EightAID.EIGHTAIDLib.Effect`
- `EightAID.EIGHTAIDLib.Utility`

共通化しやすい基盤機能をこのリポジトリに集約し、ゲーム固有のルール、シーン進行、コンテンツ依存の処理はホスト側で持つ、という分離を基本方針とします。

## 含まれている主な機能

現時点では、たとえば次のような機能を含みます。

- `SoundControllerBase` などの Audio 基盤
- `NavigationScope`、`DialogueTextPresenter`、`DialoguePresenterController`、`SelectableSpriteSwap`、`UIHitAreaVisualizer` などの UI 支援
- `InputChannel`、`InputType`、`ControllerDevice` などの Input 支援
- `PostProcessVolumeUtility`、`PostProcessParameterType`、`PostProcessParameterValue`、`TransformShakeUtility`、`UIEffectTransitionUtility` などの Effect 補助
- `SingletonMonoBehaviour`、`PersistentSingletonMonoBehaviour`、`CsvLoaderBase`、`SaveDataBase`、各種拡張メソッドなどの Utility
- `UIHitAreaVisualizerWindow` などの Editor 拡張

## 設計方針

このリポジトリに置くもの:

- タイトル固有の前提を持たない再利用可能な Unity コンポーネント
- UI / Input / Audio / Effect / Utility の共通基盤
- 複数プロジェクトで使い回せる Editor 拡張

このリポジトリに置かないもの:

- ゲーム固有のルールやバランス調整
- 単一タイトル専用のシーン遷移や進行制御
- 他プロジェクトへそのまま移植できない暫定実装

判断に迷う場合は、別の Unity プロジェクトへそのまま持っていけるか、タイトル名や専用データ構造を前提にしていないかを基準にしてください。

## Editor Tool

### UI Hit Area Visualizer

`Tools/EIGHTAID/UI Hit Area Visualizer` から開ける Editor Window です。
選択中オブジェクト、またはシーン内 Canvas に対して `UIHitAreaVisualizer` を追加・削除し、インタラクティブな UI 領域の確認に利用できます。

## 導入

Git submodule として追加する場合:

```bash
git submodule add https://github.com/EightAID/EIGHTAIDLib.git EIGHTAIDLib
git submodule update --init --recursive
```

## 保守方針

- 公開 API は可能な限り安定させる
- 破壊的変更よりも追加的変更を優先する
- 新機能を追加する場合は、既存の責務に沿った配置を優先する
