using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashDistance = 4f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 2f;

    [Header("Collision Check")]
    public float wallCheckHeight = 1f;
    public float wallStopDistance = 0.8f;

    [Header("Dash VFX")]
    public GameObject dashVfxPrefab;
    public float vfxSpawnInterval = 0.04f;
    public Vector3 vfxOffset = new Vector3(0f, 0.7f, 0f);

    [Header("Dash Camera Shake")]
    public float dashShakeDuration = 0.08f;
    public float dashShakeStrength = 0.08f;

    private bool isDashing = false;
    private float nextDashTime = 0f;

    private PlayerHealth playerHealth;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // Elenen oyuncu dash atamaz.
        if (playerHealth != null && playerHealth.isEliminated)
        {
            return;
        }

        // Sol Shift ile dash.
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime && !isDashing)
        {
            PlayDashFeedback();

            StartCoroutine(Dash());

            nextDashTime = Time.time + dashCooldown;
        }
    }

    private System.Collections.IEnumerator Dash()
    {
        isDashing = true;

        Vector3 startPosition = transform.position;
        Vector3 dashDirection = transform.forward;

        float finalDashDistance = dashDistance;

        // Dash yönünde duvar var mı kontrol ediyoruz.
        Vector3 rayOrigin = transform.position + Vector3.up * wallCheckHeight;

        if (Physics.Raycast(rayOrigin, dashDirection, out RaycastHit hit, dashDistance))
        {
            finalDashDistance = Mathf.Max(0f, hit.distance - wallStopDistance);
        }

        Vector3 targetPosition = startPosition + dashDirection * finalDashDistance;

        float elapsed = 0f;
        float nextVfxTime = 0f;

        SpawnDashVFX();

        while (elapsed < dashDuration)
        {
            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                elapsed / dashDuration
            );

            if (elapsed >= nextVfxTime)
            {
                SpawnDashVFX();
                nextVfxTime = elapsed + vfxSpawnInterval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        SpawnDashVFX();

        isDashing = false;
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