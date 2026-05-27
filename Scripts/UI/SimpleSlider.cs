using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.UI
{
    [AddComponentMenu("EightAID/UI/Simple Slider")]
    [RequireComponent(typeof(RectTransform))]
    public class SimpleSlider : Selectable, IDragHandler
    {
        [Serializable]
        public class SliderValueChangedEvent : UnityEvent<float>
        {
        }

        public enum DisplayMappingMode
        {
            Linear,
            Exponent,
            Curve
        }

        [Header("Parts")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image handleImage;

        [Header("Value")]
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 1f;
        [SerializeField] private float value;
        [SerializeField] private bool wholeNumbers;
        [SerializeField] private float step = 0.05f;

        [Header("Display Mapping")]
        [SerializeField] private DisplayMappingMode displayMappingMode = DisplayMappingMode.Linear;
        [SerializeField, Min(0.01f)] private float displayExponent = 1f;
        [SerializeField] private AnimationCurve displayCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Events")]
        [SerializeField] private SliderValueChangedEvent onValueChanged = new();

        public float MinValue
        {
            get => minValue;
            set
            {
                minValue = value;
                if (maxValue < minValue)
                {
                    maxValue = minValue;
                }

                SetValueWithoutNotify(this.value);
            }
        }

        public float MaxValue
        {
            get => maxValue;
            set
            {
                maxValue = Mathf.Max(value, minValue);
                SetValueWithoutNotify(this.value);
            }
        }

        public float Value
        {
            get => value;
            set => SetValue(value);
        }

        public float NormalizedValue
        {
            get => Mathf.Approximately(minValue, maxValue) ? 0f : Mathf.InverseLerp(minValue, maxValue, value);
            set => SetNormalizedValue(value);
        }

        public float DisplayNormalizedValue => GetDisplayNormalizedValue(NormalizedValue);

        public bool WholeNumbers
        {
            get => wholeNumbers;
            set
            {
                wholeNumbers = value;
                SetValueWithoutNotify(this.value);
            }
        }

        public float Step
        {
            get => step;
            set => step = Mathf.Max(0.0001f, value);
        }

        public DisplayMappingMode MappingMode
        {
            get => displayMappingMode;
            set
            {
                displayMappingMode = value;
                UpdateVisuals();
            }
        }

        public float DisplayExponent
        {
            get => displayExponent;
            set
            {
                displayExponent = Mathf.Max(0.01f, value);
                UpdateVisuals();
            }
        }

        public AnimationCurve DisplayCurve
        {
            get => displayCurve;
            set
            {
                displayCurve = value ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
                UpdateVisuals();
            }
        }

        public SliderValueChangedEvent OnValueChanged => onValueChanged;
        public Image BackgroundImage => backgroundImage;
        public Image FillImage => fillImage;
        public Image HandleImage => handleImage;

        protected override void Awake()
        {
            base.Awake();
            EnsureReferences();
            SetValueWithoutNotify(value);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateVisuals();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            transition = Transition.ColorTint;
            EnsureReferences();
            UpdateVisuals();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            minValue = Mathf.Min(minValue, maxValue);
            maxValue = Mathf.Max(maxValue, minValue);
            step = Mathf.Max(0.0001f, step);
            displayExponent = Mathf.Max(0.01f, displayExponent);
            EnsureReferences();
            value = ClampValue(value);
            UpdateVisuals();
        }
#endif

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            UpdateValueFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            UpdateValueFromPointer(eventData);
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            if (eventData.moveDir == MoveDirection.Left)
            {
                StepValue(-1);
                eventData.Use();
                return;
            }

            if (eventData.moveDir == MoveDirection.Right)
            {
                StepValue(1);
                eventData.Use();
                return;
            }

            base.OnMove(eventData);
        }

        public void SetValue(float newValue)
        {
            float clamped = ClampValue(newValue);
            if (Mathf.Approximately(value, clamped))
            {
                UpdateVisuals();
                return;
            }

            value = clamped;
            UpdateVisuals();
            onValueChanged.Invoke(value);
        }

        public void SetValueWithoutNotify(float newValue)
        {
            value = ClampValue(newValue);
            UpdateVisuals();
        }

        public void SetNormalizedValue(float normalizedValue)
        {
            SetValue(Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue)));
        }

        public void SetNormalizedValueWithoutNotify(float normalizedValue)
        {
            SetValueWithoutNotify(Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue)));
        }

        public void SetSprites(Sprite background, Sprite fill, Sprite handle)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = background;
            }

            if (fillImage != null)
            {
                fillImage.sprite = fill;
            }

            if (handleImage != null)
            {
                handleImage.sprite = handle;
            }

            UpdateVisuals();
        }

        public void SetDisplayExponent(bool enabled, float exponent)
        {
            displayMappingMode = enabled ? DisplayMappingMode.Exponent : DisplayMappingMode.Linear;
            displayExponent = Mathf.Max(0.01f, exponent);
            UpdateVisuals();
        }

        public void SetDisplayCurve(bool enabled, AnimationCurve curve)
        {
            displayMappingMode = enabled ? DisplayMappingMode.Curve : DisplayMappingMode.Linear;
            displayCurve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
            UpdateVisuals();
        }

        public void StepValue(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            float amount = wholeNumbers ? 1f : Mathf.Max(0.0001f, step);
            SetValue(value + amount * Math.Sign(direction));
        }

        private void UpdateValueFromPointer(PointerEventData eventData)
        {
            RectTransform targetRect = backgroundRect != null ? backgroundRect : transform as RectTransform;
            if (targetRect == null)
            {
                return;
            }

            Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, eventData.position, eventCamera, out Vector2 localPoint))
            {
                return;
            }

            Rect rect = targetRect.rect;
            float normalized = rect.width <= 0f ? 0f : Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            SetNormalizedValue(normalized);
        }

        private float ClampValue(float input)
        {
            float clamped = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers)
            {
                clamped = Mathf.Round(clamped);
            }

            return clamped;
        }

        private float GetDisplayNormalizedValue(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            return displayMappingMode switch
            {
                DisplayMappingMode.Exponent => Mathf.Clamp01(Mathf.Pow(normalized, Mathf.Max(0.01f, displayExponent))),
                DisplayMappingMode.Curve => Mathf.Clamp01((displayCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f)).Evaluate(normalized)),
                _ => normalized
            };
        }

        private void UpdateVisuals()
        {
            float displayValue = DisplayNormalizedValue;

            if (fillRect != null)
            {
                Vector2 anchorMin = fillRect.anchorMin;
                Vector2 anchorMax = fillRect.anchorMax;
                anchorMin.x = 0f;
                anchorMax.x = displayValue;
                fillRect.anchorMin = anchorMin;
                fillRect.anchorMax = anchorMax;

                Vector2 offsetMin = fillRect.offsetMin;
                Vector2 offsetMax = fillRect.offsetMax;
                offsetMin.x = 0f;
                offsetMax.x = 0f;
                fillRect.offsetMin = offsetMin;
                fillRect.offsetMax = offsetMax;
            }

            if (handleRect != null)
            {
                handleRect.anchorMin = new Vector2(displayValue, 0.5f);
                handleRect.anchorMax = new Vector2(displayValue, 0.5f);
                handleRect.anchoredPosition = Vector2.zero;
            }
        }

        private void EnsureReferences()
        {
            if (backgroundRect == null)
            {
                backgroundRect = FindChildRect("Background");
            }

            if (fillRect == null)
            {
                fillRect = FindChildRect("Fill");
            }

            if (handleRect == null)
            {
                handleRect = FindChildRect("Handle");
            }

            if (backgroundImage == null && backgroundRect != null)
            {
                backgroundImage = backgroundRect.GetComponent<Image>();
            }

            if (fillImage == null && fillRect != null)
            {
                fillImage = fillRect.GetComponent<Image>();
            }

            if (handleImage == null && handleRect != null)
            {
                handleImage = handleRect.GetComponent<Image>();
            }

            if (targetGraphic == null)
            {
                targetGraphic = handleImage != null ? handleImage : backgroundImage;
            }
        }

        private RectTransform FindChildRect(string childName)
        {
            Transform child = transform.Find(childName);
            return child as RectTransform;
        }
    }
}
