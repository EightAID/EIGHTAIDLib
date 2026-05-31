using TMPro;
using TMP_Ruby;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// TextMeshPro の可視文字数を使って会話テキストを段階表示します。
    /// </summary>
    public sealed class DialogueTextPresenter
    {
        private readonly TextMeshProUGUI _displayText;
        private string _processedText = string.Empty;
        private int _preparedCharacterCount;

        /// <summary>
        /// 現在の会話表示状態です。
        /// </summary>
        public DialogueState CurrentState { get; private set; } = DialogueState.Idle;

        /// <summary>
        /// 現在保持している加工済みテキストです。
        /// </summary>
        public string CurrentText => _processedText;

        /// <summary>
        /// Prepare時に確定した総文字数です。
        /// </summary>
        public int TotalCharacterCount => _preparedCharacterCount;

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
            // TMPのメッシュ更新タイミングに依存しないように、受け取った文字列から先に可視文字数を確定します。
            _preparedCharacterCount = CountVisibleCharacters(_processedText);
            SetDisplayText(_processedText);
            _displayText.maxVisibleCharacters = 0;
            SetState(DialogueState.Typing);
        }

        /// <summary>
        /// 指定文字数まで表示します。
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

            SetDisplayText(_processedText);
            _displayText.maxVisibleCharacters = _preparedCharacterCount;
        }

        /// <summary>
        /// テキスト表示を即座にクリアします。
        /// </summary>
        public void ClearImmediate()
        {
            _processedText = string.Empty;
            _preparedCharacterCount = 0;
            if (_displayText != null)
            {
                SetDisplayText(string.Empty);
                _displayText.maxVisibleCharacters = 0;
            }

            SetState(DialogueState.Idle);
        }

        private void SetDisplayText(string text)
        {
            if (_displayText.TryGetComponent(out TextMeshProRuby rubyText))
            {
                rubyText.Text = text;
                return;
            }

            _displayText.text = text;
        }

        private static int CountVisibleCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 0;
            bool inRichTextTag = false;
            foreach (char character in text)
            {
                if (character == '<')
                {
                    inRichTextTag = true;
                    continue;
                }

                if (inRichTextTag)
                {
                    if (character == '>')
                    {
                        inRichTextTag = false;
                    }

                    continue;
                }

                count++;
            }

            return count;
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
