using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EightAID.EIGHTAIDLib.UI
{
    public class NavigationScope : MonoBehaviour
    {
        public Action OnCancel;
        public Action OnLRLeft;
        public Action OnLRRight;
        public Action OnMenu;
        public Action OnExtra;

        private static readonly List<NavigationScope> Stack = new();

        public static NavigationScope Current => Stack.Count > 0 ? Stack[^1] : null;

        private GameObject _savedSelected;
        private GameObject _firstSelected;
        private readonly List<GameObject> _selectionHistory = new();

        public bool IsTop => Stack.Count > 0 && Stack[^1] == this;

        public void Push(GameObject firstSelected = null)
        {
            _savedSelected = EventSystem.current?.currentSelectedGameObject;
            _firstSelected = firstSelected;
            _selectionHistory.Clear();
            Stack.Add(this);
            EnsureValidSelection();
        }

        public void Pop()
        {
            if (!IsTop)
            {
                return;
            }

            Stack.RemoveAt(Stack.Count - 1);
            EventSystem.current?.SetSelectedGameObject(_savedSelected);
        }

        public void RememberCurrentSelection()
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            if (!Contains(current))
            {
                return;
            }

            if (_selectionHistory.Count > 0 && _selectionHistory[^1] == current)
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

        private void OnDestroy()
        {
            Stack.Remove(this);
        }
    }
}
