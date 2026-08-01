using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace EightAID.StoryGraph.Editor
{
    internal sealed class StoryGraphNodeView : Node
    {
        private const float NormalWidth = 240f;
        private const float CompactWidth = 180f;

        private readonly Action<StoryGraphNodeView> _onSelected;
        private readonly StoryNodeEditorPresentation _presentation;
        private readonly Label _summaryLabel;

        public StoryGraphNodeView(
            StoryNodeRecord record,
            StoryNodeDefinition definition,
            StoryNodeEditorPresentation presentation,
            Action<StoryGraphNodeView> onSelected)
        {
            Record = record;
            Definition = definition;
            _presentation = presentation;
            _onSelected = onSelected;
            title = definition.DisplayName;
            viewDataKey = record.Id;
            style.borderTopColor = definition.Color;
            style.borderTopWidth = 4f;

            Input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            Input.portName = "入力";
            inputContainer.Add(Input);

            foreach (StoryNodePortDefinition output in definition.OutputPorts)
            {
                Port port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                port.portName = output.DisplayName;
                port.userData = output.Role;
                outputContainer.Add(port);
                Outputs.Add(port);
            }

            _summaryLabel = new Label();
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            _summaryLabel.style.marginLeft = 6f;
            _summaryLabel.style.marginRight = 6f;
            _summaryLabel.style.marginTop = 4f;
            _summaryLabel.style.marginBottom = 4f;
            extensionContainer.Add(_summaryLabel);

            var compactButton = new Button(() => SetCompact(!IsCompact))
            {
                text = "−",
                tooltip = "ノードの表示をコンパクトに切り替えます。"
            };
            compactButton.style.width = 22f;
            titleButtonContainer.Add(compactButton);

            SetPosition(new Rect(record.EditorPosition, new Vector2(NormalWidth, 150f)));
            RefreshPresentation();
            RefreshExpandedState();
            RefreshPorts();
        }

        public StoryNodeRecord Record { get; }
        public StoryNodeDefinition Definition { get; }
        public Port Input { get; }
        public List<Port> Outputs { get; } = new List<Port>();
        public bool IsCompact { get; private set; }

        public override void OnSelected()
        {
            base.OnSelected();
            _onSelected?.Invoke(this);
        }

        public void RefreshPresentation()
        {
            string summary = _presentation?.GetSummary(Record.Payload) ?? string.Empty;
            _summaryLabel.text = CompactText(summary, IsCompact ? 54 : 140);
            _summaryLabel.tooltip = summary;
            _summaryLabel.style.display = string.IsNullOrWhiteSpace(summary) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetCompact(bool compact)
        {
            IsCompact = compact;
            Rect position = GetPosition();
            position.width = compact ? CompactWidth : NormalWidth;
            SetPosition(position);
            RefreshPresentation();
        }

        public bool MatchesSearch(string query, string scope)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            if (scope == "ノード種別")
            {
                return Definition.DisplayName.IndexOf(query, comparison) >= 0 ||
                       Definition.NodeTypeId.IndexOf(query, comparison) >= 0;
            }

            if (scope == "ノードID")
            {
                return Record.Id.IndexOf(query, comparison) >= 0;
            }

            bool matchesContent = (_presentation?.GetSearchTexts(Record.Payload) ?? Array.Empty<string>())
                .Any(text => !string.IsNullOrEmpty(text) && text.IndexOf(query, comparison) >= 0);
            if (scope == "本文・設定")
            {
                return matchesContent;
            }

            return matchesContent ||
                   Definition.DisplayName.IndexOf(query, comparison) >= 0 ||
                   Definition.NodeTypeId.IndexOf(query, comparison) >= 0 ||
                   Record.Id.IndexOf(query, comparison) >= 0;
        }

        public void SetSearchHighlighted(bool highlighted, bool current)
        {
            Color color = current
                ? new Color(1f, 0.72f, 0.12f)
                : highlighted ? new Color(0.25f, 0.72f, 1f) : Definition.Color;
            float width = highlighted || current ? 3f : 1f;
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderBottomColor = color;
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
            style.borderBottomWidth = width;
        }

        private static string CompactText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return compact.Length <= maxLength ? compact : compact.Substring(0, maxLength - 1) + "…";
        }
    }
}
