# EIGHTAIDLib StoryGraph

Unityプロジェクト間で再利用できる、型付きノード・グラフ編集・実行基盤です。
ゲーム固有のカード、セーブデータ、演出クラスなどを共通packageへ持ち込まず、payload・Handler・Editor Providerとして利用側から追加します。

## 導入

EIGHTAIDLibを `Assets/Scripts/EIGHTAIDLib` へgit submoduleとして配置し、利用プロジェクトの `Packages/manifest.json` に追加します。

```json
"com.eightaid.story-graph": "file:../Assets/Scripts/EIGHTAIDLib/StoryGraph"
```

Unityで次を開くと共通Editorを利用できます。

`Tools > EIGHTAIDLib > StoryGraph > グラフエディタ`

## 主な型

- `StoryGraphAsset`: ノード、接続、概要、schema versionを保存する共通asset
- `StoryNodeRecord`: ID、node type ID、型付きpayload、Editor座標を保存
- `IStoryNodePayload`: `[SerializeReference]` で保存されるノード固有データ
- `StoryGraphCursor`: Root探索、ID索引、edge roleによる1ステップ遷移
- `StoryGraphRunner`: Handlerを順に実行する非同期Runner
- `IStoryNodeEditorProvider`: 利用プロジェクトのノードを共通Editorへ登録

Unity Object参照を保持するため、payloadはJSON文字列ではなく `[SerializeReference]` を使用します。
`Sprite`、`AudioClip`、独自`ScriptableObject`も通常のSerializeFieldと同様に保持できます。

## プロジェクト固有ノードの追加

1. `[Serializable]` なpayloadを作り、`IStoryNodePayload` を実装します。
2. 実行が必要なら `IStoryNodeHandler` を実装します。
3. Editor用assemblyで `IStoryNodeEditorProvider` を実装します。
4. Providerから `StoryNodeDefinition` を返します。

ProviderはUnityの`TypeCache`で自動検出されます。共通packageを修正したり、初期化コードから手動登録したりする必要はありません。

```csharp
[Serializable]
public sealed class SetFlagPayload : IStoryNodePayload
{
    public int SchemaVersion => 1;
    public string flagId;
    public bool value;
}
```

```csharp
public sealed class MyStoryNodeEditorProvider : IStoryNodeEditorProvider
{
    public int Priority => 100;

    public IEnumerable<StoryNodeDefinition> GetDefinitions()
    {
        yield return new StoryNodeDefinition(
            "mygame.set-flag",
            "フラグ変更",
            "ゲーム進行",
            "指定したフラグを変更します。",
            () => new SetFlagPayload());
    }
}
```

詳細は `Documentation~/QuickStart.md`、`Architecture.md`、`EditorExtension.md` を参照してください。
