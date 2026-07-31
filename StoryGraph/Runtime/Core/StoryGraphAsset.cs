using System.Collections.Generic;
using UnityEngine;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// プロジェクト固有の実装に依存しないストーリーグラフの保存単位です。
    /// ノードの具体的な意味は nodeTypeId と payloadJson で拡張します。
    /// </summary>
    [CreateAssetMenu(fileName = "StoryGraph", menuName = "EightAID/Story Graph/Story Graph Asset")]
    public class StoryGraphAsset : ScriptableObject
    {
        [Header("ストーリーグラフ情報")]
        [Tooltip("データ形式の版です。Importer はこの値を見て変換処理を判断します。")]
        [SerializeField] private int _formatVersion = 1;

        [Tooltip("グラフの用途や開始条件など、制作者向けの説明です。")]
        [SerializeField, TextArea(2, 5)] private string _overview;

        [Tooltip("グラフに含まれるノードです。ノードIDはグラフ内で一意にしてください。")]
        [SerializeField] private List<StoryNodeRecord> _nodes = new List<StoryNodeRecord>();

        public int FormatVersion => _formatVersion;
        public string Overview => _overview;
        public IReadOnlyList<StoryNodeRecord> Nodes => _nodes;

        public void ReplaceGraphData(
            int formatVersion,
            string overview,
            IEnumerable<StoryNodeRecord> nodes)
        {
            _formatVersion = formatVersion;
            _overview = overview;
            _nodes.Clear();
            if (nodes != null)
            {
                _nodes.AddRange(nodes);
            }
        }

        public StoryNodeRecord FindNode(string nodeId)
        {
            return _nodes.Find(node => node != null && node.Id == nodeId);
        }

        public void AddNode(StoryNodeRecord node)
        {
            if (node == null)
            {
                throw new System.ArgumentNullException(nameof(node));
            }

            if (FindNode(node.Id) != null)
            {
                throw new System.InvalidOperationException($"StoryGraph ノードIDが重複しています。id={node.Id}");
            }

            _nodes.Add(node);
        }

        public void RemoveNode(string nodeId)
        {
            _nodes.RemoveAll(node => node != null && node.Id == nodeId);
            foreach (StoryNodeRecord node in _nodes)
            {
                if (node == null)
                {
                    continue;
                }

                var remainingEdges = new List<StoryEdgeRecord>();
                foreach (StoryEdgeRecord edge in node.Edges)
                {
                    if (edge != null && edge.TargetNodeId != nodeId)
                    {
                        remainingEdges.Add(edge);
                    }
                }

                node.ReplaceEdges(remainingEdges);
            }
        }
    }
}
