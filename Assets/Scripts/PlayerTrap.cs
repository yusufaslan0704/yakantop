using UnityEngine;

// Thrower yetenegi: yere dusunce yavaslatma alani birakan Trap Ball.
// Tus: B / orta mouse | gamepad: R3 (sag stick click).
public class PlayerTrap : MonoBehaviour
{
    [Header("Trap")]
    public float cooldown = 12f;
    public float trapRadius = 3.2f;
    public float trapDuration = 4.5f;
    [Range(0.2f, 0.9f)]
    public float speedMultiplier = 0.42f;

    [Header("Throw")]
    public BallData ballData;
    [Range(20f, 80f)]
    public float throwForce = 48f;
    [Range(0f, 0.4f)]
    public float loft = 0.18f;

    [Header("Feel")]
    public float shakeDuration = 0.08f;
    public float shakeStrength = 0.1f;
    public Color ballTint = new Color(1f, 0.4f, 0.15f, 1f);

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !IsFiring;
    public bool IsFiring { get; private set; }

    float nextReadyTime;

    PlayerThrow playerThrow;
    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;

    void Awake()
    {
        playerThrow = GetComponent<PlayerThrow>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
    }

    void Update()
    {
        if (!CanUse()) return;

        if (inputHandler != null && inputHandler.enabled && inputHandler.TrapPressed && IsReady)
        {
            Fire();
        }
    }

    bool CanUse()
    {
        if (!isActiveAndEnabled) return false;
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (playerThrow == null || !playerThrow.enabled) return false;
        if (EmoteWheelUI.IsOpen) return false;
        return true;
    }

    void Fire()
    {
        if (playerThrow == null) return;

        BallData data = ResolveBallData();
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("PlayerTrap: BallData/prefab eksik.", this);
            return;
        }

        if (!playerThrow.TryThrowAbilityBall(data, throwForce, loft, ConfigureTrapBall))
        {
            return;
        }

        nextReadyTime = Time.unscaledTime + cooldown;
        IsFiring = true;
        CameraShake.ShakeAll(shakeDuration, shakeStrength);

        CancelInvoke(nameof(ClearFiring));
        Invoke(nameof(ClearFiring), 0.4f);
    }

    void ConfigureTrapBall(Ball ball)
    {
        if (ball == null) return;
        ball.EnableTrapOnImpact(trapRadius, trapDuration, speedMultiplier);
        CombatVfx.TintBallTrail(ball.gameObject, ballTint);

        Renderer[] renderers = ball.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            if (mat == null) continue;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ballTint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", ballTint);
            mat.color = ballTint;
        }
    }

    BallData ResolveBallData()
    {
        if (ballData != null) return ballData;
        if (playerThrow != null && playerThrow.ballData != null) return playerThrow.ballData;
        return null;
    }

    void ClearFiring()
    {
        IsFiring = false;
    }

    public float GetCooldownPercent()
    {
        if (IsFiring) return 1f;
        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;
        return 1f - Mathf.Clamp01(remaining / cooldown);
    }
}
