using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EightAID.EIGHTAIDLib.UI
{
    [AddComponentMenu("EightAID/UI/Simple Scroll View Layout")]
    [RequireComponent(typeof(RectTransform))]
    public class SimpleScrollViewLayout : MonoBehaviour
    {
        public enum LayoutPreset
        {
            Custom,
            VerticalList,
            HorizontalList,
            Grid,
            ImageGallery,
            CardGrid,
            TextLog
        }

        public enum LayoutDirection
        {
            VerticalScroll,
            HorizontalScroll,
            GridVerticalScroll,
            GridHorizontalScroll
        }

        public enum ChildSizeMode
        {
            UseCurrentRectSize,
            FixedLayoutSlotOnly
        }

        public enum HorizontalAlign
        {
            Left,
            Center,
            Right
        }

        public enum VerticalAlign
        {
            Top,
            Center,
            Bottom
        }

        [Header("Preset")]
        [SerializeField, Tooltip("よく使う並べ方の初期値です。Custom 以外を選ぶと方向、隙間、余白、上限数などをまとめて設定します。")]
        private LayoutPreset preset = LayoutPreset.Custom;

        [Header("Layout")]
        [SerializeField, FormerlySerializedAs("mode"), Tooltip("子要素を並べる方向です。VerticalScroll は縦だけ、HorizontalScroll は横だけ、Grid は指定数で折り返します。")]
        private LayoutDirection layoutDirection = LayoutDirection.VerticalScroll;

        [SerializeField, FormerlySerializedAs("constraintCount"), Min(1), Tooltip("横方向に並べる最大数です。VerticalScroll では 1 個固定として扱い、GridVerticalScroll ではこの数を超えると次の行へ送ります。")]
        private int maxColumns = 1;

        [SerializeField, Min(1), Tooltip("縦方向に並べる最大数です。HorizontalScroll では 1 個固定として扱い、GridHorizontalScroll ではこの数を超えると次の列へ送ります。")]
        private int maxRows = 1;

        [SerializeField, FormerlySerializedAs("spacing"), Tooltip("子要素同士の隙間です。X は左右の隙間、Y は上下の隙間です。子要素のサイズ自体は変更しません。")]
        private Vector2 itemSpacing = new(8f, 8f);

        [SerializeField, Tooltip("Content の端から最初/最後の子要素までの余白です。子要素の形は変更しません。")]
        private RectOffset padding = new();

        [Header("Slot")]
        [SerializeField, Tooltip("子要素の現在の Rect サイズで並べるか、固定スロットだけを使って並べるかを選びます。どちらでも子要素のサイズは変更しません。")]
        private ChildSizeMode childSizeMode = ChildSizeMode.UseCurrentRectSize;

        [SerializeField, FormerlySerializedAs("cellSize"), Tooltip("FixedLayoutSlotOnly のときに使う仮想スロットの大きさです。子要素の実サイズは変えず、スロット内の配置だけ調整します。")]
        private Vector2 slotSize = new(120f, 40f);

        [Header("Align")]
        [SerializeField, Tooltip("横方向の揃え位置です。リストでは Content 内の揃え、グリッドではスロット内の揃えとして使います。")]
        private HorizontalAlign horizontalAlign = HorizontalAlign.Left;

        [SerializeField, Tooltip("縦方向の揃え位置です。リストでは Content 内の揃え、グリッドではスロット内の揃えとして使います。")]
        private VerticalAlign verticalAlign = VerticalAlign.Top;

        [Header("Children")]
        [SerializeField, Tooltip("オンにすると非アクティブな子要素をレイアウト対象から外し、詰めて並べます。")]
        private bool ignoreInactiveChildren = true;

        private RectTransform _rectTransform;
        private int _lastVisibleChildCount;
        private int _lastIgnoredChildCount;
        private int _lastRows;
        private int _lastColumns;
        private Vector2 _lastRequiredSize;

        public LayoutDirection Direction
        {
            get => layoutDirection;
            set
            {
                layoutDirection = value;
                RefreshLayout();
            }
        }

        public Vector2 SlotSize
        {
            get => slotSize;
            set
            {
                slotSize = ClampVector2(value);
                RefreshLayout();
            }
        }

        public Vector2 ItemSpacing
        {
            get => itemSpacing;
            set
            {
                itemSpacing = ClampVector2(value);
                RefreshLayout();
            }
        }

        public RectOffset Padding => padding;
        public int LastVisibleChildCount => _lastVisibleChildCount;
        public int LastIgnoredChildCount => _lastIgnoredChildCount;
        public int LastRows => _lastRows;
        public int LastColumns => _lastColumns;
        public Vector2 LastRequiredSize => _lastRequiredSize;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyPresetIfNeeded();
            slotSize = ClampVector2(slotSize);
            itemSpacing = ClampVector2(itemSpacing);
            maxColumns = Mathf.Max(1, maxColumns);
            maxRows = Mathf.Max(1, maxRows);
            RefreshLayout();
        }
#endif

        private void Awake()
        {
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_rectTransform == null)
            {
                return;
            }

            List<RectTransform> children = GetLayoutChildren();
            _lastVisibleChildCount = children.Count;
            _lastIgnoredChildCount = CountIgnoredChildren();

            if (children.Count <= 0)
            {
                _lastRows = 0;
                _lastColumns = 0;
                ApplyContentSize(Vector2.zero);
                return;
            }

            switch (layoutDirection)
            {
                case LayoutDirection.HorizontalScroll:
                    LayoutSingleRow(children);
                    break;
                case LayoutDirection.GridVerticalScroll:
                    LayoutGridVertical(children);
                    break;
                case LayoutDirection.GridHorizontalScroll:
                    LayoutGridHorizontal(children);
                    break;
                default:
                    LayoutSingleColumn(children);
                    break;
            }
        }

        public void ApplyPreset(LayoutPreset nextPreset)
        {
            preset = nextPreset;
            ApplyPresetIfNeeded();
            RefreshLayout();
        }

        private void ApplyPresetIfNeeded()
        {
            switch (preset)
            {
                case LayoutPreset.VerticalList:
                    layoutDirection = LayoutDirection.VerticalScroll;
                    childSizeMode = ChildSizeMode.UseCurrentRectSize;
                    maxColumns = 1;
                    maxRows = 1;
                    itemSpacing = new Vector2(8f, 8f);
                    padding = new RectOffset(12, 12, 12, 12);
                    horizontalAlign = HorizontalAlign.Left;
                    verticalAlign = VerticalAlign.Top;
                    break;
                case LayoutPreset.HorizontalList:
                    layoutDirection = LayoutDirection.HorizontalScroll;
                    childSizeMode = ChildSizeMode.UseCurrentRectSize;
                    maxColumns = 1;
                    maxRows = 1;
                    itemSpacing = new Vector2(8f, 8f);
                    padding = new RectOffset(12, 12, 12, 12);
                    horizontalAlign = HorizontalAlign.Left;
                    verticalAlign = VerticalAlign.Top;
                    break;
                case LayoutPreset.Grid:
                    layoutDirection = LayoutDirection.GridVerticalScroll;
                    childSizeMode = ChildSizeMode.FixedLayoutSlotOnly;
                    slotSize = new Vector2(120f, 160f);
                    maxColumns = Mathf.Max(1, maxColumns);
                    maxRows = 1;
                    itemSpacing = new Vector2(12f, 12f);
                    padding = new RectOffset(12, 12, 12, 12);
                    horizontalAlign = HorizontalAlign.Center;
                    verticalAlign = VerticalAlign.Center;
                    break;
                case LayoutPreset.ImageGallery:
                    layoutDirection = LayoutDirection.GridVerticalScroll;
                    childSizeMode = ChildSizeMode.FixedLayoutSlotOnly;
                    slotSize = new Vector2(180f, 120f);
                    maxColumns = Mathf.Max(1, maxColumns);
                    maxRows = 1;
                    itemSpacing = new Vector2(16f, 16f);
                    padding = new RectOffset(16, 16, 16, 16);
                    horizontalAlign = HorizontalAlign.Center;
                    verticalAlign = VerticalAlign.Center;
                    break;
                case LayoutPreset.CardGrid:
                    layoutDirection = LayoutDirection.GridVerticalScroll;
                    childSizeMode = ChildSizeMode.FixedLayoutSlotOnly;
                    slotSize = new Vector2(128f, 184f);
                    maxColumns = Mathf.Max(1, maxColumns);
                    maxRows = 1;
                    itemSpacing = new Vector2(10f, 14f);
                    padding = new RectOffset(16, 16, 16, 16);
                    horizontalAlign = HorizontalAlign.Center;
                    verticalAlign = VerticalAlign.Center;
                    break;
                case LayoutPreset.TextLog:
                    layoutDirection = LayoutDirection.VerticalScroll;
                    childSizeMode = ChildSizeMode.UseCurrentRectSize;
                    maxColumns = 1;
                    maxRows = 1;
                    itemSpacing = new Vector2(0f, 6f);
                    padding = new RectOffset(12, 12, 12, 12);
                    horizontalAlign = HorizontalAlign.Left;
                    verticalAlign = VerticalAlign.Top;
                    break;
            }
        }

        private void LayoutSingleColumn(IReadOnlyList<RectTransform> children)
        {
            float cursorY = -padding.top;
            float requiredWidth = padding.horizontal;

            foreach (RectTransform child in children)
            {
                Vector2 childSize = GetChildActualSize(child);
                Vector2 layoutSize = GetChildLayoutSize(child);
                float x = padding.left + GetAlignedOffsetX(childSize.x, Mathf.Max(0f, _rectTransform.rect.width - padding.horizontal));
                float y = cursorY - GetAlignedOffsetY(childSize.y, layoutSize.y);
                SetChildPosition(child, x, y, childSize);
                requiredWidth = Mathf.Max(requiredWidth, padding.horizontal + layoutSize.x);
                cursorY -= layoutSize.y + itemSpacing.y;
            }

            _lastRows = children.Count;
            _lastColumns = 1;
            ApplyContentSize(new Vector2(requiredWidth, Mathf.Max(0f, padding.bottom - cursorY - itemSpacing.y)));
        }

        private void LayoutSingleRow(IReadOnlyList<RectTransform> children)
        {
            float cursorX = padding.left;
            float requiredHeight = padding.vertical;

            foreach (RectTransform child in children)
            {
                Vector2 childSize = GetChildActualSize(child);
                Vector2 layoutSize = GetChildLayoutSize(child);
                float x = cursorX + GetAlignedOffsetX(childSize.x, layoutSize.x);
                float y = -padding.top - GetAlignedOffsetY(childSize.y, Mathf.Max(0f, _rectTransform.rect.height - padding.vertical));
                SetChildPosition(child, x, y, childSize);
                requiredHeight = Mathf.Max(requiredHeight, padding.vertical + layoutSize.y);
                cursorX += layoutSize.x + itemSpacing.x;
            }

            _lastRows = 1;
            _lastColumns = children.Count;
            ApplyContentSize(new Vector2(Mathf.Max(0f, padding.right + cursorX - itemSpacing.x), requiredHeight));
        }

        private void LayoutGridVertical(IReadOnlyList<RectTransform> children)
        {
            int columns = Mathf.Max(1, maxColumns);
            int rows = Mathf.CeilToInt(children.Count / (float)columns);
            Vector2 gridSlotSize = GetGridSlotSize(children);
            ApplyContentSize(GetGridRequiredSize(columns, rows, gridSlotSize));

            for (int index = 0; index < children.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                PlaceChildInGridSlot(children[index], column, row, gridSlotSize);
            }

            _lastRows = rows;
            _lastColumns = columns;
        }

        private void LayoutGridHorizontal(IReadOnlyList<RectTransform> children)
        {
            int rows = Mathf.Max(1, maxRows);
            int columns = Mathf.CeilToInt(children.Count / (float)rows);
            Vector2 gridSlotSize = GetGridSlotSize(children);
            ApplyContentSize(GetGridRequiredSize(columns, rows, gridSlotSize));

            for (int index = 0; index < children.Count; index++)
            {
                int column = index / rows;
                int row = index % rows;
                PlaceChildInGridSlot(children[index], column, row, gridSlotSize);
            }

            _lastRows = rows;
            _lastColumns = columns;
        }

        private void PlaceChildInGridSlot(RectTransform child, int column, int row, Vector2 gridSlotSize)
        {
            Vector2 childSize = GetChildActualSize(child);
            float slotX = padding.left + (column * (gridSlotSize.x + itemSpacing.x));
            float slotY = -padding.top - (row * (gridSlotSize.y + itemSpacing.y));
            float x = slotX + GetAlignedOffsetX(childSize.x, gridSlotSize.x);
            float y = slotY - GetAlignedOffsetY(childSize.y, gridSlotSize.y);
            SetChildPosition(child, x, y, childSize);
        }

        private Vector2 GetGridRequiredSize(int columns, int rows, Vector2 gridSlotSize)
        {
            return new Vector2(
                padding.horizontal + (gridSlotSize.x * columns) + (itemSpacing.x * Mathf.Max(0, columns - 1)),
                padding.vertical + (gridSlotSize.y * rows) + (itemSpacing.y * Mathf.Max(0, rows - 1)));
        }

        private void ApplyContentSize(Vector2 requiredSize)
        {
            Vector2 size = new(
                Mathf.Max(_rectTransform.rect.width, requiredSize.x),
                Mathf.Max(_rectTransform.rect.height, requiredSize.y));
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            _lastRequiredSize = requiredSize;
        }

        private Vector2 GetChildLayoutSize(RectTransform child)
        {
            if (childSizeMode == ChildSizeMode.FixedLayoutSlotOnly)
            {
                return slotSize;
            }

            return GetChildActualSize(child);
        }

        private static Vector2 GetChildActualSize(RectTransform child)
        {
            Rect rect = child.rect;
            return new Vector2(Mathf.Max(0f, rect.width), Mathf.Max(0f, rect.height));
        }

        private Vector2 GetGridSlotSize(IReadOnlyList<RectTransform> children)
        {
            if (childSizeMode == ChildSizeMode.FixedLayoutSlotOnly)
            {
                return slotSize;
            }

            Vector2 max = Vector2.zero;
            foreach (RectTransform child in children)
            {
                Vector2 size = GetChildActualSize(child);
                max.x = Mathf.Max(max.x, size.x);
                max.y = Mathf.Max(max.y, size.y);
            }

            return max;
        }

        private static void SetChildPosition(RectTransform child, float topLeftX, float topLeftY, Vector2 childSize)
        {
            Vector2 pivotOffset = new(childSize.x * child.pivot.x, -childSize.y * (1f - child.pivot.y));
            child.anchoredPosition = new Vector2(topLeftX, topLeftY) + pivotOffset;
        }

        private float GetAlignedOffsetX(float childWidth, float availableWidth)
        {
            float available = Mathf.Max(0f, availableWidth - childWidth);
            return horizontalAlign switch
            {
                HorizontalAlign.Center => available * 0.5f,
                HorizontalAlign.Right => available,
                _ => 0f
            };
        }

        private float GetAlignedOffsetY(float childHeight, float availableHeight)
        {
            float available = Mathf.Max(0f, availableHeight - childHeight);
            return verticalAlign switch
            {
                VerticalAlign.Center => available * 0.5f,
                VerticalAlign.Bottom => available,
                _ => 0f
            };
        }

        private int CountIgnoredChildren()
        {
            int count = 0;
            if (!ignoreInactiveChildren)
            {
                return 0;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is RectTransform child && !child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private List<RectTransform> GetLayoutChildren()
        {
            var children = new List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is not RectTransform child)
                {
                    continue;
                }

                if (ignoreInactiveChildren && !child.gameObject.activeSelf)
                {
                    continue;
                }

                children.Add(child);
            }

            return children;
        }

        private static Vector2 ClampVector2(Vector2 value)
        {
            return new Vector2(Mathf.Max(0f, value.x), Mathf.Max(0f, value.y));
        }
    }
}
