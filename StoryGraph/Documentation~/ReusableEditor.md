# リッチエディタを別プロジェクトで使う

1. `EIGHTAIDLib`をgit submoduleとして`Assets`配下へ配置します。
2. `Packages/manifest.json`から`StoryGraph` packageを参照します。
3. Runtime assemblyへpayloadとhandlerを置きます。
4. Editor assemblyへ`IStoryNodeEditorProvider`を置きます。
5. 見た目を保つ場合は`IStoryNodeEditorPresentationProvider`も実装します。

共通ウィンドウは`Tools > EIGHTAID > Story > グラフエディタ`から開けます。

検索・コンパクト表示・ズーム・日本語ツールバーは共通側にあるため、各プロジェクトで再実装する必要はありません。作品側は、どの型にどのフィールドを見せるかだけを登録します。

## 責務の境界

```text
EIGHTAIDLib StoryGraph Editor
  ├─ ウィンドウとツールバー
  ├─ GraphViewと接続保存
  ├─ 検索と選択移動
  ├─ コンパクト表示
  └─ Providerの自動検出

利用プロジェクト Editor
  ├─ ノード種別とカテゴリ
  ├─ 型別Inspector
  ├─ ノード要約と検索語
  └─ 作品固有プレビュー
```

作品固有プレビューが必要な場合も、共通packageへゲームの型を追加せず、presentation provider側で組み立ててください。
