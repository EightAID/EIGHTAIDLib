using System;
using System.Collections.Generic;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// ノード定義と実行 Handler の登録先です。
    /// Core はプロジェクト固有の型を参照せず、このレジストリ経由で機能を拡張します。
    /// </summary>
    public sealed class StoryGraphRegistry
    {
        private readonly Dictionary<string, StoryNodeDefinition> _definitions = new Dictionary<string, StoryNodeDefinition>();
        private readonly Dictionary<string, IStoryNodeHandler> _handlers = new Dictionary<string, IStoryNodeHandler>();

        public IReadOnlyCollection<StoryNodeDefinition> Definitions => _definitions.Values;

        public void Register(StoryNodeDefinition definition, IStoryNodeHandler handler)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrWhiteSpace(definition.NodeTypeId))
            {
                throw new ArgumentException("nodeTypeId を空にはできません。", nameof(definition));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (_definitions.ContainsKey(definition.NodeTypeId))
            {
                throw new InvalidOperationException($"StoryGraph ノードは既に登録されています。nodeTypeId={definition.NodeTypeId}");
            }

            _definitions.Add(definition.NodeTypeId, definition);
            _handlers.Add(definition.NodeTypeId, handler);
        }

        public bool TryGetDefinition(string nodeTypeId, out StoryNodeDefinition definition)
        {
            return _definitions.TryGetValue(nodeTypeId, out definition);
        }

        public bool TryGetHandler(string nodeTypeId, out IStoryNodeHandler handler)
        {
            return _handlers.TryGetValue(nodeTypeId, out handler);
        }
    }
}
