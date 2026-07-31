using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace EightAID.StoryGraph.Editor
{
    internal sealed class StoryGraphEditorView : GraphView
    {
        private readonly Action<StoryGraphNodeView> _onNodeSelected;
        private readonly Dictionary<string, StoryNodeDefinition> _definitions;
        private StoryGraphAsset _asset;
        private bool _isLoading;

        public StoryGraphEditorView(Action<StoryGraphNodeView> onNodeSelected)
        {
            _onNodeSelected = onNodeSelected;
            _definitions = StoryNodeEditorRegistry.GetDefinitions()
                .ToDictionary(definition => definition.NodeTypeId);

            style.flexGrow = 1f;
            Insert(0, new GridBackground());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = HandleGraphViewChanged;
        }

        public void Load(StoryGraphAsset asset)
        {
            _isLoading = true;
            try
            {
                _asset = asset;
                DeleteElements(graphElements.ToList());
                if (_asset == null)
                {
                    return;
                }

                var views = new Dictionary<string, StoryGraphNodeView>();
                foreach (StoryNodeRecord record in _asset.Nodes)
                {
                    if (record == null ||
                        string.IsNullOrWhiteSpace(record.Id) ||
                        views.ContainsKey(record.Id) ||
                        !_definitions.TryGetValue(record.NodeTypeId, out StoryNodeDefinition definition))
                    {
                        continue;
                    }

                    StoryGraphNodeView view = CreateView(record, definition);
                    views.Add(record.Id, view);
                }

                foreach (StoryNodeRecord record in _asset.Nodes)
                {
                    if (record == null || !views.TryGetValue(record.Id, out StoryGraphNodeView source))
                    {
                        continue;
                    }

                    foreach (StoryEdgeRecord edgeRecord in record.Edges)
                    {
                        if (edgeRecord == null || !views.TryGetValue(edgeRecord.TargetNodeId, out StoryGraphNodeView target))
                        {
                            continue;
                        }

                        Port output = source.Outputs.FirstOrDefault(port =>
                            port.userData is StoryEdgeRole role && role == edgeRecord.Role);
                        if (output == null)
                        {
                            continue;
                        }

                        Edge edge = output.ConnectTo(target.Input);
                        AddElement(edge);
                    }
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (_asset == null)
            {
                return;
            }

            Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            foreach (IGrouping<string, StoryNodeDefinition> group in _definitions.Values.GroupBy(item => item.Category))
            {
                foreach (StoryNodeDefinition definition in group)
                {
                    StoryNodeDefinition captured = definition;
                    evt.menu.AppendAction(
                        $"{group.Key}/{captured.DisplayName}",
                        _ => AddNode(captured, graphPosition));
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(port =>
                port != startPort &&
                port.node != startPort.node &&
                port.direction != startPort.direction).ToList();
        }

        private void AddNode(StoryNodeDefinition definition, Vector2 position)
        {
            Undo.RecordObject(_asset, "StoryGraph ノード追加");
            var record = new StoryNodeRecord(
                Guid.NewGuid().ToString("N"),
                definition.NodeTypeId,
                definition.PayloadFactory(),
                position);
            _asset.AddNode(record);
            EditorUtility.SetDirty(_asset);
            CreateView(record, definition);
        }

        private StoryGraphNodeView CreateView(StoryNodeRecord record, StoryNodeDefinition definition)
        {
            var view = new StoryGraphNodeView(record, definition, _onNodeSelected);
            AddElement(view);
            return view;
        }

        private GraphViewChange HandleGraphViewChanged(GraphViewChange change)
        {
            if (_asset == null || _isLoading)
            {
                return change;
            }

            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                Undo.RecordObject(_asset, "StoryGraph ノード移動");
                foreach (StoryGraphNodeView node in change.movedElements.OfType<StoryGraphNodeView>())
                {
                    node.Record.SetEditorPosition(node.GetPosition().position);
                }
                EditorUtility.SetDirty(_asset);
            }

            if (change.elementsToRemove != null)
            {
                Undo.RecordObject(_asset, "StoryGraph 要素削除");
                foreach (StoryGraphNodeView node in change.elementsToRemove.OfType<StoryGraphNodeView>())
                {
                    _asset.RemoveNode(node.Record.Id);
                }
            }

            if ((change.edgesToCreate != null && change.edgesToCreate.Count > 0) ||
                (change.elementsToRemove != null && change.elementsToRemove.OfType<Edge>().Any()))
            {
                EditorApplication.delayCall += SaveEdges;
            }

            EditorUtility.SetDirty(_asset);
            return change;
        }

        private void SaveEdges()
        {
            if (_asset == null)
            {
                return;
            }

            Undo.RecordObject(_asset, "StoryGraph 接続変更");
            foreach (StoryGraphNodeView source in nodes.OfType<StoryGraphNodeView>())
            {
                var records = new List<StoryEdgeRecord>();
                foreach (Port output in source.Outputs)
                {
                    StoryEdgeRole role = output.userData is StoryEdgeRole value ? value : StoryEdgeRole.Next;
                    foreach (Edge edge in output.connections)
                    {
                        if (edge.input?.node is StoryGraphNodeView target)
                        {
                            StoryEdgeRecord existing = source.Record.Edges.FirstOrDefault(candidate =>
                                candidate != null &&
                                candidate.TargetNodeId == target.Record.Id &&
                                candidate.Role == role);
                            records.Add(existing == null
                                ? new StoryEdgeRecord(target.Record.Id, role)
                                : new StoryEdgeRecord(
                                    target.Record.Id,
                                    role,
                                    existing.HasManualRoute,
                                    existing.ManualRouteX,
                                    existing.RoutePoints));
                        }
                    }
                }
                source.Record.ReplaceEdges(records);
            }
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);
        }
    }
}
