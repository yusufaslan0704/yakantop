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
    public ArenaCameraLayout layout = ArenaCameraLayout.RunnerFull;

    public Rect runnerViewport = new Rect(0f, 0f, 0.5f, 1f);
    public Rect throwerViewport = new Rect(0.5f, 0f, 0.5f, 1f);

    [Header("Thrower Camera")]
    public Vector3 throwerOffset = new Vector3(0f, 3.8f, -6.5f);
    public float throwerMouseSensitivity = 2.8f;

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

    void ApplySplitScreen()
    {
        runnerTeamCamera.enabled = true;
        runnerTeamCamera.rect = runnerViewport;

        throwerCamera.gameObject.SetActive(true);
        throwerCamera.enabled = true;
        throwerCamera.rect = throwerViewport;

        SetupThrowerFollow(mouseLook: false);
        WireRunnerTeamCameraInput(lockCursor: true);
        WireThrowerMovementCamera();
    }

    void ApplyThrowerFullScreen()
    {
        runnerTeamCamera.enabled = false;

        throwerCamera.gameObject.SetActive(true);
        throwerCamera.enabled = true;
        throwerCamera.rect = new Rect(0f, 0f, 1f, 1f);

        bool gamepadThrower = HasThrowerGamepad();
        SetupThrowerFollow(mouseLook: !gamepadThrower);
        WireThrowerMovementCamera();

        if (runnerTeamFollow != null)
        {
            runnerTeamFollow.lockCursor = false;
        }
    }

    void ApplyRunnerFullScreen()
    {
        runnerTeamCamera.enabled = true;
        runnerTeamCamera.rect = new Rect(0f, 0f, 1f, 1f);

        if (throwerCamera != null)
        {
            throwerCamera.gameObject.SetActive(false);
        }

        WireRunnerTeamCameraInput(lockCursor: true);

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

    bool HasThrowerGamepad()
    {
        if (throwerInput == null) return false;

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

        if (movement != null && movement.enabled)
        {
            movement.cameraTransform = throwerCamera.transform;
        }

        PlayerThrow throwScript = throwerPlayer.GetComponent<PlayerThrow>();

        if (throwScript != null)
        {
            throwScript.playerCamera = throwerCamera;
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
