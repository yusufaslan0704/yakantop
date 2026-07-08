using UnityEngine;
using UnityEngine.InputSystem;

public enum ControlScheme
{
    KeyboardMouse,
    Gamepad
}

// Tum oyuncu girislerini tek yerden okur.
// Hareket/dash/atis scriptleri cihazi degil bu bileseni dinler;
// boylece ayni karakter klavyeyle de gamepad'le de oynanabilir.
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Control")]
    public ControlScheme scheme = ControlScheme.KeyboardMouse;

    [Tooltip("Birden fazla gamepad bagliysa bu oyuncunun kullanacagi gamepad (0 = ilk).")]
    public int gamepadIndex = 0;

    [Header("Gamepad Settings")]
    [Range(0f, 0.5f)] public float stickDeadzone = 0.15f;

    public Vector2 MoveInput { get; private set; }
    public bool DashPressed { get; private set; }
    public bool ThrowPressed { get; private set; }
    public bool ThrowReleased { get; private set; }
    public bool ReviveHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DuckHeld { get; private set; }

    // Bu kare basilan emote (0-3). Basilmadiysa -1.
    public int EmoteIndex { get; private set; } = -1;

    void Update()
    {
        if (scheme == ControlScheme.KeyboardMouse)
        {
            ReadKeyboardMouse();
        }
        else
        {
            ReadGamepad();
        }
    }

    void ReadKeyboardMouse()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null)
        {
            ClearInput();
            return;
        }

        Vector2 move = Vector2.zero;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;

        MoveInput = Vector2.ClampMagnitude(move, 1f);

        DashPressed = keyboard.leftShiftKey.wasPressedThisFrame;
        ReviveHeld = keyboard.fKey.isPressed;

        JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
        DuckHeld = keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed;

        EmoteIndex = -1;
        if (keyboard.digit1Key.wasPressedThisFrame) EmoteIndex = 0;
        else if (keyboard.digit2Key.wasPressedThisFrame) EmoteIndex = 1;
        else if (keyboard.digit3Key.wasPressedThisFrame) EmoteIndex = 2;
        else if (keyboard.digit4Key.wasPressedThisFrame) EmoteIndex = 3;

        ThrowPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        ThrowReleased = mouse != null && mouse.leftButton.wasReleasedThisFrame;
    }

    void ReadGamepad()
    {
        Gamepad gamepad = GetGamepad();

        if (gamepad == null)
        {
            ClearInput();
            return;
        }

        Vector2 stick = gamepad.leftStick.ReadValue();

        // Ham stick degerinde deadzone islenmez, kendimiz uyguluyoruz.
        if (stick.magnitude < stickDeadzone)
        {
            stick = Vector2.zero;
        }

        MoveInput = Vector2.ClampMagnitude(stick, 1f);

        // Dash: RB veya B.
        DashPressed = gamepad.rightShoulder.wasPressedThisFrame ||
                      gamepad.buttonEast.wasPressedThisFrame;

        // Atis: RT veya X (basili tut = sarj, birak = at).
        ThrowPressed = gamepad.rightTrigger.wasPressedThisFrame ||
                       gamepad.buttonWest.wasPressedThisFrame;

        ThrowReleased = gamepad.rightTrigger.wasReleasedThisFrame ||
                        gamepad.buttonWest.wasReleasedThisFrame;

        // Ziplama: A. (Revive X'e tasindi; roller farkli oldugu icin
        // X'in atisla cakismasi sorun degil: Saver atamaz, Thrower revive edemez.)
        JumpPressed = gamepad.buttonSouth.wasPressedThisFrame;
        ReviveHeld = gamepad.buttonWest.isPressed;

        // Egilme: LT basili tut.
        DuckHeld = gamepad.leftTrigger.isPressed;

        // Emote: D-Pad yonleri.
        EmoteIndex = -1;
        if (gamepad.dpad.up.wasPressedThisFrame) EmoteIndex = 0;
        else if (gamepad.dpad.right.wasPressedThisFrame) EmoteIndex = 1;
        else if (gamepad.dpad.down.wasPressedThisFrame) EmoteIndex = 2;
        else if (gamepad.dpad.left.wasPressedThisFrame) EmoteIndex = 3;
    }

    Gamepad GetGamepad()
    {
        if (gamepadIndex < 0 || gamepadIndex >= Gamepad.all.Count)
        {
            return null;
        }

        return Gamepad.all[gamepadIndex];
    }

    void ClearInput()
    {
        MoveInput = Vector2.zero;
        DashPressed = false;
        ThrowPressed = false;
        ThrowReleased = false;
        ReviveHeld = false;
        JumpPressed = false;
        DuckHeld = false;
        EmoteIndex = -1;
    }
}
