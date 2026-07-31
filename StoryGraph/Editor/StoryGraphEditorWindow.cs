using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>
    /// StoryGraphAsset を作成・接続・編集する共通 Graph Editor です。
    /// </summary>
    public sealed class StoryGraphEditorWindow : EditorWindow
    {
        private StoryGraphEditorView _graphView;
        private VisualElement _inspector;
        private ObjectField _assetField;
        private StoryGraphAsset _asset;

        [MenuItem("Tools/EIGHTAIDLib/StoryGraph/グラフエディタ")]
        private static void Open()
        {
            GetWindow<StoryGraphEditorWindow>("StoryGraph");
        }

        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();
            _assetField = new ObjectField("対象")
            {
                objectType = typeof(StoryGraphAsset),
                allowSceneObjects = false
            };
            _assetField.RegisterValueChangedCallback(evt => SetAsset(evt.newValue as StoryGraphAsset));
            toolbar.Add(_assetField);
            rootVisualElement.Add(toolbar);

            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexGrow = 1f;
            rootVisualElement.Add(content);

            _graphView = new StoryGraphEditorView(ShowNodeInspector);
            _graphView.style.flexGrow = 1f;
            content.Add(_graphView);

            _inspector = new ScrollView();
            _inspector.style.width = 340f;
            _inspector.style.paddingLeft = 8f;
            _inspector.style.paddingRight = 8f;
            content.Add(_inspector);
        }

        private void SetAsset(StoryGraphAsset asset)
        {
            _asset = asset;
            _graphView.Load(asset);
            _inspector.Clear();
        }

        private void ShowNodeInspector(StoryGraphNodeView nodeView)
        {
            _inspector.Clear();
            if (_asset == null || nodeView == null)
            {
                return;
            }

            var title = new Label(nodeView.Definition.DisplayName);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16f;
            _inspector.Add(title);
            _inspector.Add(new HelpBox(nodeView.Definition.Description, HelpBoxMessageType.Info));

            var serializedObject = new SerializedObject(_asset);
            SerializedProperty nodesProperty = serializedObject.FindProperty("_nodes");
            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);
                if (nodeProperty.FindPropertyRelative("_id").stringValue != nodeView.Record.Id)
                {
                    continue;
                }

                PropertyField payloadField = new PropertyField(nodeProperty.FindPropertyRelative("_payload"), "設定");
                payloadField.Bind(serializedObject);
                _inspector.Add(payloadField);
                break;
            }
        }
    }
}
