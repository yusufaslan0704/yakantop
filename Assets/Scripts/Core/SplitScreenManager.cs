using UnityEngine;
using UnityEngine.InputSystem;

public enum ArenaCameraLayout
{
    Split,
    ThrowerFull,
    RunnerFull
}

// Kamera duzeni: bolunmus ekran (varsayilan), atici tam ekran, kosucu tam ekran.
// Tab ile gecis yapilir. Gamepad yokken bile sag yarida atici gorunur.
[DefaultExecutionOrder(-50)]
public class SplitScreenManager : MonoBehaviour
{
    public static SplitScreenManager Instance { get; private set; }

    [Header("Cameras")]
    public Camera runnerTeamCamera;
    public CameraFollow runnerTeamFollow;

    [Header("Layout")]
    public ArenaCameraLayout layout = ArenaCameraLayout.Split;

    public Rect runnerViewport = new Rect(0f, 0f, 0.5f, 1f);
    public Rect throwerViewport = new Rect(0.5f, 0f, 0.5f, 1f);

    [Header("Thrower Camera")]
    public Vector3 throwerOffset = new Vector3(0f, 2.35f, -4.4f);
    public float throwerMouseSensitivity = 3.6f;
    public float throwerDefaultPitch = 6f;
    public float throwerMinPitch = -42f;
    public float throwerMaxPitch = 55f;

    private Camera throwerCamera;
    private CameraFollow throwerFollow;
    private Transform throwerPlayer;
    private PlayerInputHandler throwerInput;

    void Awake()
    {
        Instance = this;

        if (runnerTeamCamera == null)
        {
            runnerTeamCamera = Camera.main;
        }

        if (runnerTeamFollow == null && runnerTeamCamera != null)
        {
            runnerTeamFollow = runnerTeamCamera.GetComponent<CameraFollow>();
        }
    }

    void Start()
    {
        FindThrowerReferences();
        RefreshLayout();
    }

