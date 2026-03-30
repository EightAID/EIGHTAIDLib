using TMPro;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// TextMeshPro の可視文字数を使って会話テキストを段階表示します。
    /// </summary>
    public sealed class DialogueTextPresenter
    {
        private readonly TextMeshProUGUI _displayText;
        private string _processedText = string.Empty;

        /// <summary>
        /// 現在の会話表示状態です。
        /// </summary>
        public DialogueState CurrentState { get; private set; } = DialogueState.Idle;

        /// <summary>
        /// 現在保持している加工済みテキストです。
        /// </summary>
        public string CurrentText => _processedText;

        /// <summary>
        /// 現在の総文字数です。
        /// </summary>
        public int TotalCharacterCount => _displayText != null ? _displayText.textInfo.characterCount : 0;

        /// <summary>
        /// テキスト表示対象を初期化します。
        /// </summary>
        public DialogueTextPresenter(TextMeshProUGUI displayText)
        {
            _displayText = displayText;
        }

        /// <summary>
        /// 表示テキストを初期化して 0 文字表示にします。
        /// </summary>
        public void Prepare(string text, IDialogueTextPreprocessor preprocessor)
        {
            if (_displayText == null)
            {
                return;
            }

            _processedText = (preprocessor ?? new DefaultDialogueTextPreprocessor()).Process(text);
            _displayText.text = _processedText;
            _displayText.maxVisibleCharacters = 0;
            _displayText.ForceMeshUpdate();
            SetState(DialogueState.Typing);
        }

        /// <summary>
        /// 次の 1 文字を表示します。
        /// </summary>
        public void RevealCharacters(int visibleCharacters)
        {
            if (_displayText == null)
            {
                return;
            }

            _displayText.maxVisibleCharacters = visibleCharacters;
        }

        /// <summary>
        /// 全文を一括表示します。
        /// </summary>
        public void RevealAll()
        {
            if (_displayText == null)
            {
                return;
            }

            _displayText.text = _processedText;
            _displayText.ForceMeshUpdate();
            _displayText.maxVisibleCharacters = _displayText.textInfo.characterCount;
        }

        /// <summary>
        /// テキスト表示を即座にクリアします。
        /// </summary>
        public void ClearImmediate()
        {
            _processedText = string.Empty;
            if (_displayText != null)
            {
                _displayText.text = string.Empty;
                _displayText.maxVisibleCharacters = 0;
            }

            SetState(DialogueState.Idle);
        }

        /// <summary>
        /// 表示状態を更新します。
        /// </summary>
        public void SetState(DialogueState state)
        {
            CurrentState = state;
        }
    }
}
