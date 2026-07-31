# アーキテクチャ

## 依存方向

```text
利用プロジェクト Runtime
  ├─ payload
  ├─ Handler / 条件評価
  └─ View / 入力
          ↓
EightAID.StoryGraph.Runtime
  ├─ StoryGraphAsset
  ├─ StoryNodeRecord / StoryEdgeRecord
  ├─ StoryGraphCursor
  ├─ StoryGraphRunner / StoryGraphRegistry
  └─ 表示・入力contract

利用プロジェクト Editor
  └─ IStoryNodeEditorProvider
          ↓
EightAID.StoryGraph.Editor
  └─ Graph Editor / payload Inspector
```

共通assemblyから利用プロジェクトの型を参照してはいけません。利用プロジェクトから共通assemblyを参照する一方向にします。

## 保存schema

`StoryGraphAsset`は次を保存します。

- format version
- 概要
- `StoryNodeRecord`の一覧

`StoryNodeRecord`は次を保存します。

- graph内で一意なID
- 安定したnode type ID
- `[SerializeReference] IStoryNodePayload`
- Editor座標
- `StoryEdgeRecord`の一覧

`StoryEdgeRecord`は接続先ID、edge role、手動route情報を保存します。
payloadとedgeへ構造情報を重複保存しません。

## payload

payloadはJSONではありません。`[Serializable]`クラスとして定義し、`IStoryNodePayload`を実装します。
これにより`Sprite`、`AudioClip`、独自`ScriptableObject`などのUnity Object参照を保持できます。

`SchemaVersion`はpayload内部schemaの更新判定に使用します。公開済み型のフィールドを変更するときは、versionを上げてEditor移行処理を用意します。

## 実行

入力ごとに1ノード進む会話では`StoryGraphCursor`を使用します。
自動処理では`StoryGraphRunner`と`IStoryNodeHandler`を使用します。

ゲーム固有条件は`IStoryGraphConditionEvaluator`、実行中表示は`IStoryGraphTrackingSink`として注入します。

## Editor

共通Editorは`IStoryNodeEditorProvider`を`TypeCache`で自動検出します。
Providerが返す`StoryNodeDefinition`によって、作成メニュー、payload factory、port、色、説明が決まります。

payload Inspectorは`SerializedProperty`で描画するため、一般的なUnity serialize可能フィールドは追加UIなしで編集できます。
