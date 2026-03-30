using DG.Tweening;
using UnityEngine;

/// <summary>
/// Transform / RectTransform 向けの簡易シェイクユーティリティ。
/// </summary>
public static class TransformShakeUtility
{
    public static Tween ShakePosition(
        Transform target,
        float duration,
        float strength,
        int vibrato = 5,
        float randomness = 0f,
        bool snapping = false)
    {
        if (target == null)
        {
            return null;
        }

        var originalPosition = target.position;
        return target.DOShakePosition(duration, strength, vibrato, randomness, snapping)
            .OnComplete(() => target.position = originalPosition)
            .OnKill(() =>
            {
                if (target != null)
                {
                    target.position = originalPosition;
                }
            });
    }

    public static Tween ShakeAnchoredPosition(
        RectTransform target,
        float duration,
        float strength,
        int vibrato = 10,
        float randomness = 90f,
        bool snapping = false)
    {
        if (target == null)
        {
            return null;
        }

        var originalPosition = target.anchoredPosition;
        return target.DOShakeAnchorPos(duration, strength, vibrato, randomness, snapping)
            .OnComplete(() => target.anchoredPosition = originalPosition)
            .OnKill(() =>
            {
                if (target != null)
                {
                    target.anchoredPosition = originalPosition;
                }
            });
    }
}
