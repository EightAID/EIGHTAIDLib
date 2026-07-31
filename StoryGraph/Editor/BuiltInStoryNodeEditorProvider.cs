using System.Collections.Generic;
using UnityEngine;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>どのプロジェクトでも利用できる基本ノードを登録します。</summary>
    internal sealed class BuiltInStoryNodeEditorProvider : IStoryNodeEditorProvider
    {
        public int Priority => 0;

        public IEnumerable<StoryNodeDefinition> GetDefinitions()
        {
            yield return new StoryNodeDefinition(
                StoryNodeTypeIds.Root, "開始", "基本", "グラフの開始地点です。",
                () => new EmptyStoryNodePayload(),
                new[] { new StoryNodePortDefinition("次へ", StoryEdgeRole.Next) },
                new Color(0.2f, 0.55f, 0.35f));
            yield return new StoryNodeDefinition(
                StoryNodeTypeIds.Message, "メッセージ", "基本", "話者名と本文を表示します。",
                () => new MessagePayload());
            yield return new StoryNodeDefinition(
                StoryNodeTypeIds.Branch, "条件分岐", "基本", "プロジェクト側の条件評価結果で分岐します。",
                () => new BranchPayload(),
                new[]
                {
                    new StoryNodePortDefinition("一致", StoryEdgeRole.True),
                    new StoryNodePortDefinition("不一致", StoryEdgeRole.False)
                },
                new Color(0.55f, 0.42f, 0.2f));
            yield return new StoryNodeDefinition(
                StoryNodeTypeIds.Delay, "待機", "基本", "指定秒数だけ待機します。",
                () => new DelayPayload());
            yield return new StoryNodeDefinition(
                StoryNodeTypeIds.Comment, "コメント", "基本", "実行されない制作者向けメモです。",
                () => new CommentPayload(),
                new[] { new StoryNodePortDefinition("次へ", StoryEdgeRole.Next) },
                new Color(0.32f, 0.32f, 0.34f));
            yield return new StoryNodeDefinition(
                StoryNodeTypeIds.End, "終了", "基本", "グラフを終了します。",
                () => new EmptyStoryNodePayload(),
                System.Array.Empty<StoryNodePortDefinition>(),
                new Color(0.55f, 0.25f, 0.25f));
        }
    }
}
