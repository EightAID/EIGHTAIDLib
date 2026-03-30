using System.Threading;
using Cysharp.Threading.Tasks;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// 1 行の会話表示フロー全体を調停します。
    /// </summary>
    public sealed class DialoguePresenterController
    {
        private readonly DialogueTextPresenter _textPresenter;
        private readonly DialogueContinueIndicator _continueIndicator;
        private IDialogueAdvanceInput _inputSource;
        private IDialogueTextPreprocessor _textPreprocessor;

        /// <summary>
        /// 会話表示コントローラーを初期化します。
        /// </summary>
        public DialoguePresenterController(
            DialogueTextPresenter textPresenter,
            DialogueContinueIndicator continueIndicator,
            IDialogueAdvanceInput inputSource,
            IDialogueTextPreprocessor textPreprocessor = null)
        {
            _textPresenter = textPresenter;
            _continueIndicator = continueIndicator;
            _inputSource = inputSource;
            _textPreprocessor = textPreprocessor ?? new DefaultDialogueTextPreprocessor();
        }

        /// <summary>
        /// 入力取得実装を差し替えます。
        /// </summary>
        public void SetInputSource(IDialogueAdvanceInput inputSource)
        {
            _inputSource = inputSource;
        }

        /// <summary>
        /// テキスト前処理実装を差し替えます。
        /// </summary>
        public void SetTextPreprocessor(IDialogueTextPreprocessor textPreprocessor)
        {
            _textPreprocessor = textPreprocessor ?? new DefaultDialogueTextPreprocessor();
        }

        /// <summary>
        /// 1 行の会話を表示し、必要なら続き入力まで待ちます。
        /// </summary>
        public async UniTask DisplayAsync(string text, DialogueDisplayOptions options, CancellationToken cancellationToken = default)
        {
            if (_textPresenter == null)
            {
                return;
            }

            options ??= DialogueDisplayOptions.Default;

            _continueIndicator?.SetVisible(false);
            _textPresenter.Prepare(text, _textPreprocessor);

            if (_inputSource != null)
            {
                await _inputSource.WaitForAdvanceInputReleaseAsync(cancellationToken);
            }

            await RunTypingAsync(options, cancellationToken);

            if (!options.WaitForContinueInput)
            {
                _textPresenter.SetState(DialogueState.Completed);
                return;
            }

            _textPresenter.SetState(DialogueState.WaitingForContinue);
            _continueIndicator?.SetVisible(true);
            _continueIndicator?.PlayWaitingAnimation();

            if (_inputSource != null)
            {
                await _inputSource.WaitForContinueAsync(
                    options.CanAcceptContinueInput,
                    options.IsAutoModeEnabled,
                    options.AutoAdvanceDelaySeconds,
                    options.UseInputChannelForContinue,
                    cancellationToken);
                _inputSource.ConsumeAdvance();
            }

            _continueIndicator?.SetVisible(false);
            _textPresenter.SetState(DialogueState.Completed);
        }

        /// <summary>
        /// 文字送り部分だけを実行します。
        /// </summary>
        private async UniTask RunTypingAsync(DialogueDisplayOptions options, CancellationToken cancellationToken)
        {
            int totalCharacters = _textPresenter.TotalCharacterCount;
            for (int visibleCharacters = 0; visibleCharacters < totalCharacters; visibleCharacters++)
            {
                _textPresenter.RevealCharacters(visibleCharacters + 1);

                bool shouldRevealAll = false;
                if (_inputSource != null)
                {
                    shouldRevealAll = await _inputSource.WaitForNextCharacterOrSkipAsync(
                        options.CharacterIntervalSeconds,
                        options.CanSkipDuringTyping,
                        cancellationToken);
                }
                else
                {
                    await UniTask.Delay((int)(options.CharacterIntervalSeconds * 1000f), cancellationToken: cancellationToken);
                }

                if (shouldRevealAll)
                {
                    _textPresenter.RevealAll();
                    break;
                }
            }
        }
    }
}
