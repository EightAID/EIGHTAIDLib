using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// ノード固有データの共通契約です。
    /// 実装型は [Serializable] を付け、StoryNodeRecord の SerializeReference に保存します。
    /// </summary>
    public interface IStoryNodePayload
    {
        int SchemaVersion { get; }
    }

    /// <summary>
    /// ノードの入出力ポート定義です。
    /// </summary>
    public readonly struct StoryNodePortDefinition
    {
        public StoryNodePortDefinition(string displayName, StoryEdgeRole role)
        {
            DisplayName = displayName;
            Role = role;
        }

        public string DisplayName { get; }
        public StoryEdgeRole Role { get; }
    }

    /// <summary>
    /// プロジェクト側が登録するノードの宣言です。
    /// PayloadFactory が返す型を共通 Editor が SerializedProperty として自動描画します。
    /// </summary>
    public sealed class StoryNodeDefinition
    {
        public StoryNodeDefinition(
            string nodeTypeId,
            string displayName,
            string category,
            string description,
            Func<IStoryNodePayload> payloadFactory,
            StoryNodePortDefinition[] outputPorts = null,
            Color? color = null)
        {
            if (string.IsNullOrWhiteSpace(nodeTypeId))
            {
                throw new ArgumentException("nodeTypeId を空にはできません。", nameof(nodeTypeId));
            }

            NodeTypeId = nodeTypeId;
            DisplayName = displayName;
            Category = category;
            Description = description;
            PayloadFactory = payloadFactory ?? throw new ArgumentNullException(nameof(payloadFactory));
            OutputPorts = outputPorts ?? new[] { new StoryNodePortDefinition("次へ", StoryEdgeRole.Next) };
            Color = color ?? new Color(0.24f, 0.3f, 0.42f);
        }

        public string NodeTypeId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Description { get; }
        public Func<IStoryNodePayload> PayloadFactory { get; }
        public IReadOnlyList<StoryNodePortDefinition> OutputPorts { get; }
        public Color Color { get; }
    }
}
