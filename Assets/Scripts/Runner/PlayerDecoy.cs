using System.Collections.Generic;
using UnityEngine;

// Runner: X ile bulundugu yerde sahte kopya birakir.
// Kopya sahibin hareketini (kosu/dash/zipla/egil) birebir aynalar.
// Gorsel olarak gercek Runner gibi durur — atıcı / bot normal oyuncu sanmali.
// Top carparsa decoy patlar, Runner elenmez.
public class PlayerDecoy : MonoBehaviour
{
    [Header("Decoy")]
    public float duration = 4.5f;
    public float cooldown = 11f;
    public float spawnForwardOffset = 0.15f;

    [Tooltip("Decoy kosma hizi (Runner'a yakin).")]
    public float decoyMoveSpeed = 7.2f;

    [Tooltip("Spawn sonrasi kendi basina dolasma; sonra oyuncuyu taklit eder.")]
    public float freeWanderDuration = 2f;

    [Header("On Hit Flash")]
    [Tooltip("Decoy top yediginde aticilari kor etme suresi.")]
    public float flashBlindDuration = 2.4f;
    public float flashShakeDuration = 0.14f;
    public float flashShakeStrength = 0.2f;

    [Header("Look")]
    public string modelResourcePath = "Models/RunnerCharacter";
    public float modelScale = 250f;
    public float modelYOffset = -1f;

    [Tooltip("Kosarken hafif dikey bob.")]
    public float idleBobAmount = 0.045f;
    public float idleBobSpeed = 9f;

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !HasActiveDecoy;
    public bool HasActiveDecoy => activeDecoy != null;
    public bool IsDecoyActive => HasActiveDecoy;

    public event System.Action OnDecoySpawned;
    public event System.Action OnDecoyEnded;

    static readonly List<DecoyClone> ActiveDecoys = new List<DecoyClone>();

