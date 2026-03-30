using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EightAID.EIGHTAIDLib.Input
{
    public class ControllerDevice : MonoBehaviour
    {
        public static event Action OnGamepadConnected;
        public static event Action OnGamepadDisconnected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            UnityEngine.InputSystem.InputSystem.onDeviceChange -= HandleDeviceChange;
            UnityEngine.InputSystem.InputSystem.onDeviceChange += HandleDeviceChange;
        }

        private static void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not Gamepad)
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log($"Gamepad connected: {device.displayName}");
                    OnGamepadConnected?.Invoke();
                    break;
                case InputDeviceChange.Removed:
                    Debug.Log($"Gamepad disconnected: {device.displayName}");
                    OnGamepadDisconnected?.Invoke();
                    break;
            }
        }

        public static bool IsGamepad()
        {
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (device is Gamepad gamepad)
                {
                    Debug.Log($"Gamepad detected: {gamepad.displayName}");
                    return true;
                }
            }

            return false;
        }
    }
}
