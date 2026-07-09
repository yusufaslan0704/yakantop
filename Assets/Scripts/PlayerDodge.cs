using UnityEngine;

// Dodge skill: kisa bir pencere acilir; bu surede gelen top savrulur, oyuncu elenmez.
// Dash'ten farkli: dash hareket, dodge savunma. Klavye Q / gamepad LB.
public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge Window")]
    public float dodgeWindowDuration = 0.28f;
    public float dodgeCooldown = 2.1f;

    [Header("Deflect")]
    [Tooltip("Savrulan topun hiz carpani.")]
    public float deflectSpeedMultiplier = 1.18f;

    [Header("Feedback")]
    public float dodgeShakeDuration = 0.07f;
    public float dodgeShakeStrength = 0.14f;
    public float deflectShakeDuration = 0.1f;
    public float deflectShakeStrength = 0.2f;
    public float dodgeFovPunch = 3.5f;

    public bool IsDodgeActive { get; private set; }

    public event System.Action OnDodgeStarted;

    private float dodgeEndTime;
    private float nextDodgeTime;

    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;
    private PlayerRole playerRole;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
    }

    void Update()
    {
        IsDodgeActive = Time.time < dodgeEndTime;

        if (!CanUseDodge()) return;

        bool dodgeInput = inputHandler != null
            ? inputHandler.DodgePressed
            : Input.GetKeyDown(KeyCode.Q);

        if (dodgeInput && Time.time >= nextDodgeTime && !IsDodgeActive)
        {
            StartDodge();
        }
    }

    bool CanUseDodge()
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;

        // Sadece kosucu ve saver dodge atabilir.
        if (playerRole != null &&
            playerRole.roleType != RoleType.Runner &&
            playerRole.roleType != RoleType.Saver)
        {
            return false;
        }

        return true;
    }

    void StartDodge()
    {
        IsDodgeActive = true;
        dodgeEndTime = Time.time + dodgeWindowDuration;
        nextDodgeTime = Time.time + dodgeCooldown;

        OnDodgeStarted?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDodge();
        }

        CameraShake.ShakeAll(dodgeShakeDuration, dodgeShakeStrength);
        CameraShake.PunchFovAll(dodgeFovPunch, 0.12f);
    }

    public void OnBallDeflected()
    {
        // Basarili savurma: pencereyi kapatabiliriz (spam onleme).
        dodgeEndTime = Time.time;
        IsDodgeActive = false;

        CameraShake.ShakeAll(deflectShakeDuration, deflectShakeStrength);
        CameraShake.PunchFovAll(dodgeFovPunch + 1.5f, 0.14f);
    }

    public float GetCooldownPercent()
    {
        if (dodgeCooldown <= 0f) return 1f;

        float remaining = nextDodgeTime - Time.time;

        if (remaining <= 0f) return 1f;

        return 1f - Mathf.Clamp01(remaining / dodgeCooldown);
    }

    public bool IsReady()
    {
        return Time.time >= nextDodgeTime && !IsDodgeActive;
    }

    void OnDisable()
    {
        IsDodgeActive = false;
        dodgeEndTime = 0f;
    }
}
