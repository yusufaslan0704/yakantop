using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashDistance = 4f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 2f;

    [Header("Dash VFX")]
    public GameObject dashVfxPrefab;
    public float vfxSpawnInterval = 0.04f;
    public Vector3 vfxOffset = new Vector3(0f, 0.7f, 0f);

    [Header("Dash Camera Shake")]
    public float dashShakeDuration = 0.08f;
    public float dashShakeStrength = 0.08f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float nextDashTime = 0f;
    private float nextVfxTime = 0f;

    private Vector3 dashDirection;

    private Rigidbody rb;
    private PlayerHealth playerHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // Elenen oyuncu dash atamaz.
        if (playerHealth != null && playerHealth.isEliminated)
        {
            StopDash();
            return;
        }

        // Sol Shift ile dash.
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime && !isDashing)
        {
            StartDash();
        }

        // Dash sırasında aralıklarla VFX bırak.
        if (isDashing && Time.time >= nextVfxTime)
        {
            SpawnDashVFX();
            nextVfxTime = Time.time + vfxSpawnInterval;
        }
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            return;
        }

        if (rb.isKinematic)
        {
            StopDash();
            return;
        }

        // Dash hızı: toplam mesafeyi toplam sürede alacak sabit hız.
        // Duvara çarparsa fizik motoru karakteri kendiliğinden durdurur.
        float dashSpeed = dashDistance / dashDuration;

        Vector3 velocity = dashDirection * dashSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            StopDash();
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = transform.forward;
        dashDirection.y = 0f;
        dashDirection.Normalize();

        nextDashTime = Time.time + dashCooldown;
        nextVfxTime = Time.time;

        PlayDashFeedback();
    }

    void StopDash()
    {
        if (!isDashing)
        {
            return;
        }

        isDashing = false;

        // Dash bitince kalan hızı sıfırla ki karakter kaymaya devam etmesin.
        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
    }

    void OnDisable()
    {
        // Kontrol başka karaktere geçerse dash yarıda kesilsin.
        StopDash();
    }

    void PlayDashFeedback()
    {
        // Dash sesi.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }

        // Dash kamera shake.
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(dashShakeDuration, dashShakeStrength);
        }
    }

    void SpawnDashVFX()
    {
        if (dashVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + vfxOffset;

        Instantiate(
            dashVfxPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    public float GetDashCooldownPercent()
    {
        if (dashCooldown <= 0f)
        {
            return 1f;
        }

        float remainingTime = nextDashTime - Time.time;

        if (remainingTime <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Clamp01(remainingTime / dashCooldown);
    }

    public bool IsDashReady()
    {
        return Time.time >= nextDashTime && !isDashing;
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}
