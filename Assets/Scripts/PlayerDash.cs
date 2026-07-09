using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashDistance = 4.6f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.85f;

    [Tooltip("Dash boyunca hiz profili. Basta patlayici, sonda yumusak biter.")]
    public AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f, 1.15f, 0f, 0f),
        new Keyframe(0.45f, 0.9f, -1.4f, -1.4f),
        new Keyframe(1f, 0.2f, -0.9f, 0f)
    );

    [Header("Dash Visuals")]
    public bool useDashVisuals = true;

    [Tooltip("Dash boyunca arkaya birakilan ince ruzgar cizgileri (karakter kopyasi YOK).")]
    public float trailSpawnInterval = 0.03f;
    public Color windColor = new Color(0.88f, 0.96f, 1f, 0.5f);

    [Header("Dash Camera Feel")]
    public float dashShakeDuration = 0.1f;
    public float dashShakeStrength = 0.12f;
    public float dashFovPunch = 5.5f;
    public float dashFovPunchDuration = 0.16f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float nextDashTime = 0f;

    private Vector3 dashDirection;
    private float curveAverage = 1f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;
    private PlayerMovement playerMovement;
    private float nextTrailSpawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerMovement = GetComponent<PlayerMovement>();

        curveAverage = ComputeCurveAverage(speedCurve);
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            StopDash();
            return;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            StopDash();
            return;
        }

        bool dashInput = inputHandler != null
            ? inputHandler.DashPressed
            : Input.GetKeyDown(KeyCode.LeftShift);

        if (dashInput && Time.time >= nextDashTime && !isDashing)
        {
            StartDash();
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

        float progress = 1f - Mathf.Clamp01(dashTimer / dashDuration);
        float baseSpeed = dashDistance / dashDuration;
        float dashSpeed = baseSpeed * (speedCurve.Evaluate(progress) / curveAverage);

        Vector3 velocity = dashDirection * dashSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;

        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            Quaternion.LookRotation(dashDirection),
            25f * Time.fixedDeltaTime
        ));

        dashTimer -= Time.fixedDeltaTime;

        if (useDashVisuals && Time.time >= nextTrailSpawnTime)
        {
            SpawnTrailWisp();
            nextTrailSpawnTime = Time.time + trailSpawnInterval;
        }

        if (dashTimer <= 0f)
        {
            StopDash();
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        Vector3 inputDirection = playerMovement != null
            ? playerMovement.CurrentMoveDirection
            : Vector3.zero;

        dashDirection = inputDirection.sqrMagnitude > 0.01f
            ? inputDirection
            : transform.forward;

        dashDirection.y = 0f;
        dashDirection.Normalize();

        nextDashTime = Time.time + dashCooldown;

        if (useDashVisuals)
        {
            SpawnWindBurst();
        }

        nextTrailSpawnTime = Time.time;

        PlayDashFeedback();
    }

    void StopDash()
    {
        if (!isDashing)
        {
            return;
        }

        isDashing = false;
    }

    void OnDisable()
    {
        StopDash();

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
            rb.angularVelocity = Vector3.zero;
        }
    }

    void SpawnTrailWisp()
    {
        Vector3 right = Vector3.Cross(Vector3.up, dashDirection).normalized;
        float side = Random.Range(-0.35f, 0.35f);
        float back = Random.Range(0.55f, 0.95f);
        float length = Random.Range(0.9f, 1.6f);
        float height = Random.Range(0.55f, 1.05f);

        Vector3 origin = transform.position + Vector3.up * 0.35f;
        Vector3 streakPos = origin - dashDirection * back + right * side + Vector3.up * height * 0.1f;

        SpawnWindStreak(streakPos, dashDirection, length, 0.025f + Random.value * 0.015f, 0.12f);
    }

    void SpawnWindBurst()
    {
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        Vector3 right = Vector3.Cross(Vector3.up, dashDirection).normalized;

        SpawnRing(origin, dashDirection);

        // Sadece arkada ve yanlarda; karakterin uzerinde degil.
        for (int i = 0; i < 4; i++)
        {
            float side = Random.Range(-0.9f, 0.9f);
            float back = Random.Range(0.7f, 1.6f);
            float length = Random.Range(1.2f, 2.2f);

            Vector3 streakPos = origin - dashDirection * back + right * side;
            SpawnWindStreak(streakPos, dashDirection, length, 0.03f, 0.14f);
        }
    }

    void SpawnRing(Vector3 position, Vector3 forward)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "DashRing";
        ring.transform.position = position;
        ring.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
        ring.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        Destroy(ring.GetComponent<Collider>());

        Material mat = CreateFadeMaterial(windColor);
        ring.GetComponent<Renderer>().material = mat;

        DashRingExpand expand = ring.AddComponent<DashRingExpand>();
        expand.duration = 0.14f;
        expand.endScale = 2.4f;
    }

    void SpawnWindStreak(Vector3 position, Vector3 direction, float length, float thickness, float fadeDuration)
    {
        GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        streak.name = "DashWind";
        streak.transform.position = position + direction * (length * 0.5f);
        streak.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        streak.transform.localScale = new Vector3(thickness, thickness * 0.5f, length);
        Destroy(streak.GetComponent<Collider>());

        Material mat = CreateAdditiveMaterial(windColor);
        streak.GetComponent<Renderer>().material = mat;

        DashGhostFade fade = streak.AddComponent<DashGhostFade>();
        fade.duration = fadeDuration;
        fade.startAlpha = windColor.a;
    }

    Material CreateFadeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        Color c = color;
        c.a = color.a;

        SetupTransparent(mat, c);
        return mat;
    }

    Material CreateAdditiveMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            return CreateFadeMaterial(color);
        }

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        return mat;
    }

    static void SetupTransparent(Material mat, Color color)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
    }

    void PlayDashFeedback()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }

        CameraShake.ShakeAll(dashShakeDuration, dashShakeStrength);
        CameraShake.PunchFovAll(dashFovPunch, dashFovPunchDuration);
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

    public float GetDashCooldownRemaining()
    {
        return Mathf.Max(0f, nextDashTime - Time.time);
    }

    public float GetDashCooldownDuration()
    {
        return dashCooldown;
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
