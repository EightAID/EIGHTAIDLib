using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>派生StoryGraphAssetをプロジェクト固有エディタへ転送する契約です。</summary>
    public interface IStoryGraphAssetEditor
    {
        int Priority { get; }
        bool CanOpen(StoryGraphAsset asset);
        void Open(StoryGraphAsset asset);
    }

    public static class StoryGraphAssetEditorRegistry
    {
        public static IReadOnlyList<IStoryGraphAssetEditor> GetEditors()
        {
            var editors = new List<IStoryGraphAssetEditor>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IStoryGraphAssetEditor>())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is IStoryGraphAssetEditor editor)
                {
                    editors.Add(editor);
                }
            }

            return editors.OrderByDescending(editor => editor.Priority).ToArray();
        }

        public static IStoryGraphAssetEditor FindEditor(StoryGraphAsset asset)
        {
            return asset == null
                ? null
                : GetEditors().FirstOrDefault(editor => editor.CanOpen(asset));
        }

        public static bool TryOpen(StoryGraphAsset asset)
        {
            IStoryGraphAssetEditor editor = FindEditor(asset);
            if (editor == null)
            {
                return false;
            }

            editor.Open(asset);
            return true;
        }
    }
}
