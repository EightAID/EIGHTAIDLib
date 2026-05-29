using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.UI
{
    [AddComponentMenu("EightAID/UI/Simple Scroll View")]
    [RequireComponent(typeof(RectTransform))]
    public class SimpleScrollView : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [Serializable]
        public class ScrollPositionChangedEvent : UnityEvent<Vector2>
        {
        }

        public enum ContentChangeScrollMode
        {
            KeepNormalizedPosition,
            KeepPixelPosition,
            StickToStart,
            StickToEnd,
            PreserveIfUserMoved
        }

        [Header("Parts")]
        [SerializeField, Tooltip("表示範囲になる RectTransform です。通常は Viewport を指定します。")]
        private RectTransform viewportRect;

        [SerializeField, Tooltip("スクロールで動かす RectTransform です。通常は Viewport の子にある Content を指定します。")]
        private RectTransform contentRect;

        [SerializeField, Tooltip("縦スクロール用の SimpleSlider です。不要な場合は未設定でも構いません。")]
        private SimpleSlider verticalSlider;

        [SerializeField, Tooltip("横スクロール用の SimpleSlider です。不要な場合は未設定でも構いません。")]
        private SimpleSlider horizontalSlider;

        [Header("Axes")]
        [SerializeField, Tooltip("縦方向のスクロールを有効にします。")]
        private bool enableVertical = true;

        [SerializeField, Tooltip("横方向のスクロールを有効にします。")]
        private bool enableHorizontal;

        [Header("Input")]
        [SerializeField, Tooltip("マウスホイールでスクロールできるようにします。")]
        private bool enableWheel = true;

        [SerializeField, Tooltip("Viewport や Content をドラッグしてスクロールできるようにします。")]
        private bool enableDrag = true;

        [SerializeField, Tooltip("マウスホイール 1 目盛りあたりの移動量です。")]
        private float wheelSensitivity = 40f;

        [SerializeField, Tooltip("ドラッグ量に対する Content の移動倍率です。")]
        private float dragSensitivity = 1f;

        [Header("Range")]
        [SerializeField, FormerlySerializedAs("topExtraRange"), Tooltip("上端側に追加するスクロール余白です。縦スライダーが 1 のときの上側位置を広げたい場合に使います。")]
        private float topScrollMargin;

        [SerializeField, FormerlySerializedAs("bottomExtraRange"), Tooltip("下端側に追加するスクロール余白です。縦スライダーが 0 のときの下側位置を広げたい場合に使います。")]
        private float bottomScrollMargin;

        [SerializeField, FormerlySerializedAs("leftExtraRange"), Tooltip("左端側に追加するスクロール余白です。横スライダーが 0 のときの左側位置を広げたい場合に使います。")]
        private float leftScrollMargin;

        [SerializeField, FormerlySerializedAs("rightExtraRange"), Tooltip("右端側に追加するスクロール余白です。横スライダーが 1 のときの右側位置を広げたい場合に使います。")]
        private float rightScrollMargin;

        [Header("Custom End Positions")]
        [SerializeField, Tooltip("オンにすると、スライダー値 0 と 1 のときの Content の anchoredPosition を直接指定します。通常の余白計算より優先されます。")]
        private bool useCustomEndPositions;

        [SerializeField, Tooltip("スライダー値が 0 のときの Content.anchoredPosition です。X は横スライダー 0、Y は縦スライダー 0 の位置として使います。")]
        private Vector2 contentPositionAtSliderZero;

        [SerializeField, Tooltip("スライダー値が 1 のときの Content.anchoredPosition です。X は横スライダー 1、Y は縦スライダー 1 の位置として使います。")]
        private Vector2 contentPositionAtSliderOne = new(0f, 0f);

        [Header("Behavior")]
        [SerializeField, Tooltip("スクロールする必要がないときにスライダーを非表示にします。")]
        private bool hideSliderWhenNotScrollable = true;

        [SerializeField, Tooltip("Content のサイズや子要素数が変わったときに自動でレイアウトとスクロール範囲を更新します。")]
        private bool autoRefreshOnContentChange = true;

        [SerializeField, Tooltip("Content のサイズや子要素数が変わったとき、現在のスクロール位置をどう維持するかを選びます。")]
        private ContentChangeScrollMode contentChangeMode = ContentChangeScrollMode.KeepPixelPosition;

        [Header("Events")]
        [SerializeField] private ScrollPositionChangedEvent onPositionChanged = new();

        private SimpleScrollViewLayout _layout;
        private Vector2 _normalizedPosition = new(0f, 1f);
        private Vector2 _lastDragPosition;
        private Vector2 _lastViewportSize;
        private Vector2 _lastContentSize;
        private int _lastChildCount = -1;
        private bool _dragging;
        private bool _syncingSlider;
        private bool _userMoved;

        public RectTransform ViewportRect => viewportRect;
        public RectTransform ContentRect => contentRect;
        public SimpleSlider VerticalSlider => verticalSlider;
        public SimpleSlider HorizontalSlider => horizontalSlider;
        public ScrollPositionChangedEvent OnPositionChanged => onPositionChanged;

        public bool EnableVertical
        {
            get => enableVertical;
            set
            {
                enableVertical = value;
                Refresh();
            }
        }

        public bool EnableHorizontal
        {
            get => enableHorizontal;
            set
            {
                enableHorizontal = value;
                Refresh();
            }
        }

        public Vector2 NormalizedPosition
        {
            get => _normalizedPosition;
            set => SetNormalizedPosition(value);
        }

        public float VerticalNormalizedPosition
        {
            get => _normalizedPosition.y;
            set => SetNormalizedPosition(new Vector2(_normalizedPosition.x, value));
        }

        public float HorizontalNormalizedPosition
        {
            get => _normalizedPosition.x;
            set => SetNormalizedPosition(new Vector2(value, _normalizedPosition.y));
        }

        public Vector2 ScrollPosition
        {
            get => GetScrollOffset();
            set => SetScrollOffset(value, true);
        }

        public bool CanScrollVertical => GetVerticalRange() > 0f;
        public bool CanScrollHorizontal => GetHorizontalRange() > 0f;

        protected override void Awake()
        {
            base.Awake();
            EnsureReferences();
            RegisterSliderListeners();
            Refresh();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureReferences();
            RegisterSliderListeners();
            Refresh();
        }

        protected override void OnDisable()
        {
            UnregisterSliderListeners();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            EnsureReferences();
            Refresh();
        }

        protected override void OnValidate()
        {
            topScrollMargin = Mathf.Max(0f, topScrollMargin);
            bottomScrollMargin = Mathf.Max(0f, bottomScrollMargin);
            leftScrollMargin = Mathf.Max(0f, leftScrollMargin);
            rightScrollMargin = Mathf.Max(0f, rightScrollMargin);
            wheelSensitivity = Mathf.Max(0f, wheelSensitivity);
            dragSensitivity = Mathf.Max(0f, dragSensitivity);
            EnsureReferences();
            Refresh();
        }
#endif

        private void LateUpdate()
        {
            if (!autoRefreshOnContentChange || contentRect == null || viewportRect == null)
            {
                return;
            }

            Vector2 viewportSize = viewportRect.rect.size;
            Vector2 contentSize = contentRect.rect.size;
            int childCount = contentRect.childCount;
            if (viewportSize == _lastViewportSize && contentSize == _lastContentSize && childCount == _lastChildCount)
            {
                return;
            }

            RefreshForContentChange();
        }

        public void Refresh()
        {
            EnsureReferences();
            RefreshLayout();
            RefreshScrollRange();
            ApplyNormalizedPosition(_normalizedPosition, false);
            CacheCurrentContentState();
        }

        public void RefreshLayout()
        {
            if (_layout == null && contentRect != null)
            {
                _layout = contentRect.GetComponent<SimpleScrollViewLayout>();
            }

            _layout?.RefreshLayout();
        }

        public void RefreshScrollRange()
        {
            SetupSlider(verticalSlider, enableVertical && CanScrollVertical);
            SetupSlider(horizontalSlider, enableHorizontal && CanScrollHorizontal);
            SyncSlidersWithoutNotify();
        }

        public void RefreshForContentChange()
        {
            Vector2 normalizedBefore = _normalizedPosition;
            Vector2 pixelBefore = GetScrollOffset();

            RefreshLayout();
            RefreshScrollRange();

            switch (contentChangeMode)
            {
                case ContentChangeScrollMode.KeepNormalizedPosition:
                    ApplyNormalizedPosition(normalizedBefore, false);
                    break;
                case ContentChangeScrollMode.StickToStart:
                    ApplyNormalizedPosition(new Vector2(0f, 1f), false);
                    break;
                case ContentChangeScrollMode.StickToEnd:
                    ApplyNormalizedPosition(new Vector2(1f, 0f), false);
                    break;
                case ContentChangeScrollMode.PreserveIfUserMoved:
                    if (_userMoved)
                    {
                        SetScrollOffset(pixelBefore, false);
                    }
                    else
                    {
                        ApplyNormalizedPosition(new Vector2(1f, 0f), false);
                    }
                    break;
                default:
                    SetScrollOffset(pixelBefore, false);
                    break;
            }

            CacheCurrentContentState();
        }

        public void SetNormalizedPosition(Vector2 normalized)
        {
            ApplyNormalizedPosition(normalized, true);
        }

        public void SetNormalizedPositionWithoutNotify(Vector2 normalized)
        {
            ApplyNormalizedPosition(normalized, false);
        }

        public void ScrollToTop()
        {
            VerticalNormalizedPosition = 1f;
        }

        public void ScrollToBottom()
        {
            VerticalNormalizedPosition = 0f;
        }

        public void ScrollToLeft()
        {
            HorizontalNormalizedPosition = 0f;
        }

        public void ScrollToRight()
        {
            HorizontalNormalizedPosition = 1f;
        }

        public void ScrollBy(Vector2 delta)
        {
            SetScrollOffset(GetScrollOffset() + delta, true);
        }

        public void ScrollByNormalized(Vector2 delta)
        {
            SetNormalizedPosition(_normalizedPosition + delta);
        }

        public void SetExtraRange(float top, float bottom, float left, float right)
        {
            topScrollMargin = Mathf.Max(0f, top);
            bottomScrollMargin = Mathf.Max(0f, bottom);
            leftScrollMargin = Mathf.Max(0f, left);
            rightScrollMargin = Mathf.Max(0f, right);
            Refresh();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!enableDrag || eventData == null)
            {
                return;
            }

            _dragging = true;
            _lastDragPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!enableDrag || !_dragging || eventData == null)
            {
                return;
            }

            Vector2 pointerDelta = eventData.position - _lastDragPosition;
            _lastDragPosition = eventData.position;

            Vector2 scrollDelta = new(
                enableHorizontal ? -pointerDelta.x * dragSensitivity : 0f,
                enableVertical ? pointerDelta.y * dragSensitivity : 0f);
            ScrollBy(scrollDelta);
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!enableWheel || eventData == null)
            {
                return;
            }

            Vector2 delta = eventData.scrollDelta * wheelSensitivity;
            if (enableVertical && CanScrollVertical)
            {
                ScrollBy(new Vector2(0f, -delta.y));
            }
            else if (enableHorizontal && CanScrollHorizontal)
            {
                ScrollBy(new Vector2(-delta.y, 0f));
            }

            eventData.Use();
        }

        private void ApplyNormalizedPosition(Vector2 normalized, bool notify)
        {
            if (viewportRect == null || contentRect == null)
            {
                return;
            }

            Vector2 clamped = new(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
            Vector2 pos = contentRect.anchoredPosition;
            if (enableHorizontal)
            {
                pos.x = useCustomEndPositions
                    ? Mathf.Lerp(contentPositionAtSliderZero.x, contentPositionAtSliderOne.x, clamped.x)
                    : -Mathf.Lerp(-leftScrollMargin, GetHorizontalRange() - leftScrollMargin, clamped.x);
            }

            if (enableVertical)
            {
                pos.y = useCustomEndPositions
                    ? Mathf.Lerp(contentPositionAtSliderZero.y, contentPositionAtSliderOne.y, clamped.y)
                    : Mathf.Lerp(GetVerticalRange() - topScrollMargin, -topScrollMargin, clamped.y);
            }

            contentRect.anchoredPosition = pos;
            _normalizedPosition = clamped;
            SyncSlidersWithoutNotify();

            if (notify)
            {
                _userMoved = true;
                onPositionChanged.Invoke(_normalizedPosition);
            }
        }

        private Vector2 GetScrollOffset()
        {
            if (contentRect == null)
            {
                return Vector2.zero;
            }

            return new Vector2(-contentRect.anchoredPosition.x, contentRect.anchoredPosition.y);
        }

        private void SetScrollOffset(Vector2 offset, bool notify)
        {
            float normalizedX;
            float normalizedY;
            if (useCustomEndPositions)
            {
                float posX = -offset.x;
                float posY = offset.y;
                normalizedX = Mathf.Approximately(contentPositionAtSliderZero.x, contentPositionAtSliderOne.x)
                    ? 0f
                    : Mathf.InverseLerp(contentPositionAtSliderZero.x, contentPositionAtSliderOne.x, posX);
                normalizedY = Mathf.Approximately(contentPositionAtSliderZero.y, contentPositionAtSliderOne.y)
                    ? 1f
                    : Mathf.InverseLerp(contentPositionAtSliderZero.y, contentPositionAtSliderOne.y, posY);
            }
            else
            {
                float rangeX = GetHorizontalRange();
                float rangeY = GetVerticalRange();
                float minX = -leftScrollMargin;
                float maxX = rangeX - leftScrollMargin;
                float minY = -topScrollMargin;
                float maxY = rangeY - topScrollMargin;
                normalizedX = rangeX <= 0f ? 0f : Mathf.InverseLerp(minX, maxX, Mathf.Clamp(offset.x, minX, maxX));
                normalizedY = rangeY <= 0f ? 1f : 1f - Mathf.InverseLerp(minY, maxY, Mathf.Clamp(offset.y, minY, maxY));
            }

            ApplyNormalizedPosition(new Vector2(normalizedX, normalizedY), notify);
        }

        private float GetVerticalRange()
        {
            if (viewportRect == null || contentRect == null || !enableVertical)
            {
                return 0f;
            }

            if (useCustomEndPositions)
            {
                return Mathf.Abs(contentPositionAtSliderOne.y - contentPositionAtSliderZero.y);
            }

            return Mathf.Max(0f, contentRect.rect.height - viewportRect.rect.height + topScrollMargin + bottomScrollMargin);
        }

        private float GetHorizontalRange()
        {
            if (viewportRect == null || contentRect == null || !enableHorizontal)
            {
                return 0f;
            }

            if (useCustomEndPositions)
            {
                return Mathf.Abs(contentPositionAtSliderOne.x - contentPositionAtSliderZero.x);
            }

            return Mathf.Max(0f, contentRect.rect.width - viewportRect.rect.width + leftScrollMargin + rightScrollMargin);
        }

        private void SetupSlider(SimpleSlider slider, bool scrollable)
        {
            if (slider == null)
            {
                return;
            }

            slider.MinValue = 0f;
            slider.MaxValue = 1f;
            slider.gameObject.SetActive(!hideSliderWhenNotScrollable || scrollable);
        }

        private void SyncSlidersWithoutNotify()
        {
            _syncingSlider = true;
            if (verticalSlider != null)
            {
                verticalSlider.SetNormalizedValueWithoutNotify(_normalizedPosition.y);
            }

            if (horizontalSlider != null)
            {
                horizontalSlider.SetNormalizedValueWithoutNotify(_normalizedPosition.x);
            }

            _syncingSlider = false;
        }

        private void RegisterSliderListeners()
        {
            UnregisterSliderListeners();
            if (verticalSlider != null)
            {
                verticalSlider.OnValueChanged.AddListener(OnVerticalSliderChanged);
            }

            if (horizontalSlider != null)
            {
                horizontalSlider.OnValueChanged.AddListener(OnHorizontalSliderChanged);
            }
        }

        private void UnregisterSliderListeners()
        {
            if (verticalSlider != null)
            {
                verticalSlider.OnValueChanged.RemoveListener(OnVerticalSliderChanged);
            }

            if (horizontalSlider != null)
            {
                horizontalSlider.OnValueChanged.RemoveListener(OnHorizontalSliderChanged);
            }
        }

        private void OnVerticalSliderChanged(float value)
        {
            if (_syncingSlider)
            {
                return;
            }

            VerticalNormalizedPosition = value;
        }

        private void OnHorizontalSliderChanged(float value)
        {
            if (_syncingSlider)
            {
                return;
            }

            HorizontalNormalizedPosition = value;
        }

        private void CacheCurrentContentState()
        {
            if (viewportRect == null || contentRect == null)
            {
                return;
            }

            _lastViewportSize = viewportRect.rect.size;
            _lastContentSize = contentRect.rect.size;
            _lastChildCount = contentRect.childCount;
        }

        private void EnsureReferences()
        {
            if (viewportRect == null)
            {
                viewportRect = transform.Find("Viewport") as RectTransform;
            }

            if (contentRect == null && viewportRect != null)
            {
                contentRect = viewportRect.Find("Content") as RectTransform;
            }

            if (verticalSlider == null)
            {
                verticalSlider = transform.Find("VerticalSlider")?.GetComponent<SimpleSlider>();
            }

            if (horizontalSlider == null)
            {
                horizontalSlider = transform.Find("HorizontalSlider")?.GetComponent<SimpleSlider>();
            }

            if (viewportRect != null && viewportRect.GetComponent<RectMask2D>() == null)
            {
                viewportRect.gameObject.AddComponent<RectMask2D>();
            }
        }
    }
}
