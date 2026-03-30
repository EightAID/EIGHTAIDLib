using System.Threading;
using Coffee.UIEffects;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UIEffect の transitionRate を UniTask で補間する共通ユーティリティ。
/// </summary>
public static class UIEffectTransitionUtility
{
    public static async UniTask AnimateAsync(
        GameObject transitionObject,
        UIEffect transitionEffect,
        float from,
        float to,
        float duration,
        CancellationToken cancellationToken = default)
    {
        if (transitionObject == null || transitionEffect == null)
        {
            return;
        }

        transitionObject.SetActive(true);
        transitionEffect.transitionRate = from;

        if (duration <= 0f)
        {
            transitionEffect.transitionRate = to;
            if (to >= 1f)
            {
                transitionObject.SetActive(false);
            }

            return;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            transitionEffect.transitionRate = Mathf.Lerp(from, to, t);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        transitionEffect.transitionRate = to;
        if (to >= 1f)
        {
            transitionObject.SetActive(false);
        }
    }
}
