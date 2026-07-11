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

    [Tooltip("Aciksa WASD yerine ok tuslari (atici klavye kontrolu).")]
    public bool useArrowKeysForMove = false;

    [Tooltip("Aciksa hareket girdisi yok sayilir (or. ThrowerFull iken kosucu).")]
    public bool suppressMoveInput = false;

    [Header("Gamepad Settings")]
    [Range(0f, 0.5f)] public float stickDeadzone = 0.15f;

    PlayerRole playerRole;

    public Vector2 MoveInput { get; private set; }
    public bool DashPressed { get; private set; }
    public bool DodgePressed { get; private set; }
    public bool FlashPressed { get; private set; }
    public bool ShieldPressed { get; private set; }
    public bool InvisibilityPressed { get; private set; }
    public bool DecoyPressed { get; private set; }
    public bool VolleyPressed { get; private set; }
    public bool VolleyHeld { get; private set; }
    public bool VolleyReleased { get; private set; }
    public bool TrapPressed { get; private set; }
    public bool FakePressed { get; private set; }
    public bool ThrowPressed { get; private set; }
    public bool ThrowReleased { get; private set; }
    public bool ReviveHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DuckHeld { get; private set; }

    // Bu kare basilan emote (0-3 hotkey / D-Pad). Basilmadiysa -1.
    public int EmoteIndex { get; private set; } = -1;

    // Emote cemberi: T / Select basili tut.
    public bool EmoteWheelHeld { get; private set; }
    public bool EmoteWheelReleased { get; private set; }
    public Vector2 EmoteWheelStick { get; private set; }

    void Update()
    {
        if (playerRole == null)
        {
            playerRole = GetComponent<PlayerRole>();
        }

        if (scheme == ControlScheme.KeyboardMouse)
        {
            ReadKeyboardMouse();
        }
        else
        {
            ReadGamepad();
        }
    }

    bool IsThrowerRole()
    {
        return playerRole != null && playerRole.roleType == RoleType.Thrower;
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

        if (useArrowKeysForMove)
        {
            if (keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.upArrowKey.isPressed) move.y += 1f;
        }
        else
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
        }

        if (suppressMoveInput)
        {
            move = Vector2.zero;
        }

        MoveInput = Vector2.ClampMagnitude(move, 1f);

        DashPressed = keyboard.leftShiftKey.wasPressedThisFrame;
        DodgePressed = keyboard.qKey.wasPressedThisFrame;
        FlashPressed = keyboard.eKey.wasPressedThisFrame;
        ShieldPressed = keyboard.gKey.wasPressedThisFrame;
        InvisibilityPressed = keyboard.vKey.wasPressedThisFrame;
        // Decoy: X (R = GameManager round/mac reset).
        DecoyPressed = keyboard.xKey.wasPressedThisFrame;
        ReviveHeld = keyboard.fKey.isPressed;

        JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
        DuckHeld = keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed;

        // Atıcı Fake: Q (kacan dodge ile ayni tus; atıcıda dodge yok).
        FakePressed = IsThrowerRole() && keyboard.qKey.wasPressedThisFrame;

        EmoteIndex = -1;
        // Atıcıda 1-5 top secimi; emote sadece T cemberi.
        if (!IsThrowerRole())
        {
            if (keyboard.digit1Key.wasPressedThisFrame) EmoteIndex = 0;
            else if (keyboard.digit2Key.wasPressedThisFrame) EmoteIndex = 1;
            else if (keyboard.digit3Key.wasPressedThisFrame) EmoteIndex = 2;
            else if (keyboard.digit4Key.wasPressedThisFrame) EmoteIndex = 3;
        }

        EmoteWheelHeld = keyboard.tKey.isPressed;
        EmoteWheelReleased = keyboard.tKey.wasReleasedThisFrame;
        EmoteWheelStick = Vector2.zero;

        ThrowPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        ThrowReleased = mouse != null && mouse.leftButton.wasReleasedThisFrame;
        // Thrower: sag tik — Volley (basili degilse) / Yuksek atis (mod 7, basili tut).
        VolleyPressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
        VolleyHeld = mouse != null && mouse.rightButton.isPressed;
        VolleyReleased = mouse != null && mouse.rightButton.wasReleasedThisFrame;
        // Thrower trap: orta mouse veya B (F = Saver revive).
        TrapPressed = keyboard.bKey.wasPressedThisFrame ||
                      (mouse != null && mouse.middleButton.wasPressedThisFrame);
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

        if (suppressMoveInput)
        {
            stick = Vector2.zero;
        }

        MoveInput = Vector2.ClampMagnitude(stick, 1f);

        // Dash: RB veya B.
        DashPressed = gamepad.rightShoulder.wasPressedThisFrame ||
                      gamepad.buttonEast.wasPressedThisFrame;

        // Dodge: LB (dash'ten ayri).
        DodgePressed = gamepad.leftShoulder.wasPressedThisFrame;

        // Flash: Y / Triangle.
        FlashPressed = gamepad.buttonNorth.wasPressedThisFrame;

        // Shield: right stick click.
        ShieldPressed = gamepad.rightStickButton.wasPressedThisFrame;

        // Invisibility: left stick click.
        InvisibilityPressed = gamepad.leftStickButton.wasPressedThisFrame;

        // Decoy (Runner): X / Square — Runner'da throw/revive yok.
        DecoyPressed = gamepad.buttonWest.wasPressedThisFrame;

        // Volley (Thrower): LB — Runner dodge ayri component; Thrower'da serbest.
        // Yuksek atis (mod 7): LB basili tut.
        VolleyPressed = gamepad.leftShoulder.wasPressedThisFrame;
        VolleyHeld = gamepad.leftShoulder.isPressed;
        VolleyReleased = gamepad.leftShoulder.wasReleasedThisFrame;

        // Trap (Thrower): R3 — Runner shield R3; Thrower'da shield yok.
        TrapPressed = gamepad.rightStickButton.wasPressedThisFrame;

        // Fake (Thrower): Y / Triangle — Runner flash ile ayni tus; Thrower'da flash yok.
        FakePressed = IsThrowerRole() && gamepad.buttonNorth.wasPressedThisFrame;

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

        // Emote: D-Pad yonleri (hizli secim). Atıcıda D-Pad top cevirir.
        EmoteIndex = -1;
        if (!IsThrowerRole())
        {
            if (gamepad.dpad.up.wasPressedThisFrame) EmoteIndex = 0;
            else if (gamepad.dpad.right.wasPressedThisFrame) EmoteIndex = 1;
            else if (gamepad.dpad.down.wasPressedThisFrame) EmoteIndex = 2;
            else if (gamepad.dpad.left.wasPressedThisFrame) EmoteIndex = 3;
        }

        // Emote cemberi: Select / Back / View.
        EmoteWheelHeld = gamepad.selectButton.isPressed;
        EmoteWheelReleased = gamepad.selectButton.wasReleasedThisFrame;
        EmoteWheelStick = gamepad.rightStick.ReadValue();
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
        DodgePressed = false;
        FlashPressed = false;
        ShieldPressed = false;
        InvisibilityPressed = false;
        DecoyPressed = false;
        VolleyPressed = false;
        VolleyHeld = false;
        VolleyReleased = false;
        TrapPressed = false;
        FakePressed = false;
        ThrowPressed = false;
        ThrowReleased = false;
        ReviveHeld = false;
        JumpPressed = false;
        DuckHeld = false;
        EmoteIndex = -1;
        EmoteWheelHeld = false;
        EmoteWheelReleased = false;
        EmoteWheelStick = Vector2.zero;
    }
}
