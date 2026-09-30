using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EightAID.EIGHTAIDLib.Input
{
    /// <summary>Device polling for shortcuts and exhibition inactivity timers.</summary>
    public static class DeviceInput
    {
        public static bool IsKeyPressed(Key key) => Keyboard.current?[key].isPressed == true;
        public static bool WasKeyPressed(Key key) => Keyboard.current?[key].wasPressedThisFrame == true;
        public static bool WasKeyReleased(Key key) => Keyboard.current?[key].wasReleasedThisFrame == true;

        public static bool HasActivity
        {
            get
            {
                if (Keyboard.current?.anyKey.isPressed == true) return true;

                foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
                {
                    if (device is Mouse mouse)
                    {
                        if (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed ||
                            mouse.forwardButton.isPressed || mouse.backButton.isPressed) return true;
                    }
                    else if (device is Gamepad || device is Joystick)
                    {
                        foreach (var control in device.allControls)
                        {
                            if (control is ButtonControl button && button.isPressed) return true;
                        }

                        // Preserve the old Horizontal/Vertical joystick dead zone.
                        var stick = device is Gamepad gamepad ? gamepad.leftStick : ((Joystick)device).stick;
                        var position = stick.ReadUnprocessedValue();
                        if (System.Math.Abs(position.x) > 0.19f || System.Math.Abs(position.y) > 0.19f) return true;
                    }
                }

                return false;
            }
        }
    }
}
