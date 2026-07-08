using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashDistance = 4f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 2f;

    [Tooltip("Dash boyunca hiz profili. Basta patlayici, sonda yumusak biter. " +
             "Egrinin toplam alani otomatik normalize edilir; mesafe hep dashDistance olur.")]
    public AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(0.6f, 0.85f, -1.2f, -1.2f),
        new Keyframe(1f, 0.25f, -0.8f, 0f)
    );

    [Header("Dash Trail")]
    public bool useTrail = true;
    public Color trailColor = new Color(0.55f, 0.85f, 1f, 0.85f);
    public float trailWidth = 0.55f;
    public float trailFadeTime = 0.28f;
    public Vector3 trailOffset = new Vector3(0f, 0.4f, 0f);

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
    private float curveAverage = 1f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;
    private PlayerMovement playerMovement;
    private TrailRenderer trail;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerMovement = GetComponent<PlayerMovement>();

        // Egrinin ortalama degerini bul: hiz bununla bolununce
        // egri ne olursa olsun toplam mesafe dashDistance kalir.
        curveAverage = ComputeCurveAverage(speedCurve);

        if (useTrail)
        {
            CreateTrail();
        }
    }

    void Update()
    {
        // Round bittiyse dash atılamaz.
        if (!GameManager.RoundIsActive)
        {
            StopDash();
            return;
        }

        // Elenen oyuncu dash atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            StopDash();
            return;
        }

        // Dash girisi: handler varsa oradan (klavye Shift / gamepad RB), yoksa eski usul.
        bool dashInput = inputHandler != null
            ? inputHandler.DashPressed
            : Input.GetKeyDown(KeyCode.LeftShift);

        if (dashInput && Time.time >= nextDashTime && !isDashing)
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

        // Dash'in neresindeyiz? (0 = basi, 1 = sonu)
        float progress = 1f - Mathf.Clamp01(dashTimer / dashDuration);

        // Egriden anlik hiz: baslangicta patlayici, sonda yumusak.
        float baseSpeed = dashDistance / dashDuration;
        float dashSpeed = baseSpeed * (speedCurve.Evaluate(progress) / curveAverage);

        Vector3 velocity = dashDirection * dashSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;

        // Karakter dash yonune aninda donsun (kayarken yana bakmasin).
        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            Quaternion.LookRotation(dashDirection),
            25f * Time.fixedDeltaTime
        ));

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

        // Oyuncunun o an bastigi yone dash at; girdi yoksa baktigi yone.
        Vector3 inputDirection = playerMovement != null
            ? playerMovement.CurrentMoveDirection
            : Vector3.zero;

        dashDirection = inputDirection.sqrMagnitude > 0.01f
            ? inputDirection
            : transform.forward;

        dashDirection.y = 0f;
        dashDirection.Normalize();

        nextDashTime = Time.time + dashCooldown;
        nextVfxTime = Time.time;

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        PlayDashFeedback();
    }

    void StopDash()
    {
        if (trail != null && isDashing)
        {
            trail.emitting = false;
        }

        if (!isDashing)
        {
            return;
        }

        isDashing = false;

        // Hizi sifirlamiyoruz: egri sonda zaten yavaslatti, kalan hiz
        // bir sonraki FixedUpdate'te PlayerMovement tarafindan devralinir.
        // Boylece dash'ten kosuya gecis akici olur.
    }

    void OnDisable()
    {
        // Kontrol başka karaktere geçerse dash yarıda kesilsin.
        StopDash();

        // Kontrol degisiminde karakter kayarak gitmesin.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
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

    // Kodla olusturulan iz: sahnede prefab/materyal ayarlamaya gerek yok.
    void CreateTrail()
    {
        GameObject trailObject = new GameObject("DashTrail");

        trailObject.transform.SetParent(transform, false);
        trailObject.transform.localPosition = trailOffset;

        trail = trailObject.AddComponent<TrailRenderer>();

        trail.time = trailFadeTime;
        trail.minVertexDistance = 0.08f;
        trail.emitting = false;
        trail.receiveShadows = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Vertex rengini destekleyen basit shader; hem built-in hem URP'de calisir.
        trail.material = new Material(Shader.Find("Sprites/Default"));

        // Genislik: govdede kalin, ucta sivri.
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, trailWidth),
            new Keyframe(1f, 0.02f)
        );

        // Renk: bastan yari saydam, sona dogru tamamen kaybolur.
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(trailColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(trailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        trail.colorGradient = gradient;
    }

    static float ComputeCurveAverage(AnimationCurve curve)
    {
        const int samples = 24;
        float sum = 0f;

        for (int i = 0; i < samples; i++)
        {
            sum += Mathf.Max(0.01f, curve.Evaluate(i / (float)(samples - 1)));
        }

        return sum / samples;
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
