using UnityEngine;

// Atıcı 10. özellik: atıcı bölgesinde blink + eski yerde gölge.
// Sonraki atışlarda gölge de aynı hedefe çapraz top atar.
[DefaultExecutionOrder(43)]
public class PlayerThrowerShadow : MonoBehaviour, IThrowerAbility
{
    [Header("Blink")]
    public float cooldown = 10f;
    public float shadowDuration = 5f;
    public float minBlinkDistance = 4.5f;
    public float ghostAlpha = 0.42f;
    [Range(0.4f, 1f)]
    public float echoForceScale = 0.88f;

    public bool IsReady => Time.unscaledTime >= nextReadyTime && activeShadow == null;
    public bool HasShadow => activeShadow != null && activeShadow.IsAlive;

    public ThrowerAbilityId AbilityId => ThrowerAbilityId.Shadow;
    public bool IsBusy => HasShadow;

    public float GetCooldownVisual() => GetCooldownPercent();

    float nextReadyTime;
    ThrowerShadowClone activeShadow;
    float lastEchoUnscaledTime = -999f;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;
    PlayerThrow playerThrow;
    RoleZoneLimiter zoneLimiter;
    Rigidbody rb;
    CharacterModelVisual modelVisual;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        playerThrow = GetComponent<PlayerThrow>();
        zoneLimiter = GetComponent<RoleZoneLimiter>();
        rb = GetComponent<Rigidbody>();
        modelVisual = GetComponent<CharacterModelVisual>();
    }

    void OnEnable()
    {
        if (playerThrow != null)
        {
            playerThrow.OnBallThrown += HandleBallThrown;
        }
    }

    void OnDisable()
    {
        if (playerThrow != null)
        {
            playerThrow.OnBallThrown -= HandleBallThrown;
        }

        ClearShadow();
    }

    void Update()
    {
        if (activeShadow != null && !activeShadow.IsAlive)
        {
            activeShadow = null;
        }

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
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (playerThrow == null || !playerThrow.ShadowModeSelected) return false;
        if (EmoteWheelUI.IsOpen) return false;
        return true;
    }

    void Activate()
    {
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        if (!TryPickBlinkPosition(oldPos, out Vector3 newPos))
        {
            return;
        }

        ClearShadow();
        activeShadow = SpawnShadow(oldPos, oldRot);

        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
            rb.position = newPos;
        }
        else
        {
            transform.position = newPos;
        }

        nextReadyTime = Time.unscaledTime + cooldown;
        CameraShake.ShakeAll(0.06f, 0.09f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }
    }

    bool TryPickBlinkPosition(Vector3 from, out Vector3 destination)
    {
        destination = from;
        Collider zone = zoneLimiter != null ? zoneLimiter.HomeThrowerZone : null;
        if (zone == null && zoneLimiter != null)
        {
            zone = zoneLimiter.throwerZoneA != null ? zoneLimiter.throwerZoneA : zoneLimiter.throwerZoneB;
        }

        if (zone == null)
        {
            return false;
        }

        Bounds bounds = zone.bounds;
        float pad = 0.85f;

        // Capraz: X'te aynala — iki noktadan çapraz atış açısı.
        float mirroredX = bounds.center.x - (from.x - bounds.center.x);
        float z = Mathf.Clamp(from.z + Random.Range(-1.2f, 1.2f), bounds.min.z + pad, bounds.max.z - pad);
        float x = Mathf.Clamp(mirroredX, bounds.min.x + pad, bounds.max.x - pad);

        destination = new Vector3(x, from.y, z);

        // Çok yakinsa rastgele uzak nokta dene.
        if ((destination - from).sqrMagnitude < minBlinkDistance * minBlinkDistance)
        {
            for (int i = 0; i < 8; i++)
            {
                float rx = Random.Range(bounds.min.x + pad, bounds.max.x - pad);
                float rz = Random.Range(bounds.min.z + pad, bounds.max.z - pad);
                Vector3 candidate = new Vector3(rx, from.y, rz);
                if ((candidate - from).sqrMagnitude >= minBlinkDistance * minBlinkDistance)
                {
                    destination = candidate;
                    break;
                }
            }
        }

        return (destination - from).sqrMagnitude >= 1f;
    }

    ThrowerShadowClone SpawnShadow(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("ThrowerShadow");
        root.transform.SetPositionAndRotation(position, rotation);
        SceneFolders.ParentTo(root.transform, SceneFolders.RuntimeSpawned);

        // Gerçek model varsa kopyala; yoksa basit kapsül.
        bool builtVisual = false;
        if (modelVisual != null && modelVisual.ModelTransform != null)
        {
            GameObject copy = Instantiate(modelVisual.ModelTransform.gameObject, root.transform);
            copy.name = "ShadowModel";
            copy.transform.localPosition = modelVisual.ModelTransform.localPosition;
            copy.transform.localRotation = modelVisual.ModelTransform.localRotation;
            copy.transform.localScale = modelVisual.ModelTransform.localScale;

            Animator anim = copy.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
            }

            builtVisual = true;
        }

        if (!builtVisual)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "ShadowCapsule";
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = Vector3.up;
            Collider col = capsule.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }

        ThrowerShadowClone shadow = root.AddComponent<ThrowerShadowClone>();
        shadow.Init(shadowDuration, ghostAlpha);
        return shadow;
    }

    void HandleBallThrown()
    {
        if (!HasShadow || playerThrow == null)
        {
            return;
        }

        if (playerThrow.IsSpawningEchoBall)
        {
            return;
        }

        // Volley gibi coklu spawn'da tek echo.
        if (Time.unscaledTime - lastEchoUnscaledTime < 0.15f)
        {
            return;
        }

        lastEchoUnscaledTime = Time.unscaledTime;

        BallData data = playerThrow.GetResolvedThrowBall();
        if (data == null || data.prefab == null)
        {
            return;
        }

        Vector3 aimPoint = playerThrow.GetLastAimPoint();
        Vector3 origin = activeShadow.GetThrowOrigin();
        Vector3 dir = aimPoint - origin;
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = activeShadow.transform.forward;
        }

        dir.Normalize();
        float force = playerThrow.GetLastThrowForce() * echoForceScale;
        playerThrow.SpawnEchoBall(origin, dir, force, data);
    }

    void ClearShadow()
    {
        if (activeShadow != null)
        {
            activeShadow.Despawn();
            activeShadow = null;
        }
    }

    public float GetCooldownPercent()
    {
        if (HasShadow) return 1f;
        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;
        return 1f - Mathf.Clamp01(remaining / cooldown);
    }
}
