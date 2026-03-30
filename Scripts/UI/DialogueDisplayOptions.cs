using System;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// 会話の 1 行表示に必要なオプションをまとめます。
    /// </summary>
    public sealed class DialogueDisplayOptions
    {
        /// <summary>
        /// 1 文字ごとの表示間隔を秒で指定します。
        /// </summary>
        public float CharacterIntervalSeconds { get; set; } = 0.05f;

        /// <summary>
        /// オート送り時に次へ進むまでの待機時間を秒で指定します。
        /// </summary>
        public float AutoAdvanceDelaySeconds { get; set; } = 2f;

        /// <summary>
        /// 表示完了後に入力待ちを行うかどうかを指定します。
        /// </summary>
        public bool WaitForContinueInput { get; set; } = true;

        /// <summary>
        /// 文字送り中にスキップ入力を許可するかどうかを返します。
        /// </summary>
        public Func<bool> CanSkipDuringTyping { get; set; }

        /// <summary>
        /// 表示完了後の入力受付を開始してよいかどうかを返します。
        /// </summary>
        public Func<bool> CanAcceptContinueInput { get; set; }

        /// <summary>
        /// オート送りが有効かどうかを返します。
        /// </summary>
        public Func<bool> IsAutoModeEnabled { get; set; }

        /// <summary>
        /// Confirm 待機を InputChannel ベースで行うかどうかを指定します。
        /// </summary>
        public bool UseInputChannelForContinue { get; set; }

        /// <summary>
        /// 既定の設定を返します。
        /// </summary>
        public static DialogueDisplayOptions Default => new DialogueDisplayOptions();
    }
}
