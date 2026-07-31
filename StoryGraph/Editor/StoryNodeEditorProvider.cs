using System.Collections.Generic;

namespace EightAID.StoryGraph.Editor
{
    /// <summary>
    /// プロジェクト固有ノードを共通 StoryGraph Editor へ登録する契約です。
    /// 共通 package はプロジェクト固有のノード型を参照しません。
    /// </summary>
    public interface IStoryNodeEditorProvider
    {
        int Priority { get; }
        IEnumerable<StoryNodeDefinition> GetDefinitions();
    }
}
