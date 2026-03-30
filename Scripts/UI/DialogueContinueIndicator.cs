using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// 会話の続き待ちを示すインジケーターを制御します。
    /// </summary>
    public sealed class DialogueContinueIndicator
    {
        private readonly Image _indicator;
        private readonly float _breathingAmount;
        private readonly float _breathingDuration;
        private Vector2 _baseAnchoredPosition;
        private bool _hasBaseAnchoredPosition;

        /// <summary>
        /// インジケーター制御を初期化します。
        /// </summary>
        public DialogueContinueIndicator(Image indicator, float breathingAmount = 10f, float breathingDuration = 1f)
        {
            _indicator = indicator;
            _breathingAmount = breathingAmount;
            _breathingDuration = breathingDuration;
        }

        /// <summary>
        /// インジケーターを表示または非表示にします。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_indicator == null)
            {
                return;
            }

            var color = _indicator.color;
            color.a = visible ? 1f : 0f;
            _indicator.color = color;

            if (!visible)
            {
                ResetAnimation();
            }
        }

        /// <summary>
        /// 待機中アニメーションを開始します。
        /// </summary>
        public void PlayWaitingAnimation()
        {
            if (_indicator == null)
            {
                return;
            }

            var rectTransform = _indicator.rectTransform;
            EnsureBasePosition(rectTransform);

            rectTransform.anchoredPosition = _baseAnchoredPosition;
            DOTween.Kill(rectTransform);
            rectTransform
                .DOAnchorPosY(_baseAnchoredPosition.y + _breathingAmount, _breathingDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(rectTransform);
        }

        /// <summary>
        /// アニメーションと位置を初期状態に戻します。
        /// </summary>
        public void ResetAnimation()
        {
            if (_indicator == null)
            {
                return;
            }

            var rectTransform = _indicator.rectTransform;
            DOTween.Kill(rectTransform);
            if (_hasBaseAnchoredPosition)
            {
                rectTransform.anchoredPosition = _baseAnchoredPosition;
            }
        }

        /// <summary>
        /// 基準位置を必要時に記録します。
        /// </summary>
        private void EnsureBasePosition(RectTransform rectTransform)
        {
            if (_hasBaseAnchoredPosition)
            {
                return;
            }

            _baseAnchoredPosition = rectTransform.anchoredPosition;
            _hasBaseAnchoredPosition = true;
        }
    }
}
