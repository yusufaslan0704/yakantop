using UnityEngine;
using UnityEngine.Rendering;

// Prefab basina top gorsel kimligi: olcek (collider bagimsiz), trail, opsiyonel ekvator halkasi.
// Fizik / SphereCollider kokte kalir; mesh child'a tasinip scale edilir.
public class BallVisualIdentity : MonoBehaviour
{
    [Header("Silhouette")]
    [Tooltip("Hedef dunya silueti (1 = Normal). Root scale telafi edilir; collider etkilenmez.")]
    public float visualScale = 1f;

    [Tooltip("Aciksa BallData hizina (throwForce/mass) gore siluet ayarlanir.")]
    public bool scaleFromThrowSpeed = true;

    [Header("Custom Mesh (Meshy vb.)")]
    [Tooltip("Resources yolu, uzantisiz. Ornek: Models/Balls/BallNormal")]
    public string customMeshResourcePath = "";

    [Tooltip("Resources albedo yolu, uzantisiz. Ornek: Models/Balls/BallNormal_Albedo")]
    public string customAlbedoResourcePath = "";

    [Tooltip("Albedo'yu soft emission olarak da kullan (cyber/neon).")]
    public bool softEmissionFromAlbedo = true;

    [Range(0f, 2f)]
    public float emissionStrength = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("Custom textured Lit smoothness. Bouncy jelly ~0.78, Mirror ~0.9.")]
    public float materialSmoothness = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("Custom textured Lit metallic. Mirror ~0.85.")]
    public float materialMetallic = 0.08f;

    [Header("Trail")]
    public float trailTime = 0.25f;
    public float trailWidth = 0.2f;

    [Header("Curve accent")]
    public bool equatorRing;
    public Color ringColor = new Color(1f, 0.52f, 0.14f, 1f);
    public float ringScale = 1.12f;
    public float ringThickness = 0.035f;

    // Path speed → world silhouette. Normal (~28) = 1. Fast kucuk, Heavy buyuk.
    const float RefPathSpeed = 28f;
    const float MinWorldScale = 0.80f;
    const float MaxWorldScale = 1.38f;

    bool scaleApplied;
    bool ringCreated;
    Transform visualRoot;

    void Awake()
    {
        ApplyCustomMeshIfNeeded();
        ApplyTrail();
        // Olcek: SetData (hiz) gelirse orada; yoksa Start'ta prefab visualScale.
    }

    void Start()
    {
        if (!scaleApplied)
        {
            ApplyWorldSilhouette(visualScale);
        }

        if (equatorRing && !ringCreated)
        {
            CreateEquatorRing();
        }
    }

    // Ball.SetData sonrasi — hiza gore siluet.
    public void NotifyBallData(BallData data)
    {
        float worldScale = visualScale;
        if (scaleFromThrowSpeed && data != null)
        {
            worldScale = WorldScaleFromThrowSpeed(data);
        }

        ApplyWorldSilhouette(worldScale);
        if (equatorRing && !ringCreated)
        {
            CreateEquatorRing();
        }
    }

    public static float WorldScaleFromThrowSpeed(BallData data)
    {
        float pathSpeed = ThrowPhysics.PathSpeedFromForce(data.throwForce, data);
        return Mathf.Clamp(RefPathSpeed / Mathf.Max(0.01f, pathSpeed), MinWorldScale, MaxWorldScale);
    }

    void ApplyCustomMeshIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(customMeshResourcePath))
        {
            return;
        }

        Mesh mesh = LoadMeshFromResources(customMeshResourcePath);
        if (mesh == null)
        {
            Debug.LogWarning(name + ": custom ball mesh yok (" + customMeshResourcePath + ").");
            return;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mf == null || mr == null)
        {
            return;
        }

        mf.sharedMesh = mesh;

        if (!string.IsNullOrWhiteSpace(customAlbedoResourcePath))
        {
            Texture2D albedo = Resources.Load<Texture2D>(customAlbedoResourcePath);
            if (albedo != null)
            {
                mr.sharedMaterial = BuildTexturedMaterial(albedo);
            }
            else
            {
                Debug.LogWarning(name + ": custom ball albedo yok (" + customAlbedoResourcePath + ").");
            }
        }
    }

    Material BuildTexturedMaterial(Texture2D albedo)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { name = "BallCustomTexturedLit" };

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", albedo);
        }

        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", albedo);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        mat.color = Color.white;

        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", Mathf.Clamp01(materialMetallic));
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", Mathf.Clamp01(materialSmoothness));
        }

        if (softEmissionFromAlbedo && mat.HasProperty("_EmissionMap"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", albedo);
            float e = Mathf.Clamp01(emissionStrength);
            mat.SetColor("_EmissionColor", new Color(e, e, e, 1f));
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return mat;
    }

    static Mesh LoadMeshFromResources(string path)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab != null)
        {
            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                return mf.sharedMesh;
            }

            SkinnedMeshRenderer smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                return smr.sharedMesh;
            }
        }

        foreach (Object asset in Resources.LoadAll(path))
        {
            if (asset is Mesh mesh)
            {
                return mesh;
            }
        }

        return null;
    }

    void ApplyTrail()
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            return;
        }

        trail.time = Mathf.Max(0.05f, trailTime);
        trail.widthMultiplier = Mathf.Max(0.02f, trailWidth);
    }

    void ApplyWorldSilhouette(float worldScale)
    {
        scaleApplied = true;
        worldScale = Mathf.Clamp(worldScale, 0.55f, 1.6f);

        float root = Mathf.Max(
            0.01f,
            (Mathf.Abs(transform.localScale.x) +
             Mathf.Abs(transform.localScale.y) +
             Mathf.Abs(transform.localScale.z)) / 3f);
        float local = worldScale / root;

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mf == null || mr == null || mf.sharedMesh == null)
        {
            return;
        }

        if (visualRoot == null)
        {
            Transform existing = transform.Find("BallVisual");
            if (existing != null)
            {
                visualRoot = existing;
            }
        }

        if (visualRoot == null)
        {
            if (Mathf.Abs(local - 1f) < 0.01f)
            {
                return;
            }

            GameObject visual = new GameObject("BallVisual");
            visualRoot = visual.transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;

            MeshFilter childMf = visual.AddComponent<MeshFilter>();
            childMf.sharedMesh = mf.sharedMesh;

            MeshRenderer childMr = visual.AddComponent<MeshRenderer>();
            childMr.sharedMaterials = mr.sharedMaterials;
            childMr.shadowCastingMode = mr.shadowCastingMode;
            childMr.receiveShadows = mr.receiveShadows;

            mr.enabled = false;
        }

        visualRoot.localScale = Vector3.one * local;
    }

    void CreateEquatorRing()
    {
        ringCreated = true;
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "CurveEquatorRing";
        Object.Destroy(ring.GetComponent<Collider>());

        Transform parent = visualRoot != null ? visualRoot : transform.Find("BallVisual");
        ring.transform.SetParent(parent != null ? parent : transform, false);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(ringScale, ringThickness, ringScale);

        Renderer rend = ring.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color")
                            ?? Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", ringColor);
            }

            mat.color = ringColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", ringColor * 1.4f);
            }

            rend.sharedMaterial = mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }
    }
}
