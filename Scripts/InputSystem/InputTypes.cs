using System;

namespace InputSystem
{
    /// <summary>
    /// 入力の種類を定義
    /// </summary>
    public enum InputType
    {
        None = 0,
        Confirm,    // A Button / Mouse Click
        Cancel,     // B Button
        Menu,       // Menu Button
        Navigate,   // Movement
        Skip,       // テキストスキップ用
    }

    /// <summary>
    /// 入力の状態
    /// </summary>
    public enum InputState
    {
        None,
        Pressed,    // 押された瞬間
        Held,       // 押し続けている
        Released,   // 離された瞬間
    }

    /// <summary>
    /// 入力イベントのデータ
    /// </summary>
    [System.Serializable]
    public struct InputEventData
    {
        public InputType inputType;
        public InputState inputState;
        public bool isFromMouse;
        public float timestamp;
        public UnityEngine.Vector2 axis;

        public InputEventData(InputType type, InputState state, bool fromMouse = false, UnityEngine.Vector2 axis = default)
        {
            inputType = type;
            inputState = state;
            isFromMouse = fromMouse;
            timestamp = UnityEngine.Time.time;
            this.axis = axis;
        }
    }

    /// <summary>
    /// 入力コンテキスト（どの画面/状況での入力か）
    /// </summary>
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

    /// <summary>
    /// 入力の設定
    /// </summary>
    [System.Serializable]
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
