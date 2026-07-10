using UnityEngine;

// Thrower yetenegi: dar yayilan 3 Fast top (sol / orta / sag).
// Tus: sag tik / gamepad LB.
// Menzil: Inspector'daki throwForce + loft ile ayarlanir.
public class PlayerVolley : MonoBehaviour
{
    [Header("Volley")]
    public float cooldown = 11f;
    [Tooltip("Toplam yay acisi (sol-sag). Kucuk tut: 18-24° ideal.")]
    public float spreadDegrees = 20f;
    public int ballCount = 3;

    [Header("Ball")]
    [Tooltip("Volley simdilik sadece Fast top atar.")]
    public BallData fastBallData;

    [Header("Menzil")]
    [Tooltip("Atis gucu. Buyuk = daha uzun menzil. Tipik: 45 (kisa) … 70 (uzun).")]
    [Range(20f, 90f)]
    public float throwForce = 62f;
    [Tooltip("Hafif yukari egim. Top erken yere dusmesin diye menzili uzatir.")]
    [Range(0f, 0.35f)]
    public float loft = 0.12f;

    [Header("Feel")]
    public float shakeDuration = 0.1f;
    public float shakeStrength = 0.14f;

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
        if (!CanUse())
        {
            return;
        }

        if (inputHandler != null && inputHandler.enabled && inputHandler.VolleyPressed && IsReady)
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

        BallData ball = ResolveFastBall();
        if (ball == null || ball.prefab == null)
        {
            Debug.LogWarning("PlayerVolley: Fast BallData atanmamis.", this);
            return;
        }

        if (!playerThrow.TryThrowSpread(spreadDegrees, ballCount, throwForce, ball, loft))
        {
            return;
        }

        nextReadyTime = Time.unscaledTime + cooldown;
        IsFiring = true;
        CameraShake.ShakeAll(shakeDuration, shakeStrength);

        CancelInvoke(nameof(ClearFiring));
        Invoke(nameof(ClearFiring), 0.35f);
    }

    BallData ResolveFastBall()
    {
        if (fastBallData != null)
        {
            return fastBallData;
        }

        BallData[] all = Resources.FindObjectsOfTypeAll<BallData>();
        for (int i = 0; i < all.Length; i++)
        {
            BallData d = all[i];
            if (d == null) continue;
            if (d.name.IndexOf("Fast", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(d.ballName) &&
                 d.ballName.IndexOf("Fast", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return d;
            }
        }

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
