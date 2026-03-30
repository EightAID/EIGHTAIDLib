using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeCanvasGroup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public async UniTask FadeIn(float duration, CancellationToken ct = default)
        {
            await FadeTo(1f, duration, ct);
            SetInteractable(true);
        }

        public async UniTask FadeOut(float duration, CancellationToken ct = default)
        {
            SetInteractable(false);
            await FadeTo(0f, duration, ct);
        }

        public void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            SetInteractable(visible);
        }

        private async UniTask FadeTo(float target, float duration, CancellationToken ct)
        {
            if (canvasGroup == null)
            {
                return;
            }

            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            canvasGroup.alpha = target;
        }

        private void SetInteractable(bool interactable)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }
}
