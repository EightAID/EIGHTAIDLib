#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A recycled command row keeps EventSystem selection and the inspected command together.
public sealed class RuntimeDebugCommandRow : Button
{
    public int CommandIndex { get; private set; }
    private DebugCommand _command;
    private Text _category;
    private Text _title;
    private Text _description;
    private Action<int> _select;
    private Action<int> _move;

    public void Initialize(Text category, Text title, Text description, Action<int> select, Action<int> move)
    {
        _category = category;
        _title = title;
        _description = description;
        _select = select;
        _move = move;
    }

    public void Bind(DebugCommand command, int index)
    {
        CommandIndex = index;
        if (ReferenceEquals(_command, command))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        _command = command;
        name = command.Id + "Row";
        int separator = command.Category.LastIndexOf(" / ", StringComparison.Ordinal);
        _category.text = separator < 0 ? command.Category : command.Category.Substring(separator + 3);
        _title.text = command.Label;
        _description.text = command.Description;
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        _select?.Invoke(CommandIndex);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Up || eventData.moveDir == MoveDirection.Down)
        {
            _move?.Invoke(eventData.moveDir == MoveDirection.Up ? -1 : 1);
            eventData.Use();
            return;
        }

        base.OnMove(eventData);
    }
}
#endif
