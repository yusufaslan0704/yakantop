using UnityEngine;

// Runner/Saver yetenegi: bir kez topu emen kalkan balonu.
// Tus: G / gamepad right-stick click.
public class PlayerShield : MonoBehaviour
{
    [Header("Shield")]
    public float activeDuration = 4.5f;
    public float cooldown = 11f;

    [Tooltip("Yaricap: tum govdeyi sarmali (ayak-bas).")]
    public float bubbleRadius = 1.75f;

    [Tooltip("Dikey olcek: 1 = kure, >1 biraz daha uzun kalkan.")]
    public float bubbleHeightScale = 1.15f;

    public Color bubbleColor = new Color(0.35f, 0.85f, 1f, 0.08f);
    public Color bubbleRimColor = new Color(0.55f, 0.95f, 1f, 0.18f);

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !IsShieldActive;
    public bool IsShieldActive { get; private set; }

    public event System.Action OnShieldStarted;
    public event System.Action OnShieldBroken;

    float nextReadyTime;
    float activeUntil;
    GameObject bubbleRoot;
    GameObject bubbleShell;
    GameObject bubbleRim;
    Material shellMat;
    Material rimMat;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
    }

    void Update()
    {
        if (IsShieldActive && Time.unscaledTime >= activeUntil)
        {
            BreakShield(playFeedback: false);
        }

        UpdateBubbleVisual();

        if (!CanUse())
        {
            return;
        }

        if (inputHandler != null && inputHandler.enabled && inputHandler.ShieldPressed && IsReady)
        {
            Activate();
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

    void Activate()
    {
        IsShieldActive = true;
        activeUntil = Time.unscaledTime + activeDuration;
        nextReadyTime = Time.unscaledTime + cooldown;

        SpawnBubble();
        OnShieldStarted?.Invoke();
        CameraShake.ShakeAll(0.06f, 0.08f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }
    }

    // Ball carpma aninda: true = top emildi, oyuncu vurulmaz.
    public bool TryConsumeShield(Ball ball)
    {
        if (!IsShieldActive) return false;
        if (ball == null) return false;

        BreakShield(playFeedback: true);
        return true;
    }

    void BreakShield(bool playFeedback)
    {
        if (!IsShieldActive) return;

        IsShieldActive = false;
        activeUntil = 0f;
        DestroyBubble();

        if (playFeedback)
        {
            OnShieldBroken?.Invoke();
            CombatVfx.SpawnShieldBreak(transform.position, bubbleRimColor);
            CameraShake.ShakeAll(0.1f, 0.16f);
            CameraShake.PunchFovAll(3f, 0.12f);
        }
    }

    void SpawnBubble()
    {
        DestroyBubble();

        // Capsule root y=1, local merkez ~0 → ayak-bas arasi govdeyi sarar.
        bubbleRoot = new GameObject("ShieldBubble");
        bubbleRoot.transform.SetParent(transform, false);
        bubbleRoot.transform.localPosition = Vector3.zero;
        bubbleRoot.transform.localRotation = Quaternion.identity;

        bubbleShell = CreateShieldSphere("Shell", bubbleRoot.transform, 1f, bubbleColor, castShadows: false);
        shellMat = bubbleShell.GetComponent<Renderer>().material;

        // Dis halka / rim: biraz daha buyuk, daha parlak — kalkan kenari hissi.
        bubbleRim = CreateShieldSphere("Rim", bubbleRoot.transform, 1.06f, bubbleRimColor, castShadows: false);
        rimMat = bubbleRim.GetComponent<Renderer>().material;
    }

    GameObject CreateShieldSphere(string objectName, Transform parent, float scaleMul, Color color, bool castShadows)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = objectName;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = GetBubbleScale(scaleMul);
        Destroy(sphere.GetComponent<Collider>());

        Renderer rend = sphere.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            SetupTransparent(mat, color);
            rend.material = mat;
            rend.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        return sphere;
    }

    Vector3 GetBubbleScale(float mul)
    {
        float diameter = bubbleRadius * 2f * mul;
        return new Vector3(diameter, diameter * bubbleHeightScale, diameter);
    }

    void UpdateBubbleVisual()
    {
        if (!IsShieldActive || bubbleRoot == null) return;

        float pulse = 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 6f);
        float rimPulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 9f + 1.2f);

        if (bubbleShell != null)
        {
            bubbleShell.transform.localScale = GetBubbleScale(pulse);
        }

        if (bubbleRim != null)
        {
            bubbleRim.transform.localScale = GetBubbleScale(1.06f * rimPulse);
        }

        float life = Mathf.Clamp01((activeUntil - Time.unscaledTime) / Mathf.Max(0.01f, activeDuration));
        ApplyFade(shellMat, bubbleColor, life, 0.03f);
        ApplyFade(rimMat, bubbleRimColor, life, 0.06f);
    }

    static void ApplyFade(Material mat, Color baseColor, float life, float minAlpha)
    {
        if (mat == null) return;

        Color c = baseColor;
        c.a = Mathf.Lerp(minAlpha, baseColor.a, life);
        ApplyColor(mat, c);
    }

    void DestroyBubble()
    {
        if (bubbleRoot != null)
        {
            Destroy(bubbleRoot);
            bubbleRoot = null;
            bubbleShell = null;
            bubbleRim = null;
        }

        if (shellMat != null)
        {
            Destroy(shellMat);
            shellMat = null;
        }

        if (rimMat != null)
        {
            Destroy(rimMat);
            rimMat = null;
        }
    }

    static void SetupTransparent(Material mat, Color color)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.SetFloat("_Cull", 0f); // Off: iceriden de seffaf kabuk gorunsun
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ApplyColor(mat, color);

        // Cok hafif emission: karakteri kapatmadan kenar pariltisi.
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            Color emission = new Color(color.r, color.g, color.b, 1f);
            mat.SetColor("_EmissionColor", emission * 0.08f);
        }
    }

    static void ApplyColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.color = color;
    }

    public float GetCooldownPercent()
    {
        if (IsShieldActive)
        {
            return Mathf.Clamp01((activeUntil - Time.unscaledTime) / Mathf.Max(0.01f, activeDuration));
        }

        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;

        return 1f - Mathf.Clamp01(remaining / cooldown);
    }

    void OnDisable()
    {
        BreakShield(playFeedback: false);
    }

    void OnDestroy()
    {
        DestroyBubble();
    }
}
