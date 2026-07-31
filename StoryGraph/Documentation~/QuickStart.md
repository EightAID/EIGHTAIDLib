# クイックスタート

## 1. グラフassetを作る

ProjectウィンドウのCreateメニューから次を選びます。

`EightAID > Story Graph > Story Graph Asset`

## 2. 共通Editorで編集する

`Tools > EIGHTAIDLib > StoryGraph > グラフエディタ` を開き、上部のAsset欄へグラフを指定します。

- 何もない場所を右クリックするとノードを追加できます。
- port同士をドラッグすると接続できます。
- ノードを選択すると右側にpayloadのInspectorが表示されます。
- 移動、削除、接続変更はUndo対象になり、assetへ保存されます。

## 3. 1ステップずつ進める

```csharp
var cursor = new StoryGraphCursor(graphAsset);
StoryNodeRecord current = cursor.ResetToRoot();
current = cursor.MoveNext(StoryEdgeRole.Next);
```

会話送りのように入力ごとに進むシステムでは`StoryGraphCursor`を使います。
条件分岐では利用プロジェクトが条件を評価し、`True`または`False`を渡します。

## 4. Handlerを自動実行する

```csharp
var registry = new StoryGraphRegistry();
registry.Register(MyDefinitions.SetFlag, new SetFlagHandler());

var runner = new StoryGraphRunner(registry, conditionEvaluator, trackingSink);
await runner.RunAsync(graphAsset, cancellationToken);
```

Handlerはpayloadを型チェックして利用してください。

```csharp
public Task<StoryNodeHandlingResult> ExecuteAsync(
    StoryNodeRecord node,
    CancellationToken cancellationToken)
{
    var payload = node.Payload as SetFlagPayload
        ?? throw new InvalidOperationException("SetFlagPayloadが必要です。");

    flags.Set(payload.flagId, payload.value);
    return Task.FromResult(StoryNodeHandlingResult.Next);
}
```

## 5. ノードを追加しやすくする

Editor用assemblyで`IStoryNodeEditorProvider`を実装します。
Providerは自動検出され、`Category/DisplayName`の階層で右クリックメニューに表示されます。
`Priority`を上げると、同じnode type IDの組み込み定義をプロジェクト用payloadへ差し替えられます。

## データ更新ルール

- node type IDは公開後に変更しないでください。
- payload構造を変える場合は`SchemaVersion`を上げ、明示的な移行処理を用意してください。
- Unity Object参照があるpayloadをJSONへ変換しないでください。
- 大規模移行では元assetを別フォルダへバックアップし、node ID・edge・参照数を照合してから保存してください。
