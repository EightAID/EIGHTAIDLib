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

        [Header("Hover Scale Settings")]
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

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _button = GetComponent<Button>();

            if (_rectTransform == null)
            {
                Debug.LogError($"{nameof(ButtonPointerEnterHandler)} must be attached to a UI object with RectTransform: {name}");
            }
        }

        private void Start()
        {
            if (_rectTransform != null)
            {
                _cachedStartScale = _rectTransform.localScale;
            }
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
            if (!IsInteractable())
            {
                return false;
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return true;
            }

            var selected = eventSystem.currentSelectedGameObject;
            return selected == null || selected == gameObject;
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
                _rectTransform.localScale = _cachedStartScale;
            }

            _isPointerOver = false;
            _isHoverScaled = false;
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