    void Update()
    {
        // Lobby acikken kamera layout degistirme / imlec kilitleme olmasin.
        if (MatchLobbyManager.Instance != null && MatchLobbyManager.Instance.IsLobbyVisible)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            CycleLayout();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void FindThrowerReferences()
    {
        if (throwerPlayer != null) return;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Thrower) continue;

            throwerPlayer = player.transform;
            throwerInput = player.GetComponent<PlayerInputHandler>();
            break;
        }
    }

    public void CycleLayout()
    {
        layout = (ArenaCameraLayout)(((int)layout + 1) % 3);
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (runnerTeamCamera == null) return;

        FindThrowerReferences();
        EnsureThrowerCamera();

        switch (layout)
        {
            case ArenaCameraLayout.Split:
                ApplySplitScreen();
                break;
            case ArenaCameraLayout.ThrowerFull:
                ApplyThrowerFullScreen();
                break;
            case ArenaCameraLayout.RunnerFull:
                ApplyRunnerFullScreen();
                break;
        }
    }

    public bool IsThrowerViewVisible()
    {
        return layout == ArenaCameraLayout.Split || layout == ArenaCameraLayout.ThrowerFull;
    }

    public Camera GetThrowerCamera()
    {
        EnsureThrowerCamera();
        return throwerCamera;
    }

    void ApplySplitScreen()
    {
        runnerTeamCamera.enabled = true;
        runnerTeamCamera.rect = runnerViewport;

        throwerCamera.gameObject.SetActive(true);
        throwerCamera.enabled = true;
        throwerCamera.rect = throwerViewport;

        // Klavye atici: sag panelde mouse look acik olsun.
        SetupThrowerFollow(mouseLook: IsKeyboardControlledThrower());
        WireRunnerTeamCameraInput(lockCursor: !IsKeyboardControlledThrower());
        WireThrowerMovementCamera();
        ApplyRunnerInvisibleCulling(runnerTeamCamera);
        ApplyKeyboardFocusForLayout();
    }

    void ApplyThrowerFullScreen()
    {
        runnerTeamCamera.enabled = false;

        throwerCamera.gameObject.SetActive(true);
        throwerCamera.enabled = true;
        throwerCamera.rect = new Rect(0f, 0f, 1f, 1f);

        SetupThrowerFollow(mouseLook: IsKeyboardControlledThrower());
        WireThrowerMovementCamera();
        ApplyKeyboardFocusForLayout();

        if (runnerTeamFollow != null)
        {
            runnerTeamFollow.lockCursor = false;
        }
    }

    void ApplyRunnerFullScreen()
    {
        runnerTeamCamera.enabled = true;
        runnerTeamCamera.rect = new Rect(0f, 0f, 1f, 1f);
        ApplyRunnerInvisibleCulling(runnerTeamCamera);

        if (throwerCamera != null)
        {
            throwerCamera.gameObject.SetActive(false);
        }

        WireRunnerTeamCameraInput(lockCursor: true);
        ApplyKeyboardFocusForLayout();

        if (throwerFollow != null)
        {
            throwerFollow.lockCursor = false;
        }
    }

    void SetupThrowerFollow(bool mouseLook)
    {
        if (throwerFollow == null || throwerPlayer == null) return;

        throwerFollow.target = throwerPlayer;
        throwerFollow.offset = throwerOffset;
        throwerFollow.useLookRotation = true;
        throwerFollow.defaultPitch = throwerDefaultPitch;
        throwerFollow.minPitch = throwerMinPitch;
        throwerFollow.maxPitch = throwerMaxPitch;
        throwerFollow.SnapToTargetFacing();

        if (HasThrowerGamepad())
        {
            throwerFollow.inputHandler = throwerInput;
            throwerFollow.lockCursor = false;
            throwerFollow.mouseSensitivity = 0f;
        }
        else if (mouseLook)
        {
            throwerFollow.inputHandler = null;
            throwerFollow.lockCursor = true;
            throwerFollow.mouseSensitivity = throwerMouseSensitivity;
        }
        else
        {
            // Bolunmus ekranda sol taraf klavye; sag atici kamerasi sadece takip eder.
            throwerFollow.inputHandler = null;
            throwerFollow.lockCursor = false;
            throwerFollow.mouseSensitivity = 0f;
        }
    }

    bool IsKeyboardControlledThrower()
    {
        return throwerInput != null &&
               throwerInput.scheme == ControlScheme.KeyboardMouse &&
               throwerInput.enabled;
    }

    bool HasThrowerGamepad()
    {
        if (throwerInput == null) return false;
        if (throwerInput.scheme != ControlScheme.Gamepad) return false;

        int index = throwerInput.gamepadIndex;
        return index >= 0 && index < Gamepad.all.Count;
    }

    void EnsureThrowerCamera()
    {
        if (throwerCamera != null) return;

        GameObject camObject = new GameObject("ThrowerCamera");
        camObject.transform.SetParent(transform, false);

        throwerCamera = camObject.AddComponent<Camera>();
        throwerCamera.CopyFrom(runnerTeamCamera);
        throwerCamera.depth = runnerTeamCamera.depth + 1;
        ApplyThrowerInvisibleCulling(throwerCamera);

        AudioListener listener = camObject.GetComponent<AudioListener>();

        if (listener != null)
        {
            Destroy(listener);
        }

        throwerFollow = camObject.AddComponent<CameraFollow>();
        throwerFollow.offset = throwerOffset;
        throwerFollow.gamepadLookSensitivity = 140f;
        throwerFollow.lockCursor = false;

        camObject.AddComponent<CameraShake>();
    }

    public void EnsureThrowerCameraReady()
    {
        FindThrowerReferences();
        EnsureThrowerCamera();
        ApplyThrowerInvisibleCulling(throwerCamera);
    }

    public static void RefreshThrowerCulling()
    {
        if (Instance == null) return;
        Instance.EnsureThrowerCamera();
        ApplyThrowerInvisibleCulling(Instance.throwerCamera);
    }

    public static void RefreshRunnerCulling()
    {
        if (Instance == null) return;
        ApplyRunnerInvisibleCulling(Instance.runnerTeamCamera);
    }

    static void ApplyThrowerInvisibleCulling(Camera cam)
    {
        if (cam == null) return;

        int layer = PlayerInvisibility.GetInvisibleLayer();
        if (layer < 0) return;

        // Atıcı kamerası görünmez kaçanları hiç çizmez.
        cam.cullingMask &= ~(1 << layer);
    }

    static void ApplyRunnerInvisibleCulling(Camera cam)
    {
        if (cam == null) return;

        int layer = PlayerThrowerInvis.GetInvisibleLayer();
        if (layer < 0) return;

        // Kacan kamerasi görünmez atıcıyı hiç çizmez.
        cam.cullingMask &= ~(1 << layer);
    }

    void WireRunnerTeamCameraInput(bool lockCursor)
    {
        if (runnerTeamFollow == null) return;

        PlayerRole runner = FindHumanControlled(RoleType.Runner);
        PlayerRole saver = FindHumanControlled(RoleType.Saver);

        PlayerInputHandler handler = runner != null
            ? runner.GetComponent<PlayerInputHandler>()
            : null;

        if (handler == null && saver != null)
        {
            handler = saver.GetComponent<PlayerInputHandler>();
        }

        runnerTeamFollow.inputHandler = handler;
        runnerTeamFollow.lockCursor = lockCursor;
    }

    void WireThrowerMovementCamera()
    {
        if (throwerPlayer == null || throwerCamera == null) return;

        PlayerMovement movement = throwerPlayer.GetComponent<PlayerMovement>();

        // Kamera her zaman baglansin; hareket sonradan acilsa da hazir olsun.
        if (movement != null)
        {
            movement.cameraTransform = throwerCamera.transform;
        }

        PlayerThrow throwScript = throwerPlayer.GetComponent<PlayerThrow>();

        if (throwScript != null)
        {
            throwScript.playerCamera = throwerCamera;
        }
    }

    // Tek klavyede: Split = ok tuslari atici / WASD kosucu.
    // ThrowerFull = WASD atici, kosucu hareketi kilit.
    void ApplyKeyboardFocusForLayout()
    {
        bool keyboardThrower = IsKeyboardControlledThrower();
        bool throwerFocused = layout == ArenaCameraLayout.ThrowerFull;

        if (throwerInput != null && keyboardThrower)
        {
            // Split: ok tuslari. Tam ekran atici: WASD.
            throwerInput.useArrowKeysForMove = !throwerFocused;
            throwerInput.suppressMoveInput = false;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Runner && player.roleType != RoleType.Saver)
            {
                continue;
            }

            PlayerInputHandler handler = player.GetComponent<PlayerInputHandler>();
            if (handler == null || !handler.enabled)
            {
                continue;
            }

            if (handler.scheme != ControlScheme.KeyboardMouse)
            {
                handler.suppressMoveInput = false;
                continue;
            }

            handler.suppressMoveInput = keyboardThrower && throwerFocused;
        }
    }

    static PlayerRole FindHumanControlled(RoleType role)
    {
        PlayerRole fallback = null;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != role) continue;

            if (fallback == null)
            {
                fallback = player;
            }

            if (player.GetComponent<PlayerInputHandler>() != null)
            {
                return player;
            }
        }

        return fallback;
    }
}
