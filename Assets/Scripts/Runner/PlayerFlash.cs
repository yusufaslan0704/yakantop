using UnityEngine;

// Runner/Saver: E ile flashbang firlatir. Patlayinca sadece aticilar kor olur.
public class PlayerFlash : MonoBehaviour
{
    [Header("Throw")]
    public float throwSpeed = 18f;
    public float throwUpSpeed = 3.5f;
    public float fuseTime = 0.55f;
    public float cooldown = 9f;

    [Header("Blind")]
    public float blindDuration = 2.4f;

    [Header("Feedback")]
    public float shakeDuration = 0.12f;
    public float shakeStrength = 0.18f;

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !projectileInFlight;
    public bool IsFlashing => Time.unscaledTime < blindUntil;

    public event System.Action OnFlashStarted;

    static float blindUntil;

    float nextReadyTime;
    bool projectileInFlight;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;
    PlayerMovement playerMovement;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (!CanUse())
        {
            return;
        }

        if (inputHandler != null && inputHandler.enabled && inputHandler.FlashPressed && IsReady)
        {
            ThrowFlash();
        }
    }

    bool CanUse()
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

    void ThrowFlash()
    {
        nextReadyTime = Time.unscaledTime + cooldown;
        projectileInFlight = true;
        OnFlashStarted?.Invoke();

        Vector3 origin = transform.position + Vector3.up * 1.15f + transform.forward * 0.4f;
        Vector3 direction = GetThrowDirection();

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Flashbang";
        projectile.transform.position = origin;
        projectile.transform.localScale = Vector3.one * 0.28f;
        SceneFolders.ParentTo(projectile.transform, SceneFolders.RuntimeSpawned);

        Collider col = projectile.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = direction * throwSpeed + Vector3.up * throwUpSpeed;

        // Atan kisiye carpmasin.
        IgnoreOwnerColliders(col);

        Renderer rend = projectile.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color c = new Color(0.95f, 0.98f, 1f, 1f);
            mat.SetColor("_BaseColor", c);
            mat.SetColor("_Color", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * 2.5f);
            }
            rend.material = mat;
        }

        FlashProjectile flash = projectile.AddComponent<FlashProjectile>();
        flash.fuseTime = fuseTime;
        flash.blindDuration = blindDuration;
        flash.shakeDuration = shakeDuration;
        flash.shakeStrength = shakeStrength;
        flash.owner = this;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }
    }

    Vector3 GetThrowDirection()
    {
        Vector3 dir = playerMovement != null && playerMovement.CurrentMoveDirection.sqrMagnitude > 0.01f
            ? playerMovement.CurrentMoveDirection
            : transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
        }

        return dir.normalized;
    }

    void IgnoreOwnerColliders(Collider projectileCollider)
    {
        if (projectileCollider == null) return;

        Collider[] ownerColliders = GetComponentsInChildren<Collider>();
        foreach (Collider ownerCol in ownerColliders)
        {
            if (ownerCol != null)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCol, true);
            }
        }
    }

    public void OnProjectileFinished()
    {
        projectileInFlight = false;
    }

    public static void ApplyBlind(float duration)
    {
        // Atıcı kamerasi hazir olsun; Tab ile bakinca overlay aninda gorunsun.
        if (SplitScreenManager.Instance != null)
        {
            SplitScreenManager.Instance.EnsureThrowerCameraReady();
        }

        float until = Time.unscaledTime + Mathf.Max(0.2f, duration);
        // Ust uste flash gelirse sureyi kisaltma.
        if (until > blindUntil)
        {
            blindUntil = until;
        }

        FlashBlindOverlay.Show(Mathf.Max(0.2f, blindUntil - Time.unscaledTime));
    }

    public float GetCooldownPercent()
    {
        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;

        return 1f - Mathf.Clamp01(remaining / cooldown);
    }

    public static bool AreThrowersBlinded()
    {
        return Time.unscaledTime < blindUntil;
    }

    public static float GetBlindRemaining()
    {
        return Mathf.Max(0f, blindUntil - Time.unscaledTime);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        blindUntil = 0f;
    }
}

// Firlatilan flashbang: fuse sonunda patlar, aticilari kor eder.
public class FlashProjectile : MonoBehaviour
{
    public float fuseTime = 0.55f;
    public float blindDuration = 2.4f;
    public float shakeDuration = 0.12f;
    public float shakeStrength = 0.18f;
    public PlayerFlash owner;

    float spawnTime;
    bool detonated;

