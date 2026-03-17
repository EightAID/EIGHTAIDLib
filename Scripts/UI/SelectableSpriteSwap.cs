using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class SelectableSpriteSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Button targetButton;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private bool setNativeSizeOnSwap = true;
    [SerializeField] private bool includePointerHover = true;
    [Header("Selection Arrow")]
    [SerializeField] private Image selectionArrowImage;
    [SerializeField] private float arrowMoveDistance = 10f;
    [SerializeField] private float arrowMoveDuration = 0.45f;

    private bool _isPointerOver;
    private bool _isSelected;
    private RectTransform _arrowRect;
    private Vector2 _arrowBaseAnchoredPos;
    private Tween _arrowTween;
    private bool _lastHighlighted;

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetImage == null && targetButton != null)
            targetImage = targetButton.targetGraphic as Image;

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (selectionArrowImage != null)
        {
            _arrowRect = selectionArrowImage.rectTransform;
            _arrowBaseAnchoredPos = _arrowRect.anchoredPosition;
            selectionArrowImage.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        _isPointerOver = false;
        _isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Refresh(force: true);
    }

    private void OnDisable()
    {
        StopArrowAnimation();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        Refresh(force: true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        Refresh(force: true);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        Refresh(force: true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        Refresh(force: true);
    }

    public void SetSelectedVisual(bool isSelected)
    {
        _isPointerOver = false;
        _isSelected = isSelected;
        Refresh(force: true);
    }

    public void SetPointerHoverEnabled(bool enabled)
    {
        includePointerHover = enabled;
        if (!enabled)
        {
            _isPointerOver = false;
            Refresh(force: true);
        }
    }

    private void Update()
    {
        // 選択元がコントローラーで変わった時に、ポインターホバー見た目が残らないよう同期する
        Refresh();
    }

    private void Refresh(bool force = false)
    {
        if (targetImage == null) return;

        bool hasCompetingSelection = EventSystem.current != null &&
                                     EventSystem.current.currentSelectedGameObject != null &&
                                     EventSystem.current.currentSelectedGameObject != gameObject;
        bool usePointerHover = includePointerHover && _isPointerOver && !hasCompetingSelection;
        bool highlighted = _isSelected || usePointerHover;
        if (!force && highlighted == _lastHighlighted) return;
        _lastHighlighted = highlighted;
        UpdateSelectionArrow(highlighted);
        if (highlighted && selectedSprite != null)
        {
            targetImage.sprite = selectedSprite;
            if (setNativeSizeOnSwap)
                ApplyNativeSizePreservingChildren();
            return;
        }

        if (normalSprite != null)
        {
            targetImage.sprite = normalSprite;
            if (setNativeSizeOnSwap)
                ApplyNativeSizePreservingChildren();
        }
    }

    private void ApplyNativeSizePreservingChildren()
    {
        var root = targetImage.rectTransform;
        if (root == null)
        {
            targetImage.SetNativeSize();
            return;
        }

        var descendants = root.GetComponentsInChildren<RectTransform>(true);
        var cached = new List<(RectTransform rect, Vector3 localPos)>(descendants.Length);
        for (int i = 0; i < descendants.Length; i++)
        {
            var rect = descendants[i];
            if (rect == null || rect == root) continue;
            cached.Add((rect, rect.localPosition));
        }

        targetImage.SetNativeSize();

        for (int i = 0; i < cached.Count; i++)
        {
            var entry = cached[i];
            if (entry.rect == null) continue;
            entry.rect.localPosition = entry.localPos;
        }
    }

    private void UpdateSelectionArrow(bool highlighted)
    {
        if (selectionArrowImage == null || _arrowRect == null) return;

        if (!highlighted)
        {
            StopArrowAnimation();
            selectionArrowImage.gameObject.SetActive(false);
            return;
        }

        selectionArrowImage.gameObject.SetActive(true);
        if (_arrowTween != null && _arrowTween.IsActive()) return;

        _arrowRect.anchoredPosition = _arrowBaseAnchoredPos;
        _arrowTween = _arrowRect
            .DOAnchorPosX(_arrowBaseAnchoredPos.x + Mathf.Abs(arrowMoveDistance), Mathf.Max(0.05f, arrowMoveDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopArrowAnimation()
    {
        if (_arrowTween != null)
        {
            _arrowTween.Kill();
            _arrowTween = null;
        }

        if (_arrowRect != null)
            _arrowRect.anchoredPosition = _arrowBaseAnchoredPos;
    }
}
