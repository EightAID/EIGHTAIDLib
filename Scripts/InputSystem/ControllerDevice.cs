using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerDevice : MonoBehaviour
{
    public static bool IsGamepad()
    {
        // 全デバイスを取得
        var devices = UnityEngine.InputSystem.InputSystem.devices;

        foreach (var device in devices){
            if (device is Gamepad){//デバイスがゲームパッド(コントローラー)の時だけ処理
                Gamepad gamepad = device as Gamepad;
                Debug.Log($"コントローラー検出: {gamepad.displayName}");
                return true;
            }
        }
        return false;
    }
}
