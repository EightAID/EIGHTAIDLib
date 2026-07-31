using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// 汎用 Message ノードの payload です。プロジェクト固有の文字置換は表示前に Adapter が行います。
    /// </summary>
    public sealed class StoryDialoguePayload
    {
        public int schemaVersion = 1;
        public string speakerName;
        public List<string> lines = new List<string>();
    }

    /// <summary>
    /// 会話表示を行う View の契約です。見た目や UI ライブラリには依存しません。
    /// </summary>
    public interface IDialogueView
    {
        Task ShowLineAsync(string speakerName, string line, CancellationToken cancellationToken);
        void Hide();
    }

    /// <summary>
    /// 会話の次送りを待つ入力実装の契約です。
    /// </summary>
    public interface IDialogueAdvanceInput
    {
        Task WaitForAdvanceAsync(CancellationToken cancellationToken);
    }
}
