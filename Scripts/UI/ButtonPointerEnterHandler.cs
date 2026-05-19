using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.UI
{
    public class ButtonPointerEnterHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static Action<AudioClip> PlaySoundCallback;
        public static Action PlayDefaultSoundCallback;

        [Header("Hover Scale Settings")]
        [SerializeField] private bool useInitialScaleAsBase = true;
        [SerializeField] private float hoverScaleMultiplier = 1.08f;
        [SerializeField] private float tweenDuration = 0.12f;
        [SerializeField] private Ease ease = Ease.OutQuad;
        [SerializeField] private AudioClip hoverSound;

        private RectTransform _rectTransform;
        private Vector3 _cachedStartScale;
        private Tween _scaleTween;
        private Button _button;
        private bool _isPointerOver;
        private bool _isHoverScaled;
        private bool _hasCachedStartScale;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _button = GetComponent<Button>();

            if (_rectTransform == null)
            {
                Debug.LogError($"{nameof(ButtonPointerEnterHandler)} must be attached to a UI object with RectTransform: {name}");
            }
            else
            {
                CacheStartScale();
            }
        }

        private void Start()
        {
            CacheStartScale();
        }

        private bool IsInteractable() => _button == null || _button.interactable;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            if (!CanApplyHoverVisual())
            {
                return;
            }

            if (hoverSound != null)
            {
                PlaySoundCallback?.Invoke(hoverSound);
            }
            else
            {
                PlayDefaultSoundCallback?.Invoke();
            }

            _isHoverScaled = true;
            ScaleTo(_cachedStartScale * hoverScaleMultiplier);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            ResetScale();
        }

        public void ResetScale()
        {
            _isHoverScaled = false;
            ScaleTo(_cachedStartScale);
        }

        private bool CanApplyHoverVisual()
        {
            return IsInteractable();
        }

        private void ScaleTo(Vector3 targetScale)
        {
            if (_rectTransform == null)
            {
                return;
            }

            _scaleTween?.Kill();
            _scaleTween = _rectTransform
                .DOScale(targetScale, tweenDuration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private void OnDisable()
        {
            _rectTransform?.DOKill();
            if (_rectTransform != null)
            {
                if (!_hasCachedStartScale)
                {
                    CacheStartScale();
                }

                _rectTransform.localScale = _cachedStartScale;
            }

            _isPointerOver = false;
            _isHoverScaled = false;
        }

        private void CacheStartScale()
        {
            if (_rectTransform == null)
            {
                return;
            }

            if (!useInitialScaleAsBase)
            {
                _cachedStartScale = Vector3.one;
                _hasCachedStartScale = true;
                return;
            }

            _cachedStartScale = _rectTransform.localScale == Vector3.zero
                ? Vector3.one
                : _rectTransform.localScale;
            _hasCachedStartScale = true;
        }

        private void Update()
        {
            if (!_isPointerOver || !_isHoverScaled)
            {
                return;
            }

            if (CanApplyHoverVisual())
            {
                return;
            }

            ResetScale();
        }
    }
}
