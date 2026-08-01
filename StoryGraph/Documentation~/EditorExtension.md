# エディタ拡張

共通エディタは、グラフ操作の利便性とプロジェクト固有UIを分離しています。

- 共通側: 日本語ツールバー、検索、コンパクト表示、ズーム、保存、接続編集
- プロジェクト側: ノード定義、要約、検索対象、型別Inspector

## ノードを登録する

Editor assemblyで`IStoryNodeEditorProvider`を実装します。Providerは`TypeCache`で自動検出されます。

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
            () => new ShowImagePayload());
    }
}
```

`nodeTypeId`は保存済みassetから参照されるため、公開後は変更しないでください。

## 型別の見せ方を登録する

`IStoryNodeEditorPresentationProvider`を実装すると、巨大なpayloadをそのまま表示せず、ノード種別ごとに必要な項目だけ表示できます。

```csharp
public sealed class SamplePresentationProvider : IStoryNodeEditorPresentationProvider
{
    public int Priority => 100;

    public IEnumerable<StoryNodeEditorPresentation> GetPresentations()
    {
        yield return new StoryNodeEditorPresentation(
            "sample.show-image",
            payload => ((ShowImagePayload)payload).image?.name,
            payload => new[] { ((ShowImagePayload)payload).caption },
            context =>
            {
                context.AddProperty("image", "画像");
                context.AddProperty("caption", "説明");
                context.AddProperty("duration", "表示時間");
            });
    }
}
```

引数の役割は次のとおりです。

- `summaryFactory`: ノード上の短い要約を作る
- `searchTextFactory`: 「本文・設定」検索へ渡す文字列を作る
- `inspectorFactory`: 右側Inspectorへ表示する項目と順序を決める

表示定義がないノードだけ、互換用の汎用payload Inspectorへフォールバックします。

## 上書き規則

同じ`nodeTypeId`を複数Providerが返した場合は、`Priority`が高いProviderの定義を使います。共通ノードを作品向けpayloadへ差し替える場合にも利用できます。
