using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>
    /// StoryGraph Editor で表示するノード定義の登録先です。
    /// Provider は TypeCache から自動検出されるため、プロジェクト側の初期化コードは不要です。
    /// </summary>
    public static class StoryNodeEditorRegistry
    {
        private static readonly List<IStoryNodeEditorProvider> Providers = new List<IStoryNodeEditorProvider>();

        public static void Register(IStoryNodeEditorProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (!Providers.Contains(provider))
            {
                Providers.Add(provider);
            }
        }

        public static IReadOnlyList<IStoryNodeEditorProvider> GetProviders()
        {
            var providers = new List<IStoryNodeEditorProvider>
            {
                new BuiltInStoryNodeEditorProvider()
            };

            providers.AddRange(Providers);
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IStoryNodeEditorProvider>())
            {
                if (type.IsAbstract || type.IsInterface || type == typeof(BuiltInStoryNodeEditorProvider))
                {
                    continue;
                }

                if (providers.Any(provider => provider.GetType() == type))
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is IStoryNodeEditorProvider provider)
                {
                    providers.Add(provider);
                }
            }

            return providers;
        }

        public static IReadOnlyList<StoryNodeDefinition> GetDefinitions()
        {
            return GetProviders()
                .OrderBy(provider => provider.Priority)
                .SelectMany(provider => provider.GetDefinitions() ?? Array.Empty<StoryNodeDefinition>())
                .GroupBy(definition => definition.NodeTypeId)
                .Select(group => group.Last())
                .OrderBy(definition => definition.Category)
                .ThenBy(definition => definition.DisplayName)
                .ToArray();
        }
    }
}
