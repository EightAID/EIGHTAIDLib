using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// 会話送りに必要な入力待機処理を抽象化します。
    /// </summary>
    public interface IDialogueAdvanceInput
    {
        /// <summary>
        /// 前フレームから押されている入力が離されるまで待ちます。
        /// </summary>
        UniTask WaitForAdvanceInputReleaseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 次の 1 文字表示まで待機し、スキップ要求があれば true を返します。
        /// </summary>
        UniTask<bool> WaitForNextCharacterOrSkipAsync(
            float charIntervalSeconds,
            Func<bool> canSkipDuringTyping,
            CancellationToken cancellationToken);

        /// <summary>
        /// 表示完了後の confirm か auto 進行を待ちます。
        /// </summary>
        UniTask WaitForContinueAsync(
            Func<bool> canAcceptContinueInput,
            Func<bool> isAutoModeEnabled,
            float autoAdvanceDelaySeconds,
            bool useInputChannelForContinue,
            CancellationToken cancellationToken);

        /// <summary>
        /// 会話送りに使った confirm 入力を消費します。
        /// </summary>
        void ConsumeAdvance();
    }
}
