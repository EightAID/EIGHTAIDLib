using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace EightAID.StoryGraph.Editor
{
    internal sealed class StoryGraphNodeView : Node
    {
        private readonly Action<StoryGraphNodeView> _onSelected;

        public StoryGraphNodeView(
            StoryNodeRecord record,
            StoryNodeDefinition definition,
            Action<StoryGraphNodeView> onSelected)
        {
            Record = record;
            Definition = definition;
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

            SetPosition(new Rect(record.EditorPosition, new Vector2(240f, 150f)));
            RefreshExpandedState();
            RefreshPorts();
        }

        public StoryNodeRecord Record { get; }
        public StoryNodeDefinition Definition { get; }
        public Port Input { get; }
        public List<Port> Outputs { get; } = new List<Port>();

        public override void OnSelected()
        {
            base.OnSelected();
            _onSelected?.Invoke(this);
        }
    }
}
