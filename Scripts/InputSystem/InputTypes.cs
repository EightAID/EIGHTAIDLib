using System;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Input
{
    public enum InputType
    {
        None = 0,
        Confirm,
        Cancel,
        Menu,
        Navigate,
        Skip,
    }

    public enum InputState
    {
        None,
        Pressed,
        Held,
        Released,
    }

    [Serializable]
    public struct InputEventData
    {
        public InputType inputType;
        public InputState inputState;
        public bool isFromMouse;
        public float timestamp;
        public Vector2 axis;

        public InputEventData(InputType type, InputState state, bool fromMouse = false, Vector2 axis = default)
        {
            inputType = type;
            inputState = state;
            isFromMouse = fromMouse;
            timestamp = Time.time;
            this.axis = axis;
        }
    }

    public enum InputContext
    {
        None = 0,
        MainMenu,
        GamePlay,
        Dialog,
        Battle,
        StageMap,
        Paused,
    }

    [Serializable]
    public struct InputSettings
    {
        public bool enabled;
        public bool consumeOnUse;
        public float cooldownTime;
        public InputContext[] allowedContexts;

        public static InputSettings Default => new InputSettings
        {
            enabled = true,
            consumeOnUse = true,
            cooldownTime = 0f,
            allowedContexts = new[] { InputContext.GamePlay }
        };
    }
}
