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

    [Tooltip("Kapsulun merkezi yerde degil; modelin ayaklari yere degsin diye asagi kaydirma.")]
    public float modelYOffset = -1f;

    [Header("Animation")]
    public float transitionSpeed = 8f;

    [Tooltip("Bu hizin ustunde kosma animasyonu oynar.")]
    public float runSpeedThreshold = 1f;

    public string idleClipPath = "Models/Idle";
    public string runClipPath = "Models/Running";
    public string jumpClipPath = "Models/Jump";

    [Tooltip("Zipla klibinin basindaki yere comelme (hazirlik) kismi atlanir (saniye). " +
             "Fizik ziplamasi aninda basladigi icin bu kisim gecikme gibi gorunuyor.")]
    public float jumpClipSkipStart = 0.35f;

    [Tooltip("Zipla animasyonu, hesaplanan havada kalma suresinin icine sigdirilir. " +
             "1'den buyuk deger animasyonu daha da hizlandirir.")]
    public float jumpSpeedMultiplier = 1.15f;

    [Header("Dances (emote sirasina gore)")]
    public string[] danceClipPaths =
    {
        "Models/Chicken Dance",
        "Models/Ymca Dance",
        "Models/Slide Hip Hop Dance"
    };

    [Header("Dash Lean")]
    [Tooltip("Dash sirasinda modelin one egilme acisi (derece).")]
    public float dashLeanAngle = 16f;
    public float dashLeanSpeed = 14f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.15f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerEmote playerEmote;
    private PlayerDash playerDash;

    private Transform modelTransform;
    private float currentLean;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;

    private readonly List<AnimationClipPlayable> playables = new List<AnimationClipPlayable>();
    private readonly List<AnimationClip> clips = new List<AnimationClip>();
    private readonly List<bool> loopingStates = new List<bool>();

    private int idleIndex = -1;
    private int runIndex = -1;
    private int jumpIndex = -1;
    private readonly List<int> danceIndices = new List<int>();

    private int currentIndex;
    private int activeDance = -1;
    private float danceEndTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        playerEmote = GetComponent<PlayerEmote>();
        playerDash = GetComponent<PlayerDash>();
    }

    void OnEnable()
    {
        if (playerEmote != null)
        {
            playerEmote.OnEmotePlayed += HandleEmote;
        }
    }

    void OnDisable()
    {
        if (playerEmote != null)
        {
            playerEmote.OnEmotePlayed -= HandleEmote;
        }
    }

    void Start()
    {
        GameObject modelPrefab = Resources.Load<GameObject>(modelResourcePath);

        if (modelPrefab == null)
        {
            Debug.LogWarning(name + ": model bulunamadi (" + modelResourcePath + "). Kapsul gorseli kaliyor.");
            return;
        }

        // Modeli eklemeden ONCE var olan tum gorselleri topla:
        // kapsul govdesi, yon gosterge kupu (DirectionMarker) vb.
        MeshRenderer[] oldRenderers = GetComponentsInChildren<MeshRenderer>();

        GameObject instance = Instantiate(modelPrefab, transform);

        instance.name = "CharacterModel";
        instance.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = modelPrefab.transform.localScale * modelScale;

        modelTransform = instance.transform;

        Animator animator = instance.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        // Hareket fizik tarafindan yonetiliyor; animasyon karakteri kaydirmasin.
        animator.applyRootMotion = false;

        HideOldVisuals(oldRenderers);
        BuildGraph(animator);

        // Kapsul gozleri, bob ve squash artik gereksiz; gercek animasyonlar var.
        CharacterAnimator proceduralAnimator = GetComponent<CharacterAnimator>();

        if (proceduralAnimator != null)
        {
            proceduralAnimator.EnterModelMode();
        }

        // Elenme kirmizisi yeni modelin renderer'larina da uygulanabilsin.
        if (playerHealth != null)
        {
            playerHealth.RefreshRenderers();
        }
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
    int AddClip(string resourcePath, bool looping)
    {
        AnimationClip clip = LoadClip(resourcePath);

        if (clip == null)
        {
            Debug.LogWarning(name + ": animasyon klibi bulunamadi: " + resourcePath);
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
        UpdateDashLean();
    }

    // Dash sirasinda model one egilir: hiz hissi verir, kayma "bilinçli" gorunur.
    void UpdateDashLean()
    {
        if (modelTransform == null) return;

        float targetLean = playerDash != null && playerDash.IsDashing() ? dashLeanAngle : 0f;

        currentLean = Mathf.Lerp(currentLean, targetLean, dashLeanSpeed * Time.deltaTime);
        modelTransform.localRotation = Quaternion.Euler(currentLean, 0f, 0f);
    }

    int ChooseState()
    {
        // Elenmis karakter kapsul fiziğiyle savruluyor; animasyon idle'da kalir.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            activeDance = -1;
            return idleIndex;
        }

        Vector3 horizontalVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        horizontalVelocity.y = 0f;

        bool moving = horizontalVelocity.magnitude > runSpeedThreshold;
        bool grounded = IsGrounded();

        // Dans: hareket etmek, ziplamak veya surenin dolmasi iptal eder.
        if (activeDance >= 0)
        {
            if (Time.time >= danceEndTime || moving || !grounded)
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

    void UpdateWeights(int targetIndex)
    {
        if (targetIndex != currentIndex)
        {
            // Tek seferlik klipler (zipla/dans) hep bastan oynasin.
            if (targetIndex >= 0 && !loopingStates[targetIndex])
            {
                if (targetIndex == jumpIndex)
                {
                    SyncJumpClip();
                }
                else
                {
                    playables[targetIndex].SetTime(0);
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

    void HandleEmote(int emoteIndex)
    {
        if (emoteIndex < 0 || emoteIndex >= danceIndices.Count) return;

        if (danceIndices[emoteIndex] < 0) return;

        activeDance = emoteIndex;
        danceEndTime = Time.time + clips[danceIndices[emoteIndex]].length;
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
