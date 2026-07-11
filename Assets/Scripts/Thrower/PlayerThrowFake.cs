using UnityEngine;

// Atıcı 9. özellik: sahte atış animasyonu — top çıkmaz, kaçan dodge yakar.
// Tus: Q / gamepad Y (Triangle).
[DefaultExecutionOrder(42)]
public class PlayerThrowFake : MonoBehaviour
{
    [Header("Fake")]
    public float cooldown = 7.5f;
    public float animLockDuration = 0.42f;
    public float shakeDuration = 0.05f;
    public float shakeStrength = 0.07f;

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !IsFaking;
    public bool IsFaking { get; private set; }

    float nextReadyTime;
    float fakeEndTime;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;
    PlayerThrow playerThrow;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        playerThrow = GetComponent<PlayerThrow>();
    }

    void Update()
    {
        if (IsFaking && Time.unscaledTime >= fakeEndTime)
        {
            IsFaking = false;
        }

        if (!CanUse())
        {
            return;
        }

        if (WasFakePressed() && IsReady)
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
        if (playerThrow == null || !playerThrow.FakeModeSelected) return false;
        if (playerThrow.IsThrowBusy) return false;
        if (EmoteWheelUI.IsOpen) return false;
        return true;
    }

    bool WasFakePressed()
    {
        if (inputHandler == null || !inputHandler.enabled)
        {
            return false;
        }

        return inputHandler.FakePressed;
    }

    void Fire()
    {
        if (playerThrow == null) return;
        if (!playerThrow.TryBeginFakeThrow(animLockDuration))
        {
            return;
        }

        IsFaking = true;
        fakeEndTime = Time.unscaledTime + animLockDuration;
        nextReadyTime = Time.unscaledTime + cooldown;

        CameraShake.ShakeAll(shakeDuration, shakeStrength);

        if (AudioManager.Instance != null)
        {
            // Atis sesi olmadan whoosh hissi: dash/alt.
            AudioManager.Instance.PlayDash();
        }
    }

    public float GetCooldownPercent()
    {
        if (IsFaking) return 1f;
        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;
        return 1f - Mathf.Clamp01(remaining / cooldown);
    }
}
