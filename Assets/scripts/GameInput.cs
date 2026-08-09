using UnityEngine;
using UnityEngine.InputSystem;

// Single place that merges keyboard/mouse and gamepad, so every interactable
// reads one API instead of each script hard-coding a key.
public static class GameInput
{
    public const float StickDeadzone = 0.18f;
    public const float LookStickSpeed = 260f;

    public static bool GamepadPresent
    {
        get { return Gamepad.current != null; }
    }

    // Combined look delta, already scaled. Mouse is per-frame, stick is per-second.
    public static Vector2 LookDelta(float mouseSensitivity)
    {
        Vector2 delta = Vector2.zero;

        var mouse = Mouse.current;
        if (mouse != null)
            delta += mouse.delta.ReadValue() * mouseSensitivity;

        var pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            if (stick.magnitude > StickDeadzone)
                delta += stick * LookStickSpeed * Time.deltaTime;
        }

        return delta;
    }

    public static Vector2 MoveStick()
    {
        var pad = Gamepad.current;
        if (pad == null)
            return Vector2.zero;

        Vector2 stick = pad.leftStick.ReadValue();
        return stick.magnitude > StickDeadzone ? stick : Vector2.zero;
    }

    public static bool JumpPressed()
    {
        var pad = Gamepad.current;
        return pad != null && pad.buttonSouth.wasPressedThisFrame;
    }

    public static bool RunHeld()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.leftShiftKey.isPressed)
            return true;

        var pad = Gamepad.current;
        return pad != null && (pad.leftStickButton.isPressed || pad.leftTrigger.ReadValue() > 0.5f);
    }

    // E on keyboard, West face button (X / Square) on a pad.
    public static bool InteractPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            return true;

        var pad = Gamepad.current;
        return pad != null && pad.buttonWest.wasPressedThisFrame;
    }

    // Escape on keyboard, East face button (B / Circle) on a pad.
    public static bool BackPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;

        var pad = Gamepad.current;
        return pad != null && pad.buttonEast.wasPressedThisFrame;
    }
}
