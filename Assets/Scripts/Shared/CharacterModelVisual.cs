using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Kapsul gorselinin yerine rigli Mixamo modelini koyar ve animasyonlari
// oyun durumuna baglar: dururken Idle, kosarken Running, havadayken Jump,
// emote atinca dans. Animator Controller yerine Playables mixer kullanir;
// klip gecisleri kodda yumusak agirlik gecisiyle yapilir.
//
// Fizik hala kapsul collider uzerinden yurur; bu script SADECE gorseldir.
public class CharacterModelVisual : MonoBehaviour
{
    [Header("Model")]
    [Tooltip("Resources altindaki model FBX yolu (uzantisiz).")]
    public string modelResourcePath = "Models/Idle";
    public float modelScale = 1f;

    [Tooltip("Runner/Saver icin otomatik ozel model kullan.")]
    public bool useRunnerDefaultModel = true;

    [Tooltip("Ozel model rig tasimiyorsa animasyonlu Mixamo modeline don.")]
    public bool fallbackToAnimatedIfStatic = true;

    const string RunnerModelPath = "Models/RunnerCharacter";
    const string SaverModelPath = "Models/SaverCharacter";
    const string ThrowerModelPath = "Models/ThrowerCharacter";

    [Tooltip("Model boyu kapsulden cok farkliysa (cok kucuk/buyuk FBX) otomatik olcekle.")]
    public bool autoFitStaticModel = true;

    [Tooltip("Kapsulun merkezi yerde degil; modelin ayaklari yere degsin diye asagi kaydirma.")]
    public float modelYOffset = -1f;

    [Header("Animation")]
    public float transitionSpeed = 8f;

    [Tooltip("Bu hizin ustunde kosma animasyonu oynar.")]
    public float runSpeedThreshold = 1f;

    public string idleClipPath = "Models/Idle";
    public string runClipPath = "Models/Running";
    public string jumpClipPath = "Models/Jump";

    [Header("Action Clips")]
    public string throwClipPath = "Models/Throw";
    public string dashClipPath = "Models/Dash";
    public string dodgeClipPath = "Models/Dodge";
    public string hitClipPath = "Models/Hit";
    public string reviveClipPath = "Models/Revive";

    [Tooltip("Zipla klibinin basindaki yere comelme (hazirlik) kismi atlanir (saniye). " +
             "Fizik ziplamasi aninda basladigi icin bu kisim gecikme gibi gorunuyor.")]
    public float jumpClipSkipStart = 0.35f;

    [Tooltip("Zipla animasyonu, hesaplanan havada kalma suresinin icine sigdirilir. " +
             "1'den buyuk deger animasyonu daha da hizlandirir.")]
    public float jumpSpeedMultiplier = 1.15f;

    [Tooltip("Throw klibinde topun elden ciktigi an (0-1). " +
             "Top spawn suresine gore klip hizi ayarlanir.")]
    [Range(0.15f, 0.9f)]
    public float throwReleaseNormalizedTime = 0.48f;

    [Tooltip("Charge windup'ta tutulacak poz (0-1). Release'ten once olmali.")]
    [Range(0.1f, 0.45f)]
    public float throwWindupHoldNormalizedTime = 0.3f;

    [Header("Dodge Visual")]
    [Tooltip("Dodge animasyonunun ekranda kalma suresi (sn). Timing dodge ile kisa tut.")]
    public float dodgeVisualDuration = 0.14f;

    [Tooltip("Klibin basindan atlanacak kisim (0-1). Hazirlik / yavas giris kesilir.")]
    [Range(0f, 0.7f)]
    public float dodgeClipSkipStart = 0.18f;

    [Tooltip("Klibin nerede kesilecegi (0-1). Sondaki el hareketinden once bitir.")]
    [Range(0.2f, 1f)]
    public float dodgeClipCutEnd = 0.55f;

    [Header("Dances (emote sirasina gore)")]
    public string[] danceClipPaths =
    {
        "Models/Chicken Dance",
        "Models/Ymca Dance",
        "Models/Slide Hip Hop Dance"
    };

    [Header("Dash Lean")]
    [Tooltip("Dash sirasinda modelin one egilme acisi (derece).")]
    public float dashLeanAngle = 22f;
    public float dashLeanSpeed = 18f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.15f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerEmote playerEmote;
    private PlayerDash playerDash;
    private PlayerDuck playerDuck;
    private PlayerMovement playerMovement;
    private PlayerInputHandler inputHandler;
    private PlayerRole playerRole;

    private Transform modelTransform;
    private Vector3 baseModelScale = Vector3.one;

    /// <summary>Aktif karakter modeli (yoksa null). Decoy bunu birebir kopyalar.</summary>
    public Transform ModelTransform => modelTransform;
    private float currentLean;
    private float duckVisualFactor = 1f;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;

