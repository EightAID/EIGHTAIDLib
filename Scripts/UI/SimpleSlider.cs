using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// Background、Fill、Handle の 3 パーツだけで構成する軽量 UI スライダーです。
    /// Unity 標準 Slider より単純な階層で、スプライト差し替え後も RectTransform のサイズを基準に表示します。
    /// </summary>
    [AddComponentMenu("EightAID/UI/Simple Slider")]
    [RequireComponent(typeof(RectTransform))]
    public class SimpleSlider : Selectable, IDragHandler
    {
        /// <summary>
        /// スライダーの値が変更されたときに、現在の Value を通知する UnityEvent です。
        /// </summary>
        [Serializable]
        public class SliderValueChangedEvent : UnityEvent<float>
        {
        }

        /// <summary>
        /// 実際の Value から見た目上の Fill/Handle 位置へ変換する方式です。
        /// </summary>
        public enum DisplayMappingMode
        {
            /// <summary>値をそのまま 0-1 の表示位置として使います。</summary>
            Linear,

            /// <summary>正規化値に指数をかけて表示位置を調整します。</summary>
            Exponent,

            /// <summary>AnimationCurve で表示位置を調整します。</summary>
            Curve
        }

        [Header("Parts")]
        [SerializeField, Tooltip("スライダー全体のクリック範囲と横幅の基準になる背景 RectTransform です。通常は Background を指定します。")]
        private RectTransform backgroundRect;

        [SerializeField, Tooltip("現在値に合わせて横方向に伸縮する Fill の RectTransform です。縦方向のサイズは Inspector 側の設定を維持します。")]
        private RectTransform fillRect;

        [SerializeField, Tooltip("現在値に合わせて横方向に移動する Handle の RectTransform です。")]
        private RectTransform handleRect;

        [SerializeField, Tooltip("背景表示に使う Image です。スプライト差し替え用 API の対象になります。")]
        private Image backgroundImage;

        [SerializeField, Tooltip("Fill 表示に使う Image です。スプライト差し替え用 API の対象になります。")]
        private Image fillImage;

        [SerializeField, Tooltip("Handle 表示に使う Image です。スプライト差し替え用 API の対象になります。マウスで直接触れる対象にもできます。")]
        private Image handleImage;

        [Header("Value")]
        [SerializeField, Tooltip("スライダーが取りうる最小値です。")]
        private float minValue = 0f;

        [SerializeField, Tooltip("スライダーが取りうる最大値です。")]
        private float maxValue = 1f;

        [SerializeField, Tooltip("現在の実値です。API からは Value / SetValue / SetValueWithoutNotify で操作します。")]
        private float value;

        [SerializeField, Tooltip("オンにすると値を整数に丸めます。")]
        private bool wholeNumbers;

        [SerializeField, Tooltip("キーボードやゲームパッドの左右入力で増減する幅です。Whole Numbers がオンの場合は 1 ずつ増減します。")]
        private float step = 0.05f;

        [Header("Display Mapping")]
        [SerializeField, Tooltip("実値を見た目上の Fill/Handle 位置へ変換する方式です。Value 自体は常に minValue - maxValue の実値として保持します。")]
        private DisplayMappingMode displayMappingMode = DisplayMappingMode.Linear;

        [SerializeField, Min(0.01f), Tooltip("Display Mapping が Exponent のときに使う指数です。1 ならリニア、2 なら前半がゆっくり進む見た目です。")]
        private float displayExponent = 1f;

        [SerializeField, Tooltip("Display Mapping が Curve のときに使う表示補正カーブです。横軸が実値の 0-1、縦軸が表示位置の 0-1 です。")]
        private AnimationCurve displayCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Events")]
        [SerializeField, Tooltip("値が変更されたときに呼ばれます。引数は現在の Value です。")]
        private SliderValueChangedEvent onValueChanged = new();

        /// <summary>
        /// スライダーが取りうる最小値です。
        /// </summary>
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

        /// <summary>
        /// スライダーが取りうる最大値です。
        /// </summary>
        public float MaxValue
        {
            get => maxValue;
            set
            {
                maxValue = Mathf.Max(value, minValue);
                SetValueWithoutNotify(this.value);
            }
        }

        /// <summary>
        /// 現在の実値です。代入すると onValueChanged を通知します。
        /// </summary>
        public float Value
        {
            get => value;
            set => SetValue(value);
        }

        /// <summary>
        /// 現在値を minValue - maxValue の範囲で 0-1 に正規化した値です。
        /// </summary>
        public float NormalizedValue
        {
            get => Mathf.Approximately(minValue, maxValue) ? 0f : Mathf.InverseLerp(minValue, maxValue, value);
            set => SetNormalizedValue(value);
        }

        /// <summary>
        /// 表示補正を適用した 0-1 の値です。Fill と Handle の見た目上の位置に使われます。
        /// </summary>
        public float DisplayNormalizedValue => GetDisplayNormalizedValue(NormalizedValue);

        /// <summary>
        /// true の場合、Value を整数に丸めます。
        /// </summary>
        public bool WholeNumbers
        {
            get => wholeNumbers;
            set
            {
                wholeNumbers = value;
                SetValueWithoutNotify(this.value);
            }
        }

        /// <summary>
        /// 左右ナビゲーション入力で値を増減する幅です。
        /// </summary>
        public float Step
        {
            get => step;
            set => step = Mathf.Max(0.0001f, value);
        }

        /// <summary>
        /// 実値から表示位置への変換方式です。
        /// </summary>
        public DisplayMappingMode MappingMode
        {
            get => displayMappingMode;
            set
            {
                displayMappingMode = value;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Exponent 表示補正に使う指数です。
        /// </summary>
        public float DisplayExponent
        {
            get => displayExponent;
            set
            {
                displayExponent = Mathf.Max(0.01f, value);
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Curve 表示補正に使う AnimationCurve です。
        /// </summary>
        public AnimationCurve DisplayCurve
        {
            get => displayCurve;
            set
            {
                displayCurve = value ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
                UpdateVisuals();
            }
        }

        /// <summary>
        /// 値が変更されたときに現在の Value を通知します。
        /// </summary>
        public SliderValueChangedEvent OnValueChanged => onValueChanged;

        /// <summary>
        /// 背景表示に使う Image です。
        /// </summary>
        public Image BackgroundImage => backgroundImage;

        /// <summary>
        /// Fill 表示に使う Image です。
        /// </summary>
        public Image FillImage => fillImage;

        /// <summary>
        /// Handle 表示に使う Image です。
        /// </summary>
        public Image HandleImage => handleImage;

        protected override void Awake()
        {
            base.Awake();
            EnsureReferences();
            EnsureHandleDragForwarder();
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

        /// <summary>
        /// 値を変更し、値が変わった場合は onValueChanged を通知します。
        /// </summary>
        /// <param name="newValue">設定したい実値です。</param>
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

        /// <summary>
        /// 値を変更しますが、onValueChanged は通知しません。
        /// 初期化や外部状態との同期に使います。
        /// </summary>
        /// <param name="newValue">設定したい実値です。</param>
        public void SetValueWithoutNotify(float newValue)
        {
            value = ClampValue(newValue);
            UpdateVisuals();
        }

        /// <summary>
        /// 0-1 の正規化値から Value を設定し、値が変わった場合は onValueChanged を通知します。
        /// </summary>
        /// <param name="normalizedValue">0-1 の正規化値です。</param>
        public void SetNormalizedValue(float normalizedValue)
        {
            SetValue(Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue)));
        }

        /// <summary>
        /// 0-1 の正規化値から Value を設定しますが、onValueChanged は通知しません。
        /// </summary>
        /// <param name="normalizedValue">0-1 の正規化値です。</param>
        public void SetNormalizedValueWithoutNotify(float normalizedValue)
        {
            SetValueWithoutNotify(Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue)));
        }

        /// <summary>
        /// Background、Fill、Handle のスプライトをまとめて差し替えます。
        /// RectTransform のサイズは変更しないため、素材サイズに UI レイアウトが引っ張られません。
        /// </summary>
        /// <param name="background">背景に使うスプライトです。</param>
        /// <param name="fill">Fill に使うスプライトです。</param>
        /// <param name="handle">Handle に使うスプライトです。</param>
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

        /// <summary>
        /// 指数による表示補正を設定します。Value 自体は変更せず、Fill と Handle の見た目上の位置だけを変えます。
        /// </summary>
        /// <param name="enabled">true の場合、指数補正を有効にします。</param>
        /// <param name="exponent">表示補正に使う指数です。</param>
        public void SetDisplayExponent(bool enabled, float exponent)
        {
            displayMappingMode = enabled ? DisplayMappingMode.Exponent : DisplayMappingMode.Linear;
            displayExponent = Mathf.Max(0.01f, exponent);
            UpdateVisuals();
        }

        /// <summary>
        /// AnimationCurve による表示補正を設定します。Value 自体は変更せず、Fill と Handle の見た目上の位置だけを変えます。
        /// </summary>
        /// <param name="enabled">true の場合、カーブ補正を有効にします。</param>
        /// <param name="curve">表示補正に使うカーブです。</param>
        public void SetDisplayCurve(bool enabled, AnimationCurve curve)
        {
            displayMappingMode = enabled ? DisplayMappingMode.Curve : DisplayMappingMode.Linear;
            displayCurve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
            UpdateVisuals();
        }

        /// <summary>
        /// Step の幅で値を増減します。左右ナビゲーション入力からも呼ばれます。
        /// </summary>
        /// <param name="direction">正なら増加、負なら減少します。</param>
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

        private void EnsureHandleDragForwarder()
        {
            if (handleRect == null)
            {
                return;
            }

            if (!handleRect.TryGetComponent(out SimpleSliderHandleDragForwarder forwarder))
            {
                forwarder = handleRect.gameObject.AddComponent<SimpleSliderHandleDragForwarder>();
            }

            forwarder.SetSlider(this);
        }

        private RectTransform FindChildRect(string childName)
        {
            Transform child = transform.Find(childName);
            return child as RectTransform;
        }
    }

}
