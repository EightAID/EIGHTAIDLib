using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>StoryGraphAssetを作成・検索・接続・編集する共通Graph Editorです。</summary>
    public sealed class StoryGraphEditorWindow : EditorWindow
    {
        private const string CompactModePrefsKey = "EightAID.StoryGraph.CompactMode";

        private readonly List<StoryGraphNodeView> _searchResults = new List<StoryGraphNodeView>();
        private StoryGraphEditorView _graphView;
        private VisualElement _inspector;
        private ObjectField _assetField;
        private TextField _searchField;
        private PopupField<string> _searchScopeField;
        private Label _searchStatusLabel;
        private StoryGraphAsset _asset;
        private int _searchIndex = -1;
        private bool _compactMode;

        [MenuItem("Tools/EIGHTAID/Story/グラフエディタ")]
        private static void Open()
        {
            GetWindow<StoryGraphEditorWindow>("StoryGraph");
        }

        public void CreateGUI()
        {
            _compactMode = EditorPrefs.GetBool(CompactModePrefsKey, true);
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            CreateToolbar();
            CreateContent();
        }

        private void CreateToolbar()
        {
            var toolbar = new Toolbar();
            _assetField = new ObjectField("対象")
            {
                objectType = typeof(StoryGraphAsset),
                allowSceneObjects = false
            };
            _assetField.style.minWidth = 240f;
            _assetField.RegisterValueChangedCallback(evt => SetAsset(evt.newValue as StoryGraphAsset));
            toolbar.Add(_assetField);
            toolbar.Add(new Button(SaveAsset) { text = "保存" });
            toolbar.Add(new Button(() => _graphView.FrameRoot()) { text = "開始へ" });

            var compactToggle = new ToolbarToggle
            {
                text = "コンパクト",
                tooltip = "全ノードの詳細表示を切り替えます。",
                value = _compactMode
            };
            compactToggle.RegisterValueChangedCallback(evt =>
            {
                _compactMode = evt.newValue;
                EditorPrefs.SetBool(CompactModePrefsKey, _compactMode);
                _graphView.SetCompact(_compactMode);
            });
            toolbar.Add(compactToggle);
            toolbar.Add(new Button(() => _graphView.ZoomBy(1.1f)) { text = "+" });
            toolbar.Add(new Button(() => _graphView.ZoomBy(0.9f)) { text = "−" });

            _searchField = new TextField { tooltip = "本文・設定・ノード種別・IDから検索します。" };
            _searchField.style.width = 220f;
            _searchField.RegisterValueChangedCallback(_ => RebuildSearchResults(true));
            toolbar.Add(_searchField);

            _searchScopeField = new PopupField<string>(
                new List<string> { "すべて", "本文・設定", "ノード種別", "ノードID" },
                0);
            _searchScopeField.style.width = 110f;
            _searchScopeField.RegisterValueChangedCallback(_ => RebuildSearchResults(true));
            toolbar.Add(_searchScopeField);
            toolbar.Add(new Button(() => MoveSearch(-1)) { text = "前へ" });
            toolbar.Add(new Button(() => MoveSearch(1)) { text = "次へ" });
            toolbar.Add(new Button(ClearSearch) { text = "クリア" });
            _searchStatusLabel = new Label("0 / 0");
            _searchStatusLabel.style.minWidth = 48f;
            _searchStatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            toolbar.Add(_searchStatusLabel);
            rootVisualElement.Add(toolbar);
        }

        private void CreateContent()
        {
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
            if (asset != null && StoryGraphAssetEditorRegistry.TryOpen(asset))
            {
                _assetField.SetValueWithoutNotify(null);
                _asset = null;
                _graphView.Load(null);
                _inspector.Clear();
                Close();
                return;
            }

            _asset = asset;
            _graphView.Load(asset);
            _graphView.SetCompact(_compactMode);
            _inspector.Clear();
            ClearSearch();
        }

        private void SaveAsset()
        {
            if (_asset != null)
            {
                AssetDatabase.SaveAssetIfDirty(_asset);
            }
        }

        private void RebuildSearchResults(bool focusFirst)
        {
            _searchResults.Clear();
            _searchResults.AddRange(_graphView.FindNodes(_searchField.value, _searchScopeField.value));
            _searchIndex = _searchResults.Count > 0 ? 0 : -1;
            if (focusFirst && _searchIndex >= 0)
            {
                _graphView.SelectSearchResult(_searchResults, _searchIndex);
            }
            UpdateSearchStatus();
        }

        private void MoveSearch(int direction)
        {
            if (_searchResults.Count == 0)
            {
                return;
            }

            _searchIndex = (_searchIndex + direction + _searchResults.Count) % _searchResults.Count;
            _graphView.SelectSearchResult(_searchResults, _searchIndex);
            UpdateSearchStatus();
        }

        private void ClearSearch()
        {
            if (_searchField != null)
            {
                _searchField.SetValueWithoutNotify(string.Empty);
            }
            _searchResults.Clear();
            _searchIndex = -1;
            _graphView.ClearSearchHighlights();
            UpdateSearchStatus();
        }

        private void UpdateSearchStatus()
        {
            if (_searchStatusLabel != null)
            {
                _searchStatusLabel.text = _searchIndex >= 0
                    ? $"{_searchIndex + 1} / {_searchResults.Count}"
                    : $"0 / {_searchResults.Count}";
            }
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
            SerializedProperty payloadProperty = FindPayloadProperty(nodesProperty, nodeView.Record.Id);
            if (payloadProperty == null)
            {
                return;
            }

            StoryNodeEditorPresentation presentation = StoryNodeEditorRegistry.GetPresentations()
                .LastOrDefault(item => item.NodeTypeId == nodeView.Record.NodeTypeId);
            var context = new StoryNodeInspectorContext(
                _asset,
                nodeView.Record,
                serializedObject,
                payloadProperty,
                _inspector);
            if (presentation?.InspectorFactory != null)
            {
                presentation.InspectorFactory(context);
            }
            else
            {
                context.AddDefaultInspector();
            }
        }

        private static SerializedProperty FindPayloadProperty(SerializedProperty nodesProperty, string nodeId)
        {
            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);
                if (nodeProperty.FindPropertyRelative("_id").stringValue == nodeId)
                {
                    return nodeProperty.FindPropertyRelative("_payload");
                }
            }
            return null;
        }
    }
}
