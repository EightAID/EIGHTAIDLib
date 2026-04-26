using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EightAID.EIGHTAIDLib.Input
{
    public class ControllerDevice : MonoBehaviour
    {
        public enum GamepadFamily
        {
            None,
            Keyboard,
            Xbox,
            PlayStation,
            Switch,
            Generic
        }

        public static event Action OnGamepadConnected;
        public static event Action OnGamepadDisconnected;
        public static event Action<GamepadFamily> OnGamepadFamilyChanged;

        public static GamepadFamily CurrentGamepadFamily { get; private set; } = GamepadFamily.None;

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
                    UpdateCurrentGamepadFamily(device as Gamepad);
                    OnGamepadConnected?.Invoke();
                    break;
                case InputDeviceChange.Removed:
                    Debug.Log($"Gamepad disconnected: {device.displayName}");
                    UpdateCurrentGamepadFamily(Gamepad.current);
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
                    UpdateCurrentGamepadFamily(gamepad);
                    return true;
                }
            }

            UpdateCurrentGamepadFamily(null);
            return false;
        }

        public static GamepadFamily GetCurrentGamepadFamily()
        {
            UpdateCurrentGamepadFamily(Gamepad.current);
            return CurrentGamepadFamily;
        }

        public static GamepadFamily DetectGamepadFamily(InputDevice device)
        {
            if (device is not Gamepad)
            {
                return GamepadFamily.None;
            }

            string source = $"{device.layout} {device.displayName} {device.description.manufacturer} {device.description.product}".ToLowerInvariant();

            if (source.Contains("dualshock") || source.Contains("dualsense") || source.Contains("playstation") || source.Contains("sony"))
            {
                return GamepadFamily.PlayStation;
            }

            if (source.Contains("switch") || source.Contains("nintendo") || source.Contains("pro controller"))
            {
                return GamepadFamily.Switch;
            }

            if (source.Contains("xinput") || source.Contains("xbox") || source.Contains("microsoft"))
            {
                return GamepadFamily.Xbox;
            }

            return GamepadFamily.Generic;
        }

        private static void UpdateCurrentGamepadFamily(Gamepad gamepad)
        {
            GamepadFamily nextFamily = DetectGamepadFamily(gamepad);
            if (nextFamily == CurrentGamepadFamily)
            {
                return;
            }

            CurrentGamepadFamily = nextFamily;
            OnGamepadFamilyChanged?.Invoke(CurrentGamepadFamily);
        }
    }
}
