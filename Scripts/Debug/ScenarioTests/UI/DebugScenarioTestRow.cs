#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>ホバー・コントローラー選択を、詳細表示と同じ選択状態へ接続する。</summary>
public sealed class DebugScenarioTestRow : Button
{
    public Action Selected { get; set; }
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        Selected?.Invoke();
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (IsInteractable()) EventSystem.current?.SetSelectedGameObject(gameObject);
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
#endif
