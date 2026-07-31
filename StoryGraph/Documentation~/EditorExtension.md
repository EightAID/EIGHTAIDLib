# Editor拡張

## Providerを作る

Editor用assemblyへ`IStoryNodeEditorProvider`実装を置きます。

```csharp
public sealed class SampleNodeEditorProvider : IStoryNodeEditorProvider
{
    public int Priority => 100;

    public IEnumerable<StoryNodeDefinition> GetDefinitions()
    {
        yield return new StoryNodeDefinition(
            "sample.show-image",
            "画像表示",
            "演出",
            "指定した画像を表示します。",
            () => new ShowImagePayload(),
            new[]
            {
                new StoryNodePortDefinition("次へ", StoryEdgeRole.Next)
            },
            new Color(0.3f, 0.4f, 0.6f));
    }
}
```

手動登録は不要です。Unityのdomain reload後に自動検出されます。

## node type ID

`プロジェクト識別子.機能名`の形式を推奨します。

- `core.message`
- `sample.show-image`
- `mygame.set-flag`

保存済みassetが参照するため、公開後は変更しないでください。

## 組み込み定義の差し替え

利用プロジェクト固有のMessage payloadを使う場合などは、同じnode type IDを返し、Providerの`Priority`を組み込み値より高くします。
最もPriorityが高い定義がEditorで使用されます。

## port

通常遷移は`Next`、条件分岐は`True`と`False`、該当edgeがない場合の遷移は`Default`を使用します。
処理結果で分岐するノードには`Success`と`Failure`を利用できます。

## payload Inspector

ノードを選択するとpayloadが`SerializedProperty`として表示されます。
独自PropertyDrawerも通常どおり利用できます。

payload型を変更した場合は既存assetを自動変換せず、バックアップ、変換、構造監査、Unity Object参照照合を行う専用移行ツールを用意してください。
