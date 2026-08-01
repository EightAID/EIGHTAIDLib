using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace EightAID.StoryGraph.Editor
{
    public sealed class StoryNodeEditorPresentation
    {
        public StoryNodeEditorPresentation(
            string nodeTypeId,
            Func<IStoryNodePayload, string> summaryFactory = null,
            Func<IStoryNodePayload, IEnumerable<string>> searchTextFactory = null,
            Action<StoryNodeInspectorContext> inspectorFactory = null)
        {
            NodeTypeId = nodeTypeId;
            SummaryFactory = summaryFactory;
            SearchTextFactory = searchTextFactory;
            InspectorFactory = inspectorFactory;
        }

        public string NodeTypeId { get; }
        public Func<IStoryNodePayload, string> SummaryFactory { get; }
        public Func<IStoryNodePayload, IEnumerable<string>> SearchTextFactory { get; }
        public Action<StoryNodeInspectorContext> InspectorFactory { get; }

        public string GetSummary(IStoryNodePayload payload)
        {
            return SummaryFactory?.Invoke(payload) ?? string.Empty;
        }

        public IEnumerable<string> GetSearchTexts(IStoryNodePayload payload)
        {
            return SearchTextFactory?.Invoke(payload) ?? Array.Empty<string>();
        }
    }

    public sealed class StoryNodeInspectorContext
    {
        public StoryNodeInspectorContext(
            StoryGraphAsset asset,
            StoryNodeRecord record,
            SerializedObject serializedAsset,
            SerializedProperty payloadProperty,
            VisualElement container)
        {
            Asset = asset;
            Record = record;
            SerializedAsset = serializedAsset;
            PayloadProperty = payloadProperty;
            Container = container;
        }

        public StoryGraphAsset Asset { get; }
        public StoryNodeRecord Record { get; }
        public SerializedObject SerializedAsset { get; }
        public SerializedProperty PayloadProperty { get; }
        public VisualElement Container { get; }

        public void AddProperty(string relativeName, string label = null)
        {
            SerializedProperty property = PayloadProperty?.FindPropertyRelative(relativeName);
            if (property == null)
            {
                return;
            }

            var field = string.IsNullOrEmpty(label)
                ? new PropertyField(property)
                : new PropertyField(property, label);
            field.Bind(SerializedAsset);
            Container.Add(field);
        }

        public void AddDefaultInspector(string label = "設定")
        {
            if (PayloadProperty == null)
            {
                return;
            }

            var field = new PropertyField(PayloadProperty, label);
            field.Bind(SerializedAsset);
            Container.Add(field);
        }
    }

    public interface IStoryNodeEditorPresentationProvider
    {
        int Priority { get; }
        IEnumerable<StoryNodeEditorPresentation> GetPresentations();
    }
}