    float nextReadyTime;
    float decoyUntil;
    DecoyClone activeDecoy;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;
    CharacterModelVisual modelVisual;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        modelVisual = GetComponent<CharacterModelVisual>();
    }

    void Update()
    {
        if (!CanUse())
        {
            return;
        }

        if (inputHandler != null && inputHandler.enabled && inputHandler.DecoyPressed && IsReady)
        {
            Activate();
        }
    }

    bool CanUse()
    {
        if (!isActiveAndEnabled) return false;
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;

        if (playerRole != null && playerRole.roleType != RoleType.Runner)
        {
            return false;
        }

        return true;
    }

    void Activate()
    {
        nextReadyTime = Time.unscaledTime + cooldown;

        if (activeDecoy != null)
        {
            activeDecoy.Pop(activeDecoy.transform.position + Vector3.up, Vector3.up, Color.white, playHitFx: false);
            activeDecoy = null;
        }

        decoyUntil = Time.unscaledTime + duration;
        activeDecoy = SpawnDecoy();
        OnDecoySpawned?.Invoke();
        CameraShake.ShakeAll(0.05f, 0.07f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }
    }

    DecoyClone SpawnDecoy()
    {
        Vector3 pos = transform.position + transform.forward * spawnForwardOffset;
        Quaternion rot = transform.rotation;

        GameObject root = new GameObject("RunnerDecoy");
        root.transform.SetPositionAndRotation(pos, rot);
        SceneFolders.ParentTo(root.transform, SceneFolders.RuntimeSpawned);

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.48f;
        col.center = Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false; // Y hizi sahipten kopyalanir (ziplama dahil).
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;

        // Sahibiyle itisme — runner'i itmesin / ic ice girmesin.
        IgnoreOwnerCollisions(col);

        // Decoy oyuncu degil: can / rol / revive yok. Vurulunca sadece flash.
        DecoyClone decoy = root.AddComponent<DecoyClone>();
        decoy.Init(this, duration, decoyMoveSpeed, ResolveWanderZone(), freeWanderDuration);
        decoy.flashBlindDuration = flashBlindDuration;
        decoy.flashShakeDuration = flashShakeDuration;
        decoy.flashShakeStrength = flashShakeStrength;
        BuildVisual(root.transform, decoy);
        return decoy;
    }

    void IgnoreOwnerCollisions(Collider decoyCol)
    {
        if (decoyCol == null) return;

        Collider[] ownerCols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < ownerCols.Length; i++)
        {
            if (ownerCols[i] != null)
            {
                Physics.IgnoreCollision(decoyCol, ownerCols[i], true);
            }
        }
    }

    Collider ResolveWanderZone()
    {
        RoleZoneLimiter limiter = GetComponent<RoleZoneLimiter>();
        if (limiter != null && limiter.runnerZone != null)
        {
            return limiter.runnerZone;
        }

        RunnerBot bot = GetComponent<RunnerBot>();
        if (bot != null && bot.wanderZone != null)
        {
            return bot.wanderZone;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null || player.roleType != RoleType.Runner) continue;
            RoleZoneLimiter other = player.GetComponent<RoleZoneLimiter>();
            if (other != null && other.runnerZone != null)
            {
                return other.runnerZone;
            }
        }

        return null;
    }

    void BuildVisual(Transform root, DecoyClone decoy)
    {
        GameObject visual = null;

        // 1) Canli Runner modelini birebir kopyala.
        Transform liveModel = modelVisual != null ? modelVisual.ModelTransform : null;
        if (liveModel != null)
        {
            visual = Instantiate(liveModel.gameObject, root);
            visual.name = "DecoyModel";
            visual.transform.localPosition = liveModel.localPosition;
            visual.transform.localRotation = liveModel.localRotation;
            visual.transform.localScale = liveModel.localScale;
            StripNonVisualComponents(visual);
            CopyLiveMaterials(liveModel.gameObject, visual);
        }
        else
        {
            // 2) Fallback: Resources model.
            string path = modelResourcePath;
            if (modelVisual != null && !string.IsNullOrEmpty(modelVisual.modelResourcePath))
            {
                path = modelVisual.modelResourcePath;
            }

            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                visual = Instantiate(prefab, root);
                visual.name = "DecoyModel";
                visual.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * modelScale;
                StripNonVisualComponents(visual);
            }
        }

        if (visual == null)
        {
            SpawnCapsuleFallback(root, decoy);
            return;
        }

        if (!TryAttachAnimator(visual, decoy))
        {
            // Ozel model anim alamadiysa Mixamo Idle rig'ine gec.
            DestroyImmediate(visual);
            visual = SpawnAnimatedIdleVisual(root);
            if (visual == null || !TryAttachAnimator(visual, decoy))
            {
                if (visual != null)
                {
                    AddIdleBob(visual.transform);
                }
                else
                {
                    SpawnCapsuleFallback(root, decoy);
                }
            }
        }
    }

    GameObject SpawnAnimatedIdleVisual(Transform root)
    {
        GameObject idlePrefab = Resources.Load<GameObject>("Models/Idle");
        if (idlePrefab == null) return null;

        GameObject visual = Instantiate(idlePrefab, root);
        visual.name = "DecoyModel_Animated";
        visual.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        StripNonVisualComponents(visual);
        FitIdleModelHeight(visual, targetHeight: 1.85f);
        return visual;
    }

    bool TryAttachAnimator(GameObject visual, DecoyClone decoy)
    {
        if (visual == null || decoy == null) return false;

        DecoyVisualAnimator anim = visual.GetComponent<DecoyVisualAnimator>();
        if (anim == null)
        {
            anim = visual.AddComponent<DecoyVisualAnimator>();
        }

        if (modelVisual != null)
        {
            if (!string.IsNullOrEmpty(modelVisual.idleClipPath))
            {
                anim.idleClipPath = modelVisual.idleClipPath;
            }

            if (!string.IsNullOrEmpty(modelVisual.runClipPath))
            {
                anim.runClipPath = modelVisual.runClipPath;
            }
        }

        if (anim.TrySetup(decoy, visual))
        {
            return true;
        }

        Destroy(anim);
        return false;
    }

    static void FitIdleModelHeight(GameObject visual, float targetHeight)
    {
        Renderer rend = visual.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Bounds b = rend.bounds;
        float height = b.size.y;
        if (height < 0.05f) return;

        visual.transform.localScale *= targetHeight / height;

        // Ayaklar decoy root altinda (kapsul merkezi y, ayak y-1).
        Bounds after = rend.bounds;
        Transform parent = visual.transform.parent;
        float desiredBottom = parent.position.y - 1f;
        Vector3 local = visual.transform.localPosition;
        local.y += desiredBottom - after.min.y;
        visual.transform.localPosition = local;
    }

    void SpawnCapsuleFallback(Transform root, DecoyClone decoy)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "DecoyCapsule";
        capsule.transform.SetParent(root, false);
        capsule.transform.localPosition = Vector3.zero;

        Collider builtIn = capsule.GetComponent<Collider>();
        if (builtIn != null) Destroy(builtIn);

        Renderer rend = capsule.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color c = new Color(0.85f, 0.35f, 0.25f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            mat.color = c;
            rend.sharedMaterial = mat;

            if (decoy != null)
            {
                decoy.BindOwnedMaterials(new[] { mat });
            }
        }

        AddIdleBob(capsule.transform);
    }

    void CopyLiveMaterials(GameObject sourceRoot, GameObject decoyRoot)
    {
        Renderer[] srcRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
        Renderer[] dstRenderers = decoyRoot.GetComponentsInChildren<Renderer>(true);
        int count = Mathf.Min(srcRenderers.Length, dstRenderers.Length);

        for (int i = 0; i < count; i++)
        {
            if (srcRenderers[i] == null || dstRenderers[i] == null) continue;
            dstRenderers[i].sharedMaterials = srcRenderers[i].sharedMaterials;
        }
    }

    static void StripNonVisualComponents(GameObject visual)
    {
        // Animator KALIR — DecoyVisualAnimator playable baglar.
        foreach (Collider c in visual.GetComponentsInChildren<Collider>(true))
        {
            Destroy(c);
        }

        foreach (Rigidbody r in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            Destroy(r);
        }
    }

    void AddIdleBob(Transform target)
    {
        if (target == null || idleBobAmount <= 0f) return;

        DecoyIdleBob bob = target.gameObject.AddComponent<DecoyIdleBob>();
        bob.amount = idleBobAmount;
        bob.speed = idleBobSpeed;
    }

    public void NotifyDecoyDestroyed(DecoyClone decoy)
    {
        if (activeDecoy == decoy)
        {
            activeDecoy = null;
            decoyUntil = 0f;
            OnDecoyEnded?.Invoke();
        }
    }

    public float GetCooldownPercent()
    {
        if (HasActiveDecoy)
        {
            return Mathf.Clamp01((decoyUntil - Time.unscaledTime) / Mathf.Max(0.01f, duration));
        }

        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;

        return 1f - Mathf.Clamp01(remaining / cooldown);
    }

    public static void Register(DecoyClone decoy)
    {
        if (decoy != null && !ActiveDecoys.Contains(decoy))
        {
            ActiveDecoys.Add(decoy);
        }
    }

    public static void Unregister(DecoyClone decoy)
    {
        ActiveDecoys.Remove(decoy);
    }

    public static bool HasAnyDecoy()
    {
        CleanupNulls();
        return ActiveDecoys.Count > 0;
    }

    public static Transform GetClosestDecoy(Vector3 from)
    {
        CleanupNulls();
        if (ActiveDecoys.Count == 0) return null;

        DecoyClone best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < ActiveDecoys.Count; i++)
        {
            DecoyClone d = ActiveDecoys[i];
            if (d == null) continue;

            float sqr = (d.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = d;
            }
        }

        return best != null ? best.transform : null;
    }

    public static Transform GetPreferredDecoyTarget(Vector3 from, float chance)
    {
        if (Random.value > Mathf.Clamp01(chance)) return null;
        return GetClosestDecoy(from);
    }

    static void CleanupNulls()
    {
        for (int i = ActiveDecoys.Count - 1; i >= 0; i--)
        {
            if (ActiveDecoys[i] == null)
            {
                ActiveDecoys.RemoveAt(i);
            }
        }
    }

    void OnDisable()
    {
        if (activeDecoy != null)
        {
            activeDecoy.Pop(activeDecoy.transform.position + Vector3.up, Vector3.up, Color.white, playHitFx: false);
            activeDecoy = null;
        }
    }
}

// Ayakta duran oyuncu hissi icin cok hafif dikey bob.
public class DecoyIdleBob : MonoBehaviour
{
    public float amount = 0.035f;
    public float speed = 2.4f;

    Vector3 baseLocalPos;
    float phase;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float y = Mathf.Sin((Time.time + phase) * speed) * amount;
        transform.localPosition = baseLocalPos + Vector3.up * y;
    }
}
