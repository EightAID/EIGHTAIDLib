namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// 会話表示前のテキスト加工を担当する抽象です。
    /// </summary>
    public interface IDialogueTextPreprocessor
    {
        /// <summary>
        /// 表示用テキストを加工して返します。
        /// </summary>
        string Process(string text);
    }
}