    void Awake()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (detonated) return;

        if (Time.time - spawnTime >= fuseTime)
        {
            Detonate();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (detonated) return;

        // Yere / duvara carpinca erken patla (owner haric).
        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && owner != null && health.transform == owner.transform)
        {
            return;
        }

        Detonate();
    }

    void Detonate()
    {
        if (detonated) return;
        detonated = true;

        Vector3 pos = transform.position;
        SpawnStarburst(pos);
        PlayerFlash.ApplyBlind(blindDuration);
        CameraShake.ShakeAll(shakeDuration, shakeStrength);

        if (owner != null)
        {
            owner.OnProjectileFinished();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHit();
        }

        Destroy(gameObject);
    }

    // Decoy / dis sistemler: ayni VFX + korluk, projectile olmadan.
    public static void ExplodeAt(
        Vector3 position,
        float blindDuration = 2.4f,
        float shakeDuration = 0.12f,
        float shakeStrength = 0.18f)
    {
        SpawnStarburstStatic(position);
        PlayerFlash.ApplyBlind(blindDuration);
        CameraShake.ShakeAll(shakeDuration, shakeStrength);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHit();
        }
    }

    void SpawnStarburst(Vector3 position)
    {
        SpawnStarburstStatic(position);
    }

    static void SpawnStarburstStatic(Vector3 position)
    {
        SpawnGlowSphereStatic(position, 0.35f, new Color(1f, 1f, 1f, 1f), 0.25f, 4.5f);

        int rayCount = 16;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i + Random.Range(-4f, 4f);
            float length = Random.Range(2.2f, 4.2f);
            float thickness = Random.Range(0.04f, 0.09f);
            Color rayColor = Color.Lerp(
                new Color(0.55f, 0.85f, 1f, 0.95f),
                Color.white,
                Random.Range(0.2f, 0.8f));

            SpawnRayStatic(position, angle, length, thickness, rayColor);
        }

        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Range(0f, 360f);
            SpawnRayStatic(position, angle, Random.Range(1.0f, 2.0f), 0.03f,
                new Color(0.7f, 0.9f, 1f, 0.7f));
        }
    }

    static void SpawnGlowSphereStatic(Vector3 position, float startScale, Color color, float duration, float endScale)
    {
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "FlashCore";
        glow.transform.position = position;
        glow.transform.localScale = Vector3.one * startScale;
        Object.Destroy(glow.GetComponent<Collider>());
        SceneFolders.ParentTo(glow.transform, SceneFolders.RuntimeSpawned);

        Renderer rend = glow.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = CreateUnlit(color);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        FlashBurstExpand expand = glow.AddComponent<FlashBurstExpand>();
        expand.duration = duration;
        expand.endScale = endScale;
        expand.startScale = startScale;
    }

    static void SpawnRayStatic(Vector3 center, float angleDeg, float length, float thickness, Color color)
    {
        Quaternion rot = Quaternion.Euler(0f, angleDeg, 0f);
        Vector3 dir = rot * Vector3.forward;

        GameObject ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ray.name = "FlashRay";
        ray.transform.position = center + dir * (length * 0.5f);
        ray.transform.rotation = Quaternion.LookRotation(dir);
        ray.transform.localScale = new Vector3(thickness, thickness, length);
        Object.Destroy(ray.GetComponent<Collider>());
        SceneFolders.ParentTo(ray.transform, SceneFolders.RuntimeSpawned);

        Renderer rend = ray.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = CreateUnlit(color);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        FlashRayFade fade = ray.AddComponent<FlashRayFade>();
        fade.duration = 0.35f;
        fade.startColor = color;
    }

    static Material CreateUnlit(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.color = color;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 3f);
        }

        return mat;
    }
}

public class FlashBurstExpand : MonoBehaviour
{
    public float duration = 0.25f;
    public float startScale = 0.35f;
    public float endScale = 4.5f;

    float elapsed;
    Material mat;
    Color startColor;

    void Awake()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            startColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;

        if (mat != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t * t);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            mat.color = c;
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}

public class FlashRayFade : MonoBehaviour
{
    public float duration = 0.35f;
    public Color startColor = Color.white;

    float elapsed;
    Material mat;
    Vector3 startScale;

    void Awake()
    {
        startScale = transform.localScale;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.localScale = new Vector3(
            startScale.x * (1f - t * 0.5f),
            startScale.y * (1f - t * 0.5f),
            startScale.z * (1f + t * 0.35f));

        if (mat != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            mat.color = c;
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
