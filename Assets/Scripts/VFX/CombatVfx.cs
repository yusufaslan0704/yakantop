using UnityEngine;

// Kisa omurlu combat / arena VFX: hit sparks, parry ring, shield shards, elim puff.
// Yeni binary asset gerektirmez; CreatePrimitive + runtime material kullanir.
public static class CombatVfx
{
    public static void SpawnImpact(Vector3 position, Vector3 normal, Color color, float scale = 1f)
    {
        SpawnBurstSphere(position, color, 0.18f * scale, 0.22f, 5.5f);
        SpawnSparks(position, normal, color, count: 7, speed: 4.5f * scale, life: 0.18f);
    }

    public static void SpawnPlayerHit(Vector3 position, Color color)
    {
        SpawnBurstSphere(position, color, 0.35f, 0.28f, 7f);
        SpawnSparks(position, Vector3.up, color, count: 10, speed: 5.5f, life: 0.22f);
    }

    public static void SpawnParryRing(Vector3 position)
    {
        Color cyan = new Color(0.45f, 0.95f, 1f, 0.85f);
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "ParryRing";
        ring.transform.position = position + Vector3.up * 0.9f;
        ring.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
        Object.Destroy(ring.GetComponent<Collider>());

        Renderer rend = ring.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = CreateUnlit(cyan);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        DashGhostFade fade = ring.AddComponent<DashGhostFade>();
        fade.duration = 0.16f;
        fade.startAlpha = 0.9f;

        VFXAutoDestroy grow = ring.AddComponent<VFXAutoDestroy>();
        grow.lifeTime = 0.16f;
        grow.growSpeed = 14f;

        SceneFolders.ParentTo(ring.transform, SceneFolders.RuntimeSpawned);
    }

    public static void SpawnShieldBreak(Vector3 position, Color color)
    {
        SpawnBurstSphere(position + Vector3.up * 0.9f, color, 0.55f, 0.2f, 8f);

        for (int i = 0; i < 10; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.6f + 0.2f;
            SpawnShard(position + Vector3.up * 0.9f, dir.normalized, color, 5.5f, 0.2f);
        }
    }

    public static void SpawnEliminationPuff(Vector3 position)
    {
        Color red = new Color(1f, 0.25f, 0.2f, 0.9f);
        SpawnBurstSphere(position + Vector3.up * 0.9f, red, 0.45f, 0.3f, 6.5f);
        SpawnSparks(position + Vector3.up * 0.9f, Vector3.up, red, count: 8, speed: 4f, life: 0.25f);
    }

    public static void TintBallTrail(GameObject ball, Color color)
    {
        if (ball == null)
        {
            return;
        }

        TrailRenderer trail = ball.GetComponentInChildren<TrailRenderer>();
        if (trail == null)
        {
            return;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(Color.Lerp(color, Color.white, 0.55f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
    }

    public static Color ReadBallColor(GameObject ball, Color fallback)
    {
        if (ball == null)
        {
            return fallback;
        }

        Renderer rend = ball.GetComponentInChildren<Renderer>();
        if (rend == null || rend.sharedMaterial == null)
        {
            return fallback;
        }

        Material mat = rend.sharedMaterial;
        if (mat.HasProperty("_BaseColor"))
        {
            return mat.GetColor("_BaseColor");
        }

        if (mat.HasProperty("_Color"))
        {
            return mat.color;
        }

        return fallback;
    }

    static void SpawnBurstSphere(Vector3 position, Color color, float startScale, float life, float growSpeed)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "CombatBurst";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * startScale;
        Object.Destroy(sphere.GetComponent<Collider>());

        Renderer rend = sphere.GetComponent<Renderer>();
        if (rend != null)
        {
            Color c = color;
            c.a = Mathf.Clamp01(color.a > 0.01f ? color.a : 0.85f);
            rend.sharedMaterial = CreateUnlit(c);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        DashGhostFade fade = sphere.AddComponent<DashGhostFade>();
        fade.duration = life;
        fade.startAlpha = 0.85f;

        VFXAutoDestroy grow = sphere.AddComponent<VFXAutoDestroy>();
        grow.lifeTime = life;
        grow.growSpeed = growSpeed;

        SceneFolders.ParentTo(sphere.transform, SceneFolders.RuntimeSpawned);
    }

    static void SpawnSparks(Vector3 position, Vector3 normal, Color color, int count, float speed, float life)
    {
        Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = (n + Random.insideUnitSphere * 0.85f).normalized;
            SpawnShard(position, dir, color, speed, life);
        }
    }

    static void SpawnShard(Vector3 position, Vector3 direction, Color color, float speed, float life)
    {
        GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shard.name = "CombatSpark";
        shard.transform.position = position;
        shard.transform.localScale = new Vector3(0.05f, 0.05f, 0.22f);
        shard.transform.rotation = Quaternion.LookRotation(direction);
        Object.Destroy(shard.GetComponent<Collider>());

        Renderer rend = shard.GetComponent<Renderer>();
        if (rend != null)
        {
            Color c = color;
            c.a = 0.9f;
            rend.sharedMaterial = CreateUnlit(c);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        Rigidbody rb = shard.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 2.5f;
        rb.angularDamping = 3f;
        rb.linearVelocity = direction * speed;
        rb.angularVelocity = Random.insideUnitSphere * 8f;

        DashGhostFade fade = shard.AddComponent<DashGhostFade>();
        fade.duration = life;
        fade.startAlpha = 0.95f;

        Object.Destroy(shard, life + 0.02f);
        SceneFolders.ParentTo(shard.transform, SceneFolders.RuntimeSpawned);
    }

    static Material CreateUnlit(Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

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
            mat.SetColor("_EmissionColor", color * 2.5f);
        }

        return mat;
    }
}
