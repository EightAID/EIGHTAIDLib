using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// Branch ノードの条件キーを各プロジェクトの状態へ問い合わせます。
    /// </summary>
    public interface IStoryGraphConditionEvaluator
    {
        bool Evaluate(string conditionKey, string expectedValue);
    }

    /// <summary>
    /// プロジェクト固有ノードを実行する契約です。
    /// </summary>
    public interface IStoryNodeHandler
    {
        Task<StoryNodeHandlingResult> ExecuteAsync(StoryNodeRecord node, CancellationToken cancellationToken);
    }

    /// <summary>
    /// ノード実行後にどの edge role をたどるかを指定します。
    /// </summary>
    public readonly struct StoryNodeHandlingResult
    {
        public StoryNodeHandlingResult(StoryEdgeRole edgeRole)
        {
            EdgeRole = edgeRole;
        }

        public StoryEdgeRole EdgeRole { get; }

        public static StoryNodeHandlingResult Next => new StoryNodeHandlingResult(StoryEdgeRole.Next);
        public static StoryNodeHandlingResult Success => new StoryNodeHandlingResult(StoryEdgeRole.Success);
        public static StoryNodeHandlingResult Failure => new StoryNodeHandlingResult(StoryEdgeRole.Failure);
    }

    /// <summary>
    /// 実行中のノード変更を受け取る通知先です。Editor 追跡やログ表示に利用します。
    /// </summary>
    public interface IStoryGraphTrackingSink
    {
        void OnNodeChanged(StoryGraphAsset graph, StoryNodeRecord node);
    }

    /// <summary>
    /// グラフの基本的な遷移を実行します。ゲーム固有の条件とノード処理は登録済み実装へ委譲します。
    /// </summary>
    public sealed class StoryGraphRunner
    {
        private readonly StoryGraphRegistry _registry;
        private readonly IStoryGraphConditionEvaluator _conditionEvaluator;
        private readonly IStoryGraphTrackingSink _trackingSink;

        public StoryGraphRunner(
            StoryGraphRegistry registry,
            IStoryGraphConditionEvaluator conditionEvaluator = null,
            IStoryGraphTrackingSink trackingSink = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _conditionEvaluator = conditionEvaluator;
            _trackingSink = trackingSink;
        }

        public async Task RunAsync(StoryGraphAsset graph, CancellationToken cancellationToken)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var cursor = new StoryGraphCursor(graph);
            StoryNodeRecord current = cursor.ResetToRoot();
            if (current == null)
            {
                throw new InvalidOperationException("[StoryGraph] Root ノードが見つかりません。");
            }

            while (current != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _trackingSink?.OnNodeChanged(graph, current);

                if (current.NodeTypeId == StoryNodeTypeIds.End)
                {
                    return;
                }

                StoryEdgeRole edgeRole = await ExecuteNodeAsync(current, cancellationToken);
                current = cursor.MoveNext(edgeRole);
            }
        }

        private async Task<StoryEdgeRole> ExecuteNodeAsync(StoryNodeRecord node, CancellationToken cancellationToken)
        {
            if (node.NodeTypeId == StoryNodeTypeIds.Root || node.NodeTypeId == StoryNodeTypeIds.Comment)
            {
                return StoryEdgeRole.Next;
            }

            if (node.NodeTypeId == StoryNodeTypeIds.Branch)
            {
                BranchPayload payload = node.Payload as BranchPayload;
                bool result = _conditionEvaluator != null && payload != null &&
                              _conditionEvaluator.Evaluate(payload.ConditionKey, payload.ExpectedValue);
                return result ? StoryEdgeRole.True : StoryEdgeRole.False;
            }

            if (!_registry.TryGetHandler(node.NodeTypeId, out IStoryNodeHandler handler))
            {
                throw new InvalidOperationException($"[StoryGraph] ノードHandlerが未登録です。nodeTypeId={node.NodeTypeId}");
            }

            return (await handler.ExecuteAsync(node, cancellationToken)).EdgeRole;
        }

        private static Dictionary<string, StoryNodeRecord> BuildNodeIndex(StoryGraphAsset graph)
        {
            var result = new Dictionary<string, StoryNodeRecord>();
            foreach (StoryNodeRecord node in graph.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    continue;
                }

                if (!result.TryAdd(node.Id, node))
                {
                    throw new InvalidOperationException($"[StoryGraph] ノードIDが重複しています。id={node.Id}");
                }
            }

            return result;
        }

        private static StoryNodeRecord FindRoot(IReadOnlyList<StoryNodeRecord> nodes)
        {
            foreach (StoryNodeRecord node in nodes)
            {
                if (node != null && node.NodeTypeId == StoryNodeTypeIds.Root)
                {
                    return node;
                }
            }

            return null;
        }

        private static StoryNodeRecord FindNextNode(
            StoryNodeRecord node,
            StoryEdgeRole requestedRole,
            IReadOnlyDictionary<string, StoryNodeRecord> nodesById)
        {
            StoryEdgeRecord fallback = null;
            foreach (StoryEdgeRecord edge in node.Edges)
            {
                if (edge == null)
                {
                    continue;
                }

                if (edge.Role == requestedRole && nodesById.TryGetValue(edge.TargetNodeId, out StoryNodeRecord target))
                {
                    return target;
                }

                if (edge.Role == StoryEdgeRole.Default)
                {
                    fallback = edge;
                }
            }

            return fallback != null && nodesById.TryGetValue(fallback.TargetNodeId, out StoryNodeRecord defaultTarget)
                ? defaultTarget
                : null;
        }

    }
}
