using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Input")]
    public PlayerInputHandler inputHandler;

    [Tooltip("Klavye/mouse kameralarinda imlec kilitlensin mi?")]
    public bool lockCursor = true;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 3.35f, -5.6f);
    public float mouseSensitivity = 3.2f;
    public float gamepadLookSensitivity = 155f;
    public float followSpeed = 16f;
    public float lookHeight = 1.35f;
    public float defaultPitch = 15f;
    public float minPitch = -30f;
    public float maxPitch = 50f;

    [Tooltip("Aciksa LookAt yerine pitch/yaw ile bak (atici nisan = ekran merkezi).")]
    public bool useLookRotation = false;

    [Header("Elevated Aim (Yuksek Atis)")]
    [Tooltip("Havadayken asagi bakis ust siniri (pozitif pitch = asagi).")]
    public float elevatedMaxPitch = 82f;

    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        pitch = defaultPitch;
        ApplyCursorState();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (!IsLobbyActive() && !EmoteWheelUI.IsOpen)
        {
            ReadLookInput();
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 desiredPosition = target.position + rotation * offset;

        // SmoothDamp hissi: hizli takip, ani durusta az overshoot.
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

        if (useLookRotation)
        {
            // Atıcı: mouse bakisi = nisan yonu (LookAt karaktere asagi cekmesin).
            transform.rotation = rotation;
        }
        else
        {
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }

    void ReadLookInput()
    {
        // Flash: atici bakisi kilitlenir.
        if (target != null)
        {
            PlayerRole role = target.GetComponent<PlayerRole>();
            if (role != null && role.roleType == RoleType.Thrower && PlayerFlash.AreThrowersBlinded())
            {
                return;
            }

            // Egri yol cizimi: mouse kamerayi degil yolu hareket ettirir.
            PlayerThrow throwScript = target.GetComponent<PlayerThrow>();
            if (throwScript != null && throwScript.IsDrawingThrowPath)
            {
                return;
            }
        }

        if (inputHandler != null && inputHandler.scheme == ControlScheme.Gamepad)
        {
            Gamepad gamepad = GetGamepad();

            if (gamepad == null) return;

            Vector2 look = gamepad.rightStick.ReadValue();

            if (look.sqrMagnitude < 0.01f) return;

            yaw += look.x * gamepadLookSensitivity * Time.deltaTime;
            pitch -= look.y * gamepadLookSensitivity * Time.deltaTime;
            ClampPitch();

            return;
        }

        // Klavye/mouse kameralarinda imlec kilitliyken mouse okur.
        if (!lockCursor) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        ClampPitch();
    }

    void ClampPitch()
    {
        float maxP = maxPitch;
        float minP = minPitch;

        if (useLookRotation && target != null)
        {
            PlayerAirLift lift = target.GetComponent<PlayerAirLift>();
            if (lift != null && lift.IsElevated)
            {
                maxP = elevatedMaxPitch;
            }
        }

        pitch = Mathf.Clamp(pitch, minP, maxP);
    }

    static bool IsLobbyActive()
    {
        return MatchLobbyManager.Instance != null && MatchLobbyManager.Instance.IsLobbyVisible;
    }

    Gamepad GetGamepad()
    {
        if (inputHandler == null) return Gamepad.current;

        int index = inputHandler.gamepadIndex;

        if (index < 0 || index >= Gamepad.all.Count)
        {
            return null;
        }

        return Gamepad.all[index];
    }

    void ApplyCursorState()
    {
        // Lobby veya emote cemberi acikken imlec serbest.
        if (!lockCursor || IsLobbyActive() || EmoteWheelUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        ApplyCursorState();
    }

    void OnDisable()
    {
        if (lockCursor && !IsLobbyActive())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Lobby / mac gecisinde disaridan imlec durumunu yenilemek icin.
    public void RefreshCursorState()
    {
        ApplyCursorState();
    }

    // Kamera hedefi degistiginde bakis acisini karaktere hizala.
    public void SnapToTargetFacing()
    {
        if (target == null) return;

        yaw = target.eulerAngles.y;
        pitch = defaultPitch;
    }
}
