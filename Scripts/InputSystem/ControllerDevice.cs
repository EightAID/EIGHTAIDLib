using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲームパッド（コントローラー）の検出と接続状態の変化を通知する。
/// </summary>
public class ControllerDevice : MonoBehaviour
{
    /// <summary>ゲームパッドが接続されたとき</summary>
    public static event Action OnGamepadConnected;

    /// <summary>ゲームパッドが切断されたとき</summary>
    public static event Action OnGamepadDisconnected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        // ドメインリロード時の二重登録を防ぐ
        UnityEngine.InputSystem.InputSystem.onDeviceChange -= HandleDeviceChange;
        UnityEngine.InputSystem.InputSystem.onDeviceChange += HandleDeviceChange;
    }

    private static void HandleDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not Gamepad) return;

        switch (change)
        {
            case InputDeviceChange.Added:
                Debug.Log($"コントローラー接続: {device.displayName}");
                OnGamepadConnected?.Invoke();
                break;
            case InputDeviceChange.Removed:
                Debug.Log($"コントローラー切断: {device.displayName}");
                OnGamepadDisconnected?.Invoke();
                break;
        }
    }

    /// <summary>現在ゲームパッドが接続されているか</summary>
    public static bool IsGamepad()
    {
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is Gamepad gamepad)
            {
                Debug.Log($"コントローラー検出: {gamepad.displayName}");
                return true;
            }
        }
        return false;
    }
}
