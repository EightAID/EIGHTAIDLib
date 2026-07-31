using System;
using System.Collections.Generic;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// グラフ上の現在位置と遷移だけを管理します。
    /// ゲーム固有の表示処理を持たないため、会話送り・デバッグ再生・自動実行から共用できます。
    /// </summary>
    public sealed class StoryGraphCursor
    {
        private readonly StoryGraphAsset _graph;
        private readonly Dictionary<string, StoryNodeRecord> _nodesById;

        public StoryGraphCursor(StoryGraphAsset graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _nodesById = BuildNodeIndex(graph);
        }

        public StoryGraphAsset Graph => _graph;
        public StoryNodeRecord Current { get; private set; }

        public StoryNodeRecord ResetToRoot()
        {
            foreach (StoryNodeRecord node in _graph.Nodes)
            {
                if (node != null && node.NodeTypeId == StoryNodeTypeIds.Root)
                {
                    Current = node;
                    return Current;
                }
            }

            Current = null;
            return null;
        }

        public StoryNodeRecord MoveNext(StoryEdgeRole requestedRole)
        {
            if (Current == null)
            {
                return null;
            }

            StoryEdgeRecord fallback = null;
            foreach (StoryEdgeRecord edge in Current.Edges)
            {
                if (edge == null)
                {
                    continue;
                }

                if (edge.Role == requestedRole &&
                    _nodesById.TryGetValue(edge.TargetNodeId, out StoryNodeRecord target))
                {
                    Current = target;
                    return Current;
                }

                if (edge.Role == StoryEdgeRole.Default)
                {
                    fallback = edge;
                }
            }

            Current = fallback != null &&
                      _nodesById.TryGetValue(fallback.TargetNodeId, out StoryNodeRecord defaultTarget)
                ? defaultTarget
                : null;
            return Current;
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
                    throw new InvalidOperationException($"StoryGraph node ID is duplicated: {node.Id}");
                }
            }

            return result;
        }
    }
}
