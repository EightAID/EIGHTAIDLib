using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EightAID.EIGHTAIDLib.Input
{
    public static class InputChannel
    {
        private static readonly Dictionary<InputType, List<UniTaskCompletionSource>> Sources = new();

        public static async UniTask WaitAsync(InputType type, CancellationToken ct)
        {
            var source = new UniTaskCompletionSource();

            if (!Sources.TryGetValue(type, out var list))
            {
                list = new List<UniTaskCompletionSource>();
                Sources[type] = list;
            }

            list.Add(source);

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    if (Sources.TryGetValue(type, out var pending))
                    {
                        pending.Remove(source);
                    }

                    source.TrySetCanceled(ct);
                });
            }

            await source.Task;
        }

        public static async UniTask<InputType> WaitAnyAsync(CancellationToken ct, params InputType[] types)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var tasks = new UniTask[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                tasks[i] = WaitAsync(types[i], linked.Token);
            }

            var winIndex = await UniTask.WhenAny(tasks);
            linked.Cancel();
            return types[winIndex];
        }

        public static bool IsInputBlocked { get; set; }

        public static void Notify(InputType type)
        {
            if (IsInputBlocked && (type == InputType.Confirm || type == InputType.Skip))
            {
                return;
            }

            if (!Sources.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            var snapshot = new List<UniTaskCompletionSource>(list);
            list.Clear();

            foreach (var source in snapshot)
            {
                source.TrySetResult();
            }
        }
    }
}
