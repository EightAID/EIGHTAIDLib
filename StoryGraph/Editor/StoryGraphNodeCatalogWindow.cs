using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>登録済みの共通・固有ノードを確認するための StoryGraph Editor カタログです。</summary>
    public sealed class StoryGraphNodeCatalogWindow : EditorWindow
    {
        [MenuItem("Tools/EIGHTAID/Story/ノードカタログ")]
        private static void Open()
        {
            GetWindow<StoryGraphNodeCatalogWindow>("StoryGraph ノード");
        }

        private void OnGUI()
        {
            var definitions = StoryNodeEditorRegistry.GetDefinitions();

            EditorGUILayout.LabelField("登録済みノード", EditorStyles.boldLabel);
            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("ノード Provider が未登録です。プロジェクト側で StoryNodeEditorRegistry.Register を呼んでください。", MessageType.Info);
                return;
            }

            foreach (StoryNodeDefinition definition in definitions)
            {
                EditorGUILayout.LabelField($"{definition.Category} / {definition.DisplayName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(definition.Description, EditorStyles.wordWrappedLabel);
            }
        }
    }
}
