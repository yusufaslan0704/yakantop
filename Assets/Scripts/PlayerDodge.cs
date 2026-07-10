using UnityEngine;

// Dodge = carpma aninda timing (i-frame YOK).
// Q sadece basilan ani kaydeder.
// Top carptiginda: Q cok kisa sure once basildiysa 1 kez savrulur; degilse Eliminate calisir.
public class PlayerDodge : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Carpma anina gore Q toleransi (~2 frame).")]
    [SerializeField] float parryTimingSeconds = 0.04f;

    [SerializeField] float cooldownSeconds = 3.2f;

    [Header("Deflect")]
    [SerializeField] float deflectSpeedMultiplier = 1.18f;

    [Header("Feedback")]
    [SerializeField] float dodgeShakeDuration = 0.06f;
    [SerializeField] float dodgeShakeStrength = 0.12f;
    [SerializeField] float deflectShakeDuration = 0.1f;
    [SerializeField] float deflectShakeStrength = 0.2f;
    [SerializeField] float dodgeFovPunch = 3.5f;

    // Sadece animasyon/UI. Ball bunu koruma icin KULLANMAZ.
    public bool IsDodgeActive { get; private set; }

    public event System.Action OnDodgeStarted;

    float nextDodgeTime;
    float lastPressUnscaledTime = -999f;
    bool pressConsumed = true; // baslangicta tuketilmis: Q yoksa asla parry yok
    float animFlashUntil;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        pressConsumed = true;
        lastPressUnscaledTime = -999f;
    }

    void OnEnable()
    {
        // Enable/disable sonrasi eski basisi kullanma.
        pressConsumed = true;
        lastPressUnscaledTime = -999f;
        IsDodgeActive = false;
    }

    void Update()
    {
        IsDodgeActive = Time.unscaledTime < animFlashUntil;

        if (!CanUseDodge())
        {
            return;
        }

        if (!WasDodgePressedThisFrame())
        {
            return;
        }

        if (Time.unscaledTime < nextDodgeTime)
        {
            return;
        }

        RegisterPress();
    }

    bool CanUseDodge()
    {
        if (!isActiveAndEnabled) return false;
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;

        if (playerRole != null &&
            playerRole.roleType != RoleType.Runner &&
            playerRole.roleType != RoleType.Saver)
        {
            return false;
        }

        return true;
    }

    bool WasDodgePressedThisFrame()
    {
        // Sadece bu karakterin kendi input handler'i.
        // Global Keyboard.q / Gamepad.LB okuma: botlar da "Q basildi" sanilip
        // yanlis parry alabiliyordu.
        return inputHandler != null && inputHandler.enabled && inputHandler.DodgePressed;
    }

    void RegisterPress()
    {
        nextDodgeTime = Time.unscaledTime + cooldownSeconds;
        lastPressUnscaledTime = Time.unscaledTime;
        pressConsumed = false;
        animFlashUntil = Time.unscaledTime + 0.08f;
        IsDodgeActive = true;

        OnDodgeStarted?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDodge();
        }

        CameraShake.ShakeAll(dodgeShakeDuration, dodgeShakeStrength);
        CameraShake.PunchFovAll(dodgeFovPunch, 0.1f);
    }

    // Ball carpma aninda. true = 1 kez dodge. false = Eliminate yolu.
    public bool TryConsumeParry(Ball ball)
    {
        if (!isActiveAndEnabled) return false;
        if (ball == null) return false;
        if (pressConsumed) return false;

        float age = Time.unscaledTime - lastPressUnscaledTime;
        if (age < 0f || age > parryTimingSeconds)
        {
            return false;
        }

        // Bu basisi tuket — sonraki top mutlaka vurur.
        pressConsumed = true;
        IsDodgeActive = false;
        animFlashUntil = 0f;

        DeflectBall(ball);
        return true;
    }

    public void OnBallDeflected()
    {
        pressConsumed = true;
        IsDodgeActive = false;
        animFlashUntil = 0f;
    }

    void DeflectBall(Ball ball)
    {
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        Vector3 deflectDirection = ball.transform.position - transform.position;
        deflectDirection.y = 0.15f;

        if (deflectDirection.sqrMagnitude < 0.0001f)
        {
            deflectDirection = -transform.forward;
            deflectDirection.y = 0.15f;
        }

        deflectDirection.Normalize();

        float speed = Mathf.Max(12f, ballRb.linearVelocity.magnitude);
        ballRb.linearVelocity = deflectDirection * speed * deflectSpeedMultiplier;

        CombatVfx.SpawnParryRing(transform.position);
        CameraShake.ShakeAll(deflectShakeDuration, deflectShakeStrength);
        CameraShake.PunchFovAll(dodgeFovPunch + 1.5f, 0.12f);
    }

    public float GetCooldownPercent()
    {
        if (cooldownSeconds <= 0f) return 1f;

        float remaining = nextDodgeTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;

        return 1f - Mathf.Clamp01(remaining / cooldownSeconds);
    }

    public bool IsReady()
    {
        return Time.unscaledTime >= nextDodgeTime;
    }

    void OnDisable()
    {
        IsDodgeActive = false;
        pressConsumed = true;
        animFlashUntil = 0f;
        lastPressUnscaledTime = -999f;
    }
}