    private readonly List<AnimationClipPlayable> playables = new List<AnimationClipPlayable>();
    private readonly List<AnimationClip> clips = new List<AnimationClip>();
    private readonly List<bool> loopingStates = new List<bool>();

    private int idleIndex = -1;
    private int runIndex = -1;
    private int jumpIndex = -1;
    private int throwIndex = -1;
    private int dashIndex = -1;
    private int dodgeIndex = -1;
    private int hitIndex = -1;
    private int reviveIndex = -1;
    private readonly List<int> danceIndices = new List<int>();

    private int currentIndex;
    private int activeDance = -1;
    private float danceEndTime;

    private int activeActionIndex = -1;
    private float actionEndTime;
    private bool chargeWindupActive;

    private PlayerThrow playerThrow;
    private PlayerDodge playerDodge;
    private PlayerAirLift airLift;

    private static Material fallbackMaterial;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        playerEmote = GetComponent<PlayerEmote>();
        playerDash = GetComponent<PlayerDash>();
        playerDuck = GetComponent<PlayerDuck>();
        playerThrow = GetComponent<PlayerThrow>();
        playerDodge = GetComponent<PlayerDodge>();
        playerMovement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        airLift = GetComponent<PlayerAirLift>();
    }

    public void ResetPose()
    {
        currentLean = 0f;
        duckVisualFactor = 1f;
        activeActionIndex = -1;
        activeDance = -1;

        if (modelTransform == null)
        {
            return;
        }

        modelTransform.localRotation = Quaternion.identity;
        modelTransform.localScale = baseModelScale;
        modelTransform.localPosition = new Vector3(0f, modelYOffset, 0f);
    }

    void OnEnable()
    {
        if (playerEmote != null)
        {
            playerEmote.OnEmotePlayed += HandleEmote;
        }

        if (playerHealth != null)
        {
            playerHealth.OnEliminated += HandleHit;
            playerHealth.OnRevived += HandleRevive;
        }

        if (playerThrow != null)
        {
            playerThrow.OnThrowStarted += HandleThrow;
            playerThrow.OnChargeStarted += HandleChargeStarted;
            playerThrow.OnChargeCancelled += HandleChargeCancelled;
        }

        if (playerDodge != null)
        {
            playerDodge.OnDodgeStarted += HandleDodge;
        }
    }

    void OnDisable()
    {
        if (playerEmote != null)
        {
            playerEmote.OnEmotePlayed -= HandleEmote;
        }

        if (playerHealth != null)
        {
            playerHealth.OnEliminated -= HandleHit;
            playerHealth.OnRevived -= HandleRevive;
        }

        if (playerThrow != null)
        {
            playerThrow.OnThrowStarted -= HandleThrow;
            playerThrow.OnChargeStarted -= HandleChargeStarted;
            playerThrow.OnChargeCancelled -= HandleChargeCancelled;
        }

        if (playerDodge != null)
        {
            playerDodge.OnDodgeStarted -= HandleDodge;
        }
    }

    void Start()
    {
        ApplyRoleModelDefaults();

        // Modeli eklemeden ONCE var olan tum gorselleri topla:
        // kapsul govdesi, yon gosterge kupu (DirectionMarker) vb.
        MeshRenderer[] oldRenderers = GetComponentsInChildren<MeshRenderer>();

        if (!TryInstantiateModel(out GameObject instance))
        {
            Debug.LogWarning(name + ": model bulunamadi (" + modelResourcePath + "). Kapsul gorseli kaliyor.");
            return;
        }

        instance.name = "CharacterModel";
        instance.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
        instance.transform.localRotation = Quaternion.identity;

        modelTransform = instance.transform;
        baseModelScale = Vector3.one * modelScale;
        modelTransform.localScale = baseModelScale;

        EnsureMaterials(instance);

        foreach (SkinnedMeshRenderer smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.updateWhenOffscreen = true;
        }

        Renderer[] modelRenderers = instance.GetComponentsInChildren<Renderer>();

        if (modelRenderers.Length == 0)
        {
            Debug.LogWarning(name + ": model yuklendi ama Renderer yok (" + modelResourcePath + ").");
            Destroy(instance);
            return;
        }

        HideOldVisuals(oldRenderers);

        Animator animator = instance.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        // Hareket fizik tarafindan yonetiliyor; animasyon karakteri kaydirmasin.
        animator.applyRootMotion = false;
        EnsureAnimatorAvatar(animator, instance);

        SkinnedMeshRenderer skinnedMesh = instance.GetComponentInChildren<SkinnedMeshRenderer>();

        bool isRoleModel =
            modelResourcePath == RunnerModelPath ||
            modelResourcePath == SaverModelPath ||
            modelResourcePath == ThrowerModelPath;

        if (skinnedMesh == null &&
            fallbackToAnimatedIfStatic &&
            !isRoleModel &&
            modelResourcePath != idleClipPath)
        {
            Debug.Log(name + ": model rig'siz (" + modelResourcePath + "). Animasyonlu fallback -> " + idleClipPath);
            Destroy(instance);
            modelResourcePath = idleClipPath;

            if (!TryInstantiateModel(out instance))
            {
                Debug.LogWarning(name + ": animasyonlu fallback yuklenemedi. Kapsul gorseli kaliyor.");
                return;
            }

            instance.name = "CharacterModel";
            instance.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
            instance.transform.localRotation = Quaternion.identity;
            modelTransform = instance.transform;
            baseModelScale = Vector3.one * modelScale;
            modelTransform.localScale = baseModelScale;

            EnsureMaterials(instance);

            animator = instance.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            skinnedMesh = instance.GetComponentInChildren<SkinnedMeshRenderer>();
        }
        else if (skinnedMesh == null && isRoleModel)
        {
            Debug.LogWarning(
                name + ": " + modelResourcePath +
                " skinned mesh yok; yine de ozel model tutuluyor (Idle fallback yok).");
        }

        if (autoFitStaticModel)
        {
            FitModelToCapsule(instance);
        }

        if (skinnedMesh != null)
        {
            BuildGraph(animator);
        }

        // Skinned mesh bind/avatar sorunlarinda cull edilmesin.
        foreach (SkinnedMeshRenderer smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.updateWhenOffscreen = true;
            smr.enabled = true;

            if (smr.sharedMesh != null)
            {
                Bounds local = smr.sharedMesh.bounds;
                if (local.size.y < 0.05f || local.size.y > 50f)
                {
                    smr.localBounds = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 2f, 1.2f));
                }
            }
        }

        CharacterAnimator proceduralAnimator = GetComponent<CharacterAnimator>();

        if (proceduralAnimator != null && graph.IsValid())
        {
            proceduralAnimator.EnterModelMode();
        }

        if (playerHealth != null)
        {
            playerHealth.RefreshRenderers();
        }

        int rendererCount = instance.GetComponentsInChildren<Renderer>().Length;
        string matName = "?";
        Renderer firstRenderer = instance.GetComponentInChildren<Renderer>();
        if (firstRenderer != null && firstRenderer.sharedMaterial != null)
        {
            matName = firstRenderer.sharedMaterial.name;
        }

        Debug.Log(
            name + ": karakter modeli yuklendi -> " + modelResourcePath +
            " scale=" + (modelTransform != null ? modelTransform.localScale.y.ToString("F2") : "?") +
            " boundY=" + GetModelBoundHeight(instance).ToString("F2") +
            " renderers=" + rendererCount +
            " mat=" + matName);
    }

    static float GetModelBoundHeight(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return 0f;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.size.y;
    }

    bool TryInstantiateModel(out GameObject instance)
    {
        instance = null;

        GameObject modelPrefab = Resources.Load<GameObject>(modelResourcePath);

        if (modelPrefab != null)
        {
            instance = Instantiate(modelPrefab, transform);
            return true;
        }

        Mesh mesh = null;

        foreach (Object asset in Resources.LoadAll(modelResourcePath))
        {
            if (asset is GameObject go)
            {
                instance = Instantiate(go, transform);
                return true;
            }

            if (asset is Mesh meshAsset)
            {
                mesh = meshAsset;
            }
        }

        if (mesh == null)
        {
            return false;
        }

        instance = new GameObject("CharacterModel");
        instance.transform.SetParent(transform, false);

        MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        instance.AddComponent<MeshRenderer>();

        return true;
    }

    void EnsureMaterials(GameObject instance)
    {
        if (fallbackMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            fallbackMaterial = new Material(shader)
            {
                color = new Color(0.72f, 0.74f, 0.78f, 1f),
                name = "RoleCharacterFallbackLit"
            };
        }

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            Material mat = renderer.sharedMaterial;

            // Bu FBX'lerde Material yok; Unity default/pembe/seffaf birakabiliyor.
            // Role modellerinde her zaman opak Lit kullan.
            bool forceRoleFallback =
                modelResourcePath == RunnerModelPath ||
                modelResourcePath == SaverModelPath ||
                modelResourcePath == ThrowerModelPath;

            bool needsFallback =
                forceRoleFallback ||
                mat == null ||
                mat.shader == null ||
                mat.shader.name.Contains("Error") ||
                mat.name == "Default-Material" ||
                mat.name.StartsWith("Default", System.StringComparison.Ordinal) ||
                mat.name.Contains("No Name");

            if (!needsFallback && mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                if (c.a < 0.05f)
                {
                    needsFallback = true;
                }
            }

            if (!needsFallback)
            {
                continue;
            }

            renderer.sharedMaterial = fallbackMaterial;
            renderer.enabled = true;
        }
    }

    static void EnsureAnimatorAvatar(Animator animator, GameObject instance)
    {
        if (animator == null)
        {
            return;
        }

        if (animator.avatar != null && animator.avatar.isValid)
        {
            return;
        }

        // Instantiate edilen FBX'te Avatar bazen Animator'a baglanmaz.
        // Parent hiyerarsisinde Avatar asset referansi yok; bu durumda
        // Playables Humanoid retarget yapamaz ve mesh bind-pose'da kalir.
        Debug.LogWarning(
            (instance != null ? instance.name : "model") +
            ": Animator avatar eksik/gecersiz. Mesh bind-pose'da kalabilir.");
    }

    void ApplyRoleModelDefaults()
    {
        if (!useRunnerDefaultModel)
        {
            return;
        }

        PlayerRole role = GetComponent<PlayerRole>();

        if (role == null)
        {
            return;
        }

        if (role.roleType == RoleType.Runner)
        {
            ApplyRoleModelPath(RunnerModelPath);
        }
        else if (role.roleType == RoleType.Saver)
        {
            ApplyRoleModelPath(SaverModelPath);
        }
        else if (role.roleType == RoleType.Thrower)
        {
            ApplyRoleModelPath(ThrowerModelPath);
            // Atıcı daha yavas gezer; kosu animasyonu erken ve net baslasin.
            runClipPath = "Models/Running";
            runSpeedThreshold = 0.35f;
        }
    }

    bool IsThrowerRole()
    {
        return playerRole != null && playerRole.roleType == RoleType.Thrower;
    }

    void ApplyRoleModelPath(string preferredPath)
    {
        // Role modelleri (Runner/Saver/Thrower) her zaman tercih edilir.
        // Rig yoksa bile Idle'a dusme — ozel mesh gorunsun; animasyon sonra eklenir.
        bool isRoleModel =
            preferredPath == RunnerModelPath ||
            preferredPath == SaverModelPath ||
            preferredPath == ThrowerModelPath;

        if (!isRoleModel &&
            fallbackToAnimatedIfStatic &&
            !ModelPrefabHasRig(preferredPath))
        {
            modelResourcePath = idleClipPath;
        }
        else
        {
            modelResourcePath = preferredPath;
        }
    }

    static bool ModelPrefabHasRig(string resourcePath)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);

        if (prefab != null)
        {
            return prefab.GetComponentInChildren<SkinnedMeshRenderer>() != null;
        }

        foreach (Object asset in Resources.LoadAll(resourcePath))
        {
            if (asset is GameObject go && go.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                return true;
            }
        }

        return false;
    }

    void FitModelToCapsule(GameObject instance)
    {
        if (modelTransform == null)
        {
            return;
        }

        const float targetHeight = 1.85f;

        // Sadece skinned mesh'leri kullan; FBX'teki yardimci/dev mesh'ler
        // bound'u sisebilir ve modeli yanlis kucultur.
        SkinnedMeshRenderer[] skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
        float height = 0f;
        Bounds worldBounds = default;
        bool hasWorld = false;

        foreach (SkinnedMeshRenderer smr in skinned)
        {
            Bounds b = smr.bounds;
            if (b.size.y < 1e-6f)
            {
                continue;
            }

            // Asiri buyuk local mesh (yardimci geo) world'de de sisikse atla.
            if (smr.sharedMesh != null && smr.sharedMesh.bounds.size.y > 20f && b.size.y > 10f)
            {
                continue;
            }

            if (!hasWorld)
            {
                worldBounds = b;
                hasWorld = true;
            }
            else
            {
                worldBounds.Encapsulate(b);
            }
        }

        if (hasWorld)
        {
            height = worldBounds.size.y;
        }

        if (height < 0.05f)
        {
            Transform hips = FindChildRecursive(instance.transform, "mixamorig:Hips");
            if (hips != null)
            {
                float hipLocalY = Mathf.Abs(hips.localPosition.y);
                float hipWorldY = Mathf.Abs(hips.position.y - transform.position.y);
                float hipHeight = Mathf.Max(hipLocalY * Mathf.Abs(modelTransform.lossyScale.y), hipWorldY);
                height = Mathf.Max(hipHeight * 1.75f, 0.01f);
            }
        }

        if (height < 0.01f)
        {
            // Son care: tum renderer'larin en kucuk makul bound'u.
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                float y = renderer.bounds.size.y;
                if (y > 0.001f && y < 5f)
                {
                    height = Mathf.Max(height, y);
                    if (!hasWorld)
                    {
                        worldBounds = renderer.bounds;
                        hasWorld = true;
                    }
                }
            }
        }

        if (height < 0.01f)
        {
            return;
        }

        if (height >= 1.2f && height <= 2.4f)
        {
            if (hasWorld)
            {
                float bottomOffset = worldBounds.min.y - transform.position.y;
                modelTransform.localPosition = new Vector3(0f, modelYOffset - bottomOffset, 0f);
            }

            return;
        }

        float scaleFactor = targetHeight / height;
        modelTransform.localScale *= scaleFactor;
        baseModelScale = modelTransform.localScale;

        hasWorld = false;
        foreach (SkinnedMeshRenderer smr in skinned)
        {
            Bounds b = smr.bounds;
            if (b.size.y < 1e-6f) continue;
            if (smr.sharedMesh != null && smr.sharedMesh.bounds.size.y > 20f && b.size.y > 10f) continue;

            if (!hasWorld)
            {
                worldBounds = b;
                hasWorld = true;
            }
            else
            {
                worldBounds.Encapsulate(b);
            }
        }

        if (hasWorld)
        {
            float bottomOffset = worldBounds.min.y - transform.position.y;
            modelTransform.localPosition = new Vector3(0f, modelYOffset - bottomOffset, 0f);
        }
    }

    static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    void HideOldVisuals(MeshRenderer[] oldRenderers)
    {
        foreach (MeshRenderer renderer in oldRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    void BuildGraph(Animator animator)
    {
        graph = PlayableGraph.Create(name + "_Visual");

        idleIndex = AddClip(idleClipPath, looping: true);
        runIndex = AddClip(runClipPath, looping: true);
        jumpIndex = AddClip(jumpClipPath, looping: false);
        throwIndex = AddClip(throwClipPath, looping: false);
        dashIndex = AddClip(dashClipPath, looping: false);
        dodgeIndex = AddClip(dodgeClipPath, looping: false);

        // Dodge FBX yoksa Dash klibini ayri playable olarak kullan.
        if (dodgeIndex < 0 && !string.IsNullOrEmpty(dashClipPath))
        {
            dodgeIndex = AddClip(dashClipPath, looping: false);
        }

        hitIndex = AddClip(hitClipPath, looping: false);
        reviveIndex = AddClip(reviveClipPath, looping: false);

        foreach (string dancePath in danceClipPaths)
        {
            danceIndices.Add(AddClip(dancePath, looping: false));
        }

        if (idleIndex < 0)
        {
            Debug.LogWarning(name + ": Idle klibi yok, animasyon sistemi kapali.");
            graph.Destroy();
            return;
        }

        mixer = AnimationMixerPlayable.Create(graph, playables.Count);

        for (int i = 0; i < playables.Count; i++)
        {
            graph.Connect(playables[i], 0, mixer, i);
            mixer.SetInputWeight(i, i == idleIndex ? 1f : 0f);
        }

        currentIndex = idleIndex;

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Anim", animator);
        output.SetSourcePlayable(mixer);

        graph.Play();
    }

    // Klibi yukleyip playable listesine ekler; bulunamazsa -1 doner.
    // Eksik aksiyon klipleri (Dodge/Revive vb.) uyari basmadan sessizce atlanir.
    int AddClip(string resourcePath, bool looping)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return -1;
        }

        AnimationClip clip = LoadClip(resourcePath);

        if (clip == null)
        {
            bool optionalAction =
                resourcePath == dodgeClipPath ||
                resourcePath == reviveClipPath ||
                resourcePath == dashClipPath ||
                resourcePath == throwClipPath ||
                resourcePath == hitClipPath;

            if (!optionalAction)
            {
                Debug.LogWarning(name + ": animasyon klibi bulunamadi: " + resourcePath);
            }

            return -1;
        }

        AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);

        playables.Add(playable);
        clips.Add(clip);
        loopingStates.Add(looping);

        return playables.Count - 1;
    }

    AnimationClip LoadClip(string resourcePath)
    {
        foreach (Object asset in Resources.LoadAll(resourcePath))
        {
            AnimationClip clip = asset as AnimationClip;

            if (clip != null && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }

    void Update()
    {
        if (!graph.IsValid()) return;

        TickChargeWindup();
        int targetIndex = ChooseState();

        UpdateWeights(targetIndex);
        UpdateRunPlaybackSpeed(targetIndex);
        LoopClips();
        UpdateModelPose();
    }

    // Dash egilmesi ve egilme durusunu model uzerinde yonetir; kok transforma dokunmaz.
    void UpdateModelPose()
    {
        if (modelTransform == null) return;

        bool dashing = playerDash != null && playerDash.IsDashing();
        bool ducking = playerDuck != null && playerDuck.IsDucking;

        float targetLean = dashing ? dashLeanAngle : 0f;
        currentLean = Mathf.Lerp(currentLean, targetLean, dashLeanSpeed * Time.deltaTime);

        if (!dashing && currentLean < 0.5f)
        {
            currentLean = 0f;
        }

        float targetDuck = ducking ? playerDuck.duckHeightScale : 1f;
        duckVisualFactor = Mathf.Lerp(duckVisualFactor, targetDuck, dashLeanSpeed * Time.deltaTime);

        if (!ducking && Mathf.Abs(duckVisualFactor - 1f) < 0.01f)
        {
            duckVisualFactor = 1f;
        }

        modelTransform.localRotation = Quaternion.Euler(currentLean, 0f, 0f);

        float duckWidth = 1f + (1f - duckVisualFactor) * 0.2f;
        Vector3 dashScale = dashing
            ? new Vector3(0.97f, 0.95f, 1.05f)
            : Vector3.one;

        modelTransform.localScale = new Vector3(
            baseModelScale.x * duckWidth * dashScale.x,
            baseModelScale.y * duckVisualFactor * dashScale.y,
            baseModelScale.z * duckWidth * dashScale.z);

        float yPos = modelYOffset - (1f - duckVisualFactor) * 0.45f;
        modelTransform.localPosition = new Vector3(0f, yPos, 0f);
    }

    int ChooseState()
    {
        // Hit animasyonu eleme aninda one cikar; bitince idle'a doner.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            activeDance = -1;

            if (IsActionPlaying(hitIndex))
            {
                return hitIndex;
            }

            activeActionIndex = -1;
            return idleIndex;
        }

        Vector3 horizontalVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        horizontalVelocity.y = 0f;

        bool moving = IsLocomoting(horizontalVelocity);
        bool grounded = IsGrounded();
        bool dashing = playerDash != null && playerDash.IsDashing();
        bool dodging = playerDodge != null && playerDodge.IsDodgeActive;

        // Tek seferlik aksiyon (throw/hit/revive/dodge) bitene kadar oncelikli.
        // Charge windup da throw aksiyonunu tutar.
        if (chargeWindupActive && throwIndex >= 0)
        {
            return throwIndex;
        }

        if (activeActionIndex >= 0)
        {
            if (Time.time >= actionEndTime)
            {
                activeActionIndex = -1;
            }
            else
            {
                return activeActionIndex;
            }
        }

        if (dashing && dashIndex >= 0)
        {
            return dashIndex;
        }

        if (dodging && dodgeIndex >= 0)
        {
            return dodgeIndex;
        }

        // Dans: hareket etmek, ziplamak veya surenin dolmasi iptal eder.
        if (activeDance >= 0)
        {
            if (Time.time >= danceEndTime || moving || !grounded || dashing)
            {
                activeDance = -1;
            }
            else
            {
                return danceIndices[activeDance];
            }
        }

        // Atıcı ziplamaz; kucuk yer temasi bosluklari kosuyu calmamali.
        // Yuksek atis modunda da jump pozu kullanma.
        bool useJumpPose = !grounded && rb != null && !rb.isKinematic && jumpIndex >= 0;
        if (useJumpPose && IsThrowerRole())
        {
            bool airLifted = airLift != null && airLift.IsElevated;
            useJumpPose = !airLifted && rb.linearVelocity.y > 0.8f;
        }

        if (useJumpPose)
        {
            return jumpIndex;
        }

        if (moving && runIndex >= 0)
        {
            return runIndex;
        }

        return idleIndex;
    }

    bool IsLocomoting(Vector3 horizontalVelocity)
    {
        if (horizontalVelocity.magnitude > runSpeedThreshold)
        {
            return true;
        }

        // Girdi basilir basmaz kosuya gec (hiz birikmesini bekleme).
        if (playerMovement != null &&
            playerMovement.enabled &&
            playerMovement.CurrentMoveDirection.sqrMagnitude > 0.01f)
        {
            return true;
        }

        if (inputHandler != null &&
            inputHandler.enabled &&
            !inputHandler.suppressMoveInput &&
            inputHandler.MoveInput.sqrMagnitude > 0.04f)
        {
            return true;
        }

        return false;
    }

    void UpdateRunPlaybackSpeed(int targetIndex)
    {
        if (runIndex < 0 || targetIndex != runIndex || !graph.IsValid())
        {
            return;
        }

        float speed = 1f;

        if (rb != null)
        {
            Vector3 horizontal = rb.linearVelocity;
            horizontal.y = 0f;
            // Runner ~8.5, Thrower ~5.5 — kosu klibini hiza oranla.
            speed = Mathf.Clamp(horizontal.magnitude / 7.5f, 0.55f, 1.25f);
        }

        playables[runIndex].SetSpeed(speed);
    }

    bool IsActionPlaying(int index)
    {
        return index >= 0 && activeActionIndex == index && Time.time < actionEndTime;
    }

    void PlayAction(int index)
    {
        if (index < 0 || !graph.IsValid())
        {
            return;
        }

        activeDance = -1;
        activeActionIndex = index;
        actionEndTime = Time.time + clips[index].length;
        playables[index].SetTime(0);
        playables[index].SetSpeed(1);

        // Aksiyon aninda yumusak blend beklemeden hemen goster.
        SnapMixerTo(index);
    }

    void SnapMixerTo(int index)
    {
        if (!mixer.IsValid())
        {
            return;
        }

        for (int i = 0; i < playables.Count; i++)
        {
            mixer.SetInputWeight(i, i == index ? 1f : 0f);
        }

        currentIndex = index;
    }

    void HandleThrow()
    {
        if (throwIndex < 0)
        {
            return;
        }

        if (chargeWindupActive)
        {
            chargeWindupActive = false;
            PlayAction(throwIndex);
            SyncThrowClipFromWindup();
            return;
        }

        PlayAction(throwIndex);
        SyncThrowClip();
    }

    void HandleChargeStarted()
    {
        if (throwIndex < 0 || !graph.IsValid())
        {
            return;
        }

        chargeWindupActive = true;
        activeDance = -1;
        PlayAction(throwIndex);
        actionEndTime = Time.time + 120f;

        float clipLength = clips[throwIndex].length;
        float holdTime = clipLength * Mathf.Clamp(throwWindupHoldNormalizedTime, 0.1f, 0.45f);
        float approach = Mathf.Max(0.12f, holdTime / 0.28f);
        approach = Mathf.Clamp(approach, 0.6f, 3.5f);
        playables[throwIndex].SetTime(0);
        playables[throwIndex].SetSpeed(approach);
    }

    void HandleChargeCancelled()
    {
        if (!chargeWindupActive && activeActionIndex != throwIndex)
        {
            return;
        }

        chargeWindupActive = false;
        if (activeActionIndex == throwIndex)
        {
            activeActionIndex = -1;
        }

        if (throwIndex >= 0 && graph.IsValid())
        {
            playables[throwIndex].SetSpeed(1f);
        }
    }

    void TickChargeWindup()
    {
        if (!chargeWindupActive || throwIndex < 0 || !graph.IsValid())
        {
            return;
        }

        float clipLength = clips[throwIndex].length;
        if (clipLength <= 0.01f)
        {
            return;
        }

        float holdTime = clipLength * Mathf.Clamp(throwWindupHoldNormalizedTime, 0.1f, 0.45f);
        double t = playables[throwIndex].GetTime();
        if (t >= holdTime)
        {
            playables[throwIndex].SetTime(holdTime);
            playables[throwIndex].SetSpeed(0f);
        }
    }

    void HandleDodge()
    {
        if (dodgeIndex < 0)
        {
            return;
        }

        chargeWindupActive = false;
        PlayAction(dodgeIndex);
        SyncDodgeClip();
    }

    // Dodge: baslangici atla, sondaki el hareketinden once kes,
    // kalan ortayi kisa surede hizli oynat.
    void SyncDodgeClip()
    {
        if (dodgeIndex < 0 || !graph.IsValid())
        {
            return;
        }

        float clipLength = clips[dodgeIndex].length;
        if (clipLength <= 0.01f)
        {
            return;
        }

        float startNorm = Mathf.Clamp(dodgeClipSkipStart, 0f, 0.7f);
        float endNorm = Mathf.Clamp(dodgeClipCutEnd, startNorm + 0.1f, 1f);

        float startTime = clipLength * startNorm;
        float endTime = clipLength * endNorm;
        float playLength = Mathf.Max(0.05f, endTime - startTime);

        // Parry animasyonu kisa flash; eski uzun dodge penceresine bagli degil.
        float visualDuration = Mathf.Clamp(dodgeVisualDuration, 0.08f, 0.2f);

        float speed = playLength / Mathf.Max(0.08f, visualDuration);
        speed = Mathf.Clamp(speed, 2.5f, 12f);

        playables[dodgeIndex].SetTime(startTime);
        playables[dodgeIndex].SetSpeed(speed);
        actionEndTime = Time.time + visualDuration;
    }

    // Topun cikacagi ana kadar throw klibini hizlandirir:
    // release frame == ballReleaseDelay.
    void SyncThrowClip()
    {
        if (throwIndex < 0 || !graph.IsValid())
        {
            return;
        }

        float clipLength = clips[throwIndex].length;
        if (clipLength <= 0.01f)
        {
            return;
        }

        float releaseNorm = Mathf.Clamp(throwReleaseNormalizedTime, 0.15f, 0.9f);
        float releaseClipTime = clipLength * releaseNorm;

        float delay = 0.4f;
        if (playerThrow != null)
        {
            delay = Mathf.Max(0.08f, playerThrow.ballReleaseDelay);
        }

        float speed = releaseClipTime / delay;
        speed = Mathf.Clamp(speed, 0.75f, 4.5f);

        playables[throwIndex].SetTime(0);
        playables[throwIndex].SetSpeed(speed);
        actionEndTime = Time.time + (clipLength / speed);
    }

    // Windup pozundan release frame'e; top cikisi hala ballReleaseDelay.
    void SyncThrowClipFromWindup()
    {
        if (throwIndex < 0 || !graph.IsValid())
        {
            return;
        }

        float clipLength = clips[throwIndex].length;
        if (clipLength <= 0.01f)
        {
            return;
        }

        float releaseNorm = Mathf.Clamp(throwReleaseNormalizedTime, 0.15f, 0.9f);
        float releaseClipTime = clipLength * releaseNorm;
        float current = (float)playables[throwIndex].GetTime();
        current = Mathf.Clamp(current, 0f, releaseClipTime);

        float remaining = Mathf.Max(0.04f, releaseClipTime - current);
        float delay = 0.4f;
        if (playerThrow != null)
        {
            delay = Mathf.Max(0.08f, playerThrow.ballReleaseDelay);
        }

        float speed = remaining / delay;
        speed = Mathf.Clamp(speed, 0.6f, 5f);

        playables[throwIndex].SetTime(current);
        playables[throwIndex].SetSpeed(speed);
        float remainingClip = Mathf.Max(0.05f, clipLength - current);
        actionEndTime = Time.time + (remainingClip / speed);
    }

    void HandleHit()
    {
        chargeWindupActive = false;
        PlayAction(hitIndex);
    }

    void HandleRevive()
    {
        PlayAction(reviveIndex);
    }

    void HandleEmote(int emoteIndex)
    {
        if (danceIndices.Count == 0) return;

        // Emote sayisi dans klibinden fazla olabilir; dongusel esle.
        int danceSlot = ((emoteIndex % danceIndices.Count) + danceIndices.Count) % danceIndices.Count;
        if (danceIndices[danceSlot] < 0) return;

        activeActionIndex = -1;
        activeDance = danceSlot;
        danceEndTime = Time.time + clips[danceIndices[danceSlot]].length;
    }

    void UpdateWeights(int targetIndex)
    {
        if (targetIndex != currentIndex)
        {
            // Throw/Dodge Sync* ile ayarlandigi icin burada sifirlanmaz.
            if (targetIndex >= 0 && !loopingStates[targetIndex])
            {
                if (targetIndex == jumpIndex)
                {
                    SyncJumpClip();
                }
                else if (targetIndex != throwIndex && targetIndex != dodgeIndex)
                {
                    playables[targetIndex].SetTime(0);
                    playables[targetIndex].SetSpeed(1);
                }
            }

            currentIndex = targetIndex;
        }

        for (int i = 0; i < playables.Count; i++)
        {
            float current = mixer.GetInputWeight(i);
            float target = i == currentIndex ? 1f : 0f;

            mixer.SetInputWeight(i, Mathf.MoveTowards(current, target, transitionSpeed * Time.deltaTime));
        }
    }

    // Zipla klibini gercek havada kalma suresine sigdirir:
    // hazirlik kismi atlanir, kalan kisim tahmini ucus suresinde bitecek hizda oynar.
    void SyncJumpClip()
    {
        float skip = Mathf.Clamp(jumpClipSkipStart, 0f, clips[jumpIndex].length * 0.8f);
        float remainingClip = clips[jumpIndex].length - skip;

        // Yukari cikis + asagi inis suresi: t = 2v / g.
        float upVelocity = rb != null ? Mathf.Max(0f, rb.linearVelocity.y) : 0f;
        float airTime = Mathf.Max(0.35f, 2f * upVelocity / -Physics.gravity.y);

        float speed = (remainingClip / airTime) * Mathf.Max(0.1f, jumpSpeedMultiplier);

        playables[jumpIndex].SetTime(skip);
        playables[jumpIndex].SetSpeed(speed);
    }

    void LoopClips()
    {
        for (int i = 0; i < playables.Count; i++)
        {
            if (!loopingStates[i]) continue;

            // Import ayarinda "Loop Time" aciksa klip kendi kendine dikissiz
            // donguleniyor; elle sarmaya gerek yok.
            if (clips[i].isLooping) continue;

            double time = playables[i].GetTime();
            double length = clips[i].length;

            // Basa "sarmak" yerine fazi koruyarak dongule:
            // taskan sure yeni turun basina eklenir, takilma olmaz.
            if (time >= length && length > 0)
            {
                playables[i].SetTime(time % length);
            }
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance * Mathf.Max(0.3f, transform.localScale.y)
        );
    }

    void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
