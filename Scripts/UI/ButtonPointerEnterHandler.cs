using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ボタンにホバーしたとき拡大アニメーションと SE を再生する汎用コンポーネント。
/// SE 再生は PlaySoundCallback に関数を登録することで外部から注入する。
/// 例: ButtonPointerEnterHandler.PlaySoundCallback = clip => SoundController.Instance?.PlaySoundReference(clip);
/// </summary>
public class ButtonPointerEnterHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>ホバー SE の再生先。プロジェクト側で SoundController 等に接続する。</summary>
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
            Debug.LogError($"{nameof(ButtonPointerEnterHandler)} は RectTransform のあるUIに付けてください: {name}");
    }

    private void Start()
    {
        if (_rectTransform != null)
            _cachedStartScale = _rectTransform.localScale;
    }

    private bool IsInteractable() => _button == null || _button.interactable;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        if (!CanApplyHoverVisual()) return;

        if (hoverSound != null)
            PlaySoundCallback?.Invoke(hoverSound);

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
        if (!IsInteractable()) return false;
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return true;
        var selected = eventSystem.currentSelectedGameObject;
        return selected == null || selected == gameObject;
    }

    private void ScaleTo(Vector3 targetScale)
    {
        if (_rectTransform == null) return;
        _scaleTween?.Kill();
        _scaleTween = _rectTransform
            .DOScale(targetScale, tweenDuration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        _rectTransform?.DOKill();
        if (_rectTransform != null) _rectTransform.localScale = _cachedStartScale;
        _isPointerOver = false;
        _isHoverScaled = false;
    }

    private void Update()
    {
        if (!_isPointerOver || !_isHoverScaled) return;
        if (CanApplyHoverVisual()) return;
        ResetScale();
    }
}
