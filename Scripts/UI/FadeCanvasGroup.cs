using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// CanvasGroup の alpha を UniTask でフェードイン/アウトする汎用コンポーネント。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class FadeCanvasGroup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>フェードイン（alpha 0 → 1）</summary>
    public async UniTask FadeIn(float duration, CancellationToken ct = default)
    {
        await FadeTo(1f, duration, ct);
        SetInteractable(true);
    }

    /// <summary>フェードアウト（alpha 1 → 0）し、インタラクションを無効化する</summary>
    public async UniTask FadeOut(float duration, CancellationToken ct = default)
    {
        SetInteractable(false);
        await FadeTo(0f, duration, ct);
    }

    /// <summary>即座に表示状態を切り替える</summary>
    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        SetInteractable(visible);
    }

    private async UniTask FadeTo(float target, float duration, CancellationToken ct)
    {
        if (canvasGroup == null) return;

        float start   = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ct.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        canvasGroup.alpha = target;
    }

    private void SetInteractable(bool interactable)
    {
        canvasGroup.interactable   = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }
}
