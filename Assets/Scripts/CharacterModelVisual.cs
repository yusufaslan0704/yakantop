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

    [Tooltip("Runner rolu icin otomatik ozel model kullan.")]
    public bool useRunnerDefaultModel = true;

    [Tooltip("Ozel model rig tasimiyorsa animasyonlu Mixamo modeline don.")]
    public bool fallbackToAnimatedIfStatic = true;

    const string RunnerModelPath = "Models/RunnerCharacter";

    [Tooltip("Statik (rig'siz) modelleri kapsul boyutuna otomatik sigdir.")]
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

    [Header("Dodge Visual")]
    [Tooltip("Dodge animasyonunun ekranda kalma suresi (sn). Kisa tut: tepki hissi icin.")]
    public float dodgeVisualDuration = 0.26f;

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

    private Transform modelTransform;
    private Vector3 baseModelScale = Vector3.one;
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

    private PlayerThrow playerThrow;
    private PlayerDodge playerDodge;

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

        SkinnedMeshRenderer skinnedMesh = instance.GetComponentInChildren<SkinnedMeshRenderer>();

        if (skinnedMesh == null && fallbackToAnimatedIfStatic && modelResourcePath != idleClipPath)
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

        if (skinnedMesh == null)
        {
            if (autoFitStaticModel)
            {
                FitStaticModelToCapsule(instance);
            }
        }
        else
        {
            BuildGraph(animator);
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

        Debug.Log(name + ": karakter modeli yuklendi -> " + modelResourcePath);
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
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial != null)
            {
                continue;
            }

            if (fallbackMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                fallbackMaterial = new Material(shader)
                {
                    color = new Color(0.75f, 0.75f, 0.78f, 1f)
                };
            }

            renderer.sharedMaterial = fallbackMaterial;
        }
    }

    void ApplyRoleModelDefaults()
    {
        if (!useRunnerDefaultModel)
        {
            return;
        }

        PlayerRole role = GetComponent<PlayerRole>();

        if (role != null && role.roleType == RoleType.Runner)
        {
            if (fallbackToAnimatedIfStatic && !ModelPrefabHasRig(RunnerModelPath))
            {
                modelResourcePath = idleClipPath;
            }
            else
            {
                modelResourcePath = RunnerModelPath;
            }
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

    void FitStaticModelToCapsule(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        const float targetHeight = 1.85f;
        float height = bounds.size.y;

        if (height < 0.01f)
        {
            return;
        }

        float scaleFactor = targetHeight / height;
        modelTransform.localScale *= scaleFactor;
        baseModelScale = modelTransform.localScale;

        bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float bottomOffset = bounds.min.y - transform.position.y;
        modelTransform.localPosition = new Vector3(0f, modelYOffset - bottomOffset, 0f);
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

        int targetIndex = ChooseState();

        UpdateWeights(targetIndex);
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

        bool moving = horizontalVelocity.magnitude > runSpeedThreshold;
        bool grounded = IsGrounded();
        bool dashing = playerDash != null && playerDash.IsDashing();
        bool dodging = playerDodge != null && playerDodge.IsDodgeActive;

        // Tek seferlik aksiyon (throw/hit/revive/dodge) bitene kadar oncelikli.
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

        if (!grounded && rb != null && !rb.isKinematic && jumpIndex >= 0)
        {
            return jumpIndex;
        }

        if (moving && runIndex >= 0)
        {
            return runIndex;
        }

        return idleIndex;
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

        PlayAction(throwIndex);
        SyncThrowClip();
    }

    void HandleDodge()
    {
        if (dodgeIndex < 0)
        {
            return;
        }

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

        float visualDuration = dodgeVisualDuration;
        if (playerDodge != null)
        {
            visualDuration = Mathf.Clamp(
                Mathf.Max(dodgeVisualDuration, playerDodge.dodgeWindowDuration * 0.8f),
                0.16f,
                0.36f);
        }

        float speed = playLength / Mathf.Max(0.08f, visualDuration);
        speed = Mathf.Clamp(speed, 1.8f, 10f);

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

    void HandleHit()
    {
        PlayAction(hitIndex);
    }

    void HandleRevive()
    {
        PlayAction(reviveIndex);
    }

    void HandleEmote(int emoteIndex)
    {
        if (emoteIndex < 0 || emoteIndex >= danceIndices.Count) return;

        if (danceIndices[emoteIndex] < 0) return;

        activeActionIndex = -1;
        activeDance = emoteIndex;
        danceEndTime = Time.time + clips[danceIndices[emoteIndex]].length;
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
