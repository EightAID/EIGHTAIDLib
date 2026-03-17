using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace InputSystem
{
    /// <summary>
    /// 静的な入力待ちチャンネル。
    /// InputSystemBase から Notify() で通知し、各所で WaitAsync() で待つ。
    /// </summary>
    public static class InputChannel
    {
        private static readonly Dictionary<InputType, List<UniTaskCompletionSource>> _sources = new();

        /// <summary>
        /// 特定の入力を待つ
        /// </summary>
        public static async UniTask WaitAsync(InputType type, CancellationToken ct)
        {
            var src = new UniTaskCompletionSource();

            if (!_sources.TryGetValue(type, out var list))
            {
                list = new List<UniTaskCompletionSource>();
                _sources[type] = list;
            }
            list.Add(src);

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    if (_sources.TryGetValue(type, out var l))
                        l.Remove(src);
                    src.TrySetCanceled(ct);
                });
            }

            await src.Task;
        }

        /// <summary>
        /// 複数のうちいずれかの入力を待ち、どの入力が来たかを返す
        /// </summary>
        public static async UniTask<InputType> WaitAnyAsync(CancellationToken ct, params InputType[] types)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var tasks = new UniTask[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                tasks[i] = WaitAsync(types[i], linked.Token);
            }

            var winIndex = await UniTask.WhenAny(tasks);
            linked.Cancel(); // 残りのタスクをキャンセル
            return types[winIndex];
        }

        /// <summary>
        /// true の間は Confirm / Skip の通知を遮断する（メニュー・パネル表示中に使用）
        /// </summary>
        public static bool IsInputBlocked { get; set; }

        /// <summary>
        /// InputSystemBase から呼ぶ通知メソッド。
        /// 該当 InputType を待っている全タスクを完了させる。
        /// </summary>
        public static void Notify(InputType type)
        {
            if (IsInputBlocked && (type == InputType.Confirm || type == InputType.Skip))
                return;

            if (!_sources.TryGetValue(type, out var list) || list.Count == 0)
                return;

            // リストをコピーしてからイテレート（TrySetResult 内で変更される可能性があるため）
            var snapshot = new List<UniTaskCompletionSource>(list);
            list.Clear();

            foreach (var src in snapshot)
            {
                src.TrySetResult();
            }
        }
    }
}
