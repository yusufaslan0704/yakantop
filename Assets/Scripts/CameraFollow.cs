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

    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        ApplyCursorState();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (!IsLobbyActive())
        {
            ReadLookInput();
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 desiredPosition = target.position + rotation * offset;

        // SmoothDamp hissi: hizli takip, ani durusta az overshoot.
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

        transform.LookAt(target.position + Vector3.up * lookHeight);
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
        }

        if (inputHandler != null && inputHandler.scheme == ControlScheme.Gamepad)
        {
            Gamepad gamepad = GetGamepad();

            if (gamepad == null) return;

            Vector2 look = gamepad.rightStick.ReadValue();

            if (look.sqrMagnitude < 0.01f) return;

            yaw += look.x * gamepadLookSensitivity * Time.deltaTime;
            pitch -= look.y * gamepadLookSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -30f, 50f);

            return;
        }

        // Klavye/mouse kameralarinda imlec kilitliyken mouse okur.
        if (!lockCursor) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -30f, 50f);
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
        // Lobby acikken kamera imleci kilitlemesin; UI tiklanabilir kalsin.
        if (!lockCursor || IsLobbyActive())
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
        pitch = 15f;
    }
}
