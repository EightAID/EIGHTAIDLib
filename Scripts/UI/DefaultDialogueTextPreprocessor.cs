namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// テキストを加工しない既定実装です。
    /// </summary>
    public sealed class DefaultDialogueTextPreprocessor : IDialogueTextPreprocessor
    {
        /// <summary>
        /// 元のテキストをそのまま返します。
        /// </summary>
        public string Process(string text)
        {
            return text ?? string.Empty;
        }
    }
}
