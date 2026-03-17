using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// パネルが開いている間、入力をそのパネルにスコープするためのコンポーネント。
/// DeckListPanel / MenuPanel / StillViewerPanel などの GameObject に追加し、
/// Open() で Push、Close() で Pop を呼ぶ。
/// InputSystemBase.HandleScopeInput() が NavigationScope.Current を参照して入力を委譲する。
/// </summary>
public class NavigationScope : MonoBehaviour
{
    /// <summary>B ボタン（キャンセル）</summary>
    public Action OnCancel;
    /// <summary>LR 軸の左入力 / L ショルダー</summary>
    public Action OnLRLeft;
    /// <summary>LR 軸の右入力 / R ショルダー</summary>
    public Action OnLRRight;
    /// <summary>Menu ボタン</summary>
    public Action OnMenu;
    /// <summary>X ボタン（追加アクション）</summary>
    public Action OnExtra;

    static readonly List<NavigationScope> _stack = new List<NavigationScope>();

    /// <summary>現在最前面のスコープ（スタックが空なら null）</summary>
    public static NavigationScope Current =>
        _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

    GameObject _savedSelected;
    GameObject _firstSelected;
    readonly List<GameObject> _selectionHistory = new List<GameObject>();

    /// <summary>
    /// スタックに積む。firstSelected が非 null なら EventSystem のフォーカスを移す。
    /// </summary>
    public void Push(GameObject firstSelected = null)
    {
        _savedSelected = EventSystem.current?.currentSelectedGameObject;
        _firstSelected = firstSelected;
        _selectionHistory.Clear();
        _stack.Add(this);
        EnsureValidSelection();
    }

    /// <summary>
    /// スタックから降ろす。Push 前の選択を復元する。
    /// </summary>
    public void Pop()
    {
        if (!IsTop) return;
        _stack.RemoveAt(_stack.Count - 1);
        EventSystem.current?.SetSelectedGameObject(_savedSelected);
    }

    /// <summary>このスコープがスタックの最前面か</summary>
    public bool IsTop => _stack.Count > 0 && _stack[_stack.Count - 1] == this;

    public void RememberCurrentSelection()
    {
        var current = EventSystem.current?.currentSelectedGameObject;
        if (!Contains(current))
        {
            return;
        }

        if (_selectionHistory.Count > 0 && _selectionHistory[_selectionHistory.Count - 1] == current)
        {
            return;
        }

        _selectionHistory.Add(current);
    }

    public void RestoreRememberedSelection()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        while (_selectionHistory.Count > 0)
        {
            int lastIndex = _selectionHistory.Count - 1;
            GameObject candidate = _selectionHistory[lastIndex];
            _selectionHistory.RemoveAt(lastIndex);

            if (candidate != null && candidate.activeInHierarchy && Contains(candidate))
            {
                eventSystem.SetSelectedGameObject(candidate);
                return;
            }
        }

        EnsureValidSelection();
    }

    public bool Contains(GameObject target)
    {
        return target != null && target.transform.IsChildOf(transform);
    }

    public GameObject GetFallbackSelected()
    {
        if (_firstSelected != null && _firstSelected.activeInHierarchy)
        {
            return _firstSelected;
        }

        var selectables = GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable != null && selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
            {
                return selectable.gameObject;
            }
        }

        return null;
    }

    public void EnsureValidSelection()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        var current = eventSystem.currentSelectedGameObject;
        if (Contains(current))
        {
            return;
        }

        eventSystem.SetSelectedGameObject(GetFallbackSelected());
    }

    void OnDestroy() => _stack.Remove(this);
}
