using UnityEngine;

// Runner Decoy — oyuncu DEGILDIR (revive yok; top = flash).
// 1) Ilk N saniye kendi basina rastgele kosar (oyuncudan ayrilsin).
// 2) Sonra sahibin hareketini birebir aynalar.
public class DecoyClone : MonoBehaviour
{
    [Header("Phases")]
    [Tooltip("Spawn sonrasi kendi basina dolasma suresi.")]
    public float freeWanderDuration = 2f;

    [Header("Wander")]
    public float wanderMoveSpeed = 7.2f;
    public float wanderRotationSpeed = 12f;
    public float targetReachDistance = 0.75f;
    public float newTargetIntervalMin = 0.7f;
    public float newTargetIntervalMax = 1.6f;
    public float stuckRepickDelay = 0.35f;

    [Header("Mirror")]
    public float rotationCatchUp = 20f;

    [Header("Zone / Walls")]
    public float zonePadding = 0.9f;
    public float capsuleCastSkin = 0.05f;

    [Header("Hit Flash")]
    public float flashBlindDuration = 2.4f;
    public float flashShakeDuration = 0.14f;
    public float flashShakeStrength = 0.2f;

    PlayerDecoy owner;
    Rigidbody rb;
    Rigidbody ownerRb;
    Transform ownerTransform;
    PlayerDuck ownerDuck;
    CapsuleCollider capsule;
    Collider wanderZone;

    public PlayerDecoy Owner => owner;

    float dieAt;
    float mirrorAt;
    bool dead;
    bool mirroring;
    Material[] ownedMats;
    Vector3 baseScale = Vector3.one;

    Vector3 wanderTarget;
    Vector3 wanderMoveDir;
    float nextTargetTime;
    float nextStuckRepickTime;

    public bool IsMoving
    {
        get
        {
            if (dead || rb == null) return false;
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            return v.sqrMagnitude > 0.25f;
        }
    }

    public void Init(PlayerDecoy decoyOwner, float duration, float speed, Collider zone, float wanderSeconds = 2f)
    {
        owner = decoyOwner;
        wanderZone = zone;
        wanderMoveSpeed = speed;
        freeWanderDuration = Mathf.Max(0f, wanderSeconds);
        dieAt = Time.unscaledTime + duration;
        mirrorAt = Time.unscaledTime + freeWanderDuration;
        mirroring = freeWanderDuration <= 0f;
        PlayerDecoy.Register(this);

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        baseScale = transform.localScale;

        if (owner != null)
        {
            ownerTransform = owner.transform;
            ownerRb = owner.GetComponent<Rigidbody>();
            ownerDuck = owner.GetComponent<PlayerDuck>();

            if (ownerRb != null)
            {
                rb.rotation = ownerRb.rotation;
            }
        }

        // Ilk anda oyuncudan uzaklasacak bir hedef sec.
        PickWanderTargetAwayFromOwner();
    }

    void Update()
    {
        if (dead) return;

        if (!GameManager.RoundIsActive || Time.unscaledTime >= dieAt)
        {
            Pop(transform.position + Vector3.up, Vector3.up, Color.white, playHitFx: false);
            return;
        }

        if (!mirroring && Time.unscaledTime >= mirrorAt)
        {
            mirroring = true;
        }

        if (mirroring)
        {
            MirrorDuckScale();
        }
        else
        {
            // Wander fazinda egilme kopyalama — dik dur.
            ResetDuckScale();
            UpdateWanderDirection();
        }
    }

    void FixedUpdate()
    {
        if (dead || rb == null) return;

        if (!GameManager.RoundIsActive)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (mirroring)
        {
            FixedMirror();
        }
        else
        {
            FixedWander();
        }
    }

    void FixedWander()
    {
        Vector3 desired = wanderMoveDir * wanderMoveSpeed;
        desired = SlideAlongWalls(desired);
        desired.y = 0f;

        rb.linearVelocity = desired;
        rb.angularVelocity = Vector3.zero;

        if (wanderMoveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(wanderMoveDir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                wanderRotationSpeed * Time.fixedDeltaTime
            ));
        }

        Vector3 pos = rb.position;
        ClampPosition(ref pos);
        if ((pos - rb.position).sqrMagnitude > 0.0001f)
        {
            rb.position = pos;
        }

        if (wanderMoveDir.sqrMagnitude > 0.01f &&
            rb.linearVelocity.sqrMagnitude < 0.35f &&
            Time.time >= nextStuckRepickTime)
        {
            nextStuckRepickTime = Time.time + stuckRepickDelay;
            PickWanderTargetAwayFromOwner();
        }
    }

    void FixedMirror()
    {
        if (ownerRb == null || ownerTransform == null)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        PlayerHealth ownerHealth = owner != null ? owner.GetComponent<PlayerHealth>() : null;
        if (ownerHealth != null && ownerHealth.IsEliminated)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        Vector3 ownerVel = ownerRb.linearVelocity;
        Vector3 horizontal = new Vector3(ownerVel.x, 0f, ownerVel.z);
        horizontal = SlideAlongWalls(horizontal);

        Vector3 mirrored = horizontal;
        mirrored.y = ownerVel.y;
        rb.linearVelocity = mirrored;
        rb.angularVelocity = Vector3.zero;

        Quaternion targetRot = Quaternion.Euler(0f, ownerRb.rotation.eulerAngles.y, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationCatchUp * Time.fixedDeltaTime));

        Vector3 pos = rb.position;
        ClampPosition(ref pos);
        if ((pos - rb.position).sqrMagnitude > 0.0001f)
        {
            rb.position = pos;
        }
    }

    void UpdateWanderDirection()
    {
        wanderMoveDir = Vector3.zero;

        Vector3 toTarget = wanderTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= targetReachDistance || Time.time >= nextTargetTime)
        {
            PickWanderTargetAwayFromOwner();
            toTarget = wanderTarget - transform.position;
            toTarget.y = 0f;
        }

        if (toTarget.sqrMagnitude > 0.01f)
        {
            wanderMoveDir = toTarget.normalized;
        }
    }

    void PickWanderTargetAwayFromOwner()
    {
        nextTargetTime = Time.time + Random.Range(newTargetIntervalMin, newTargetIntervalMax);

        Vector3 from = transform.position;
        Vector3 away = Vector3.zero;
        if (ownerTransform != null)
        {
            away = from - ownerTransform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f && ownerRb != null)
            {
                Vector3 v = ownerRb.linearVelocity;
                v.y = 0f;
                away = v.sqrMagnitude > 0.01f ? v : ownerTransform.forward;
            }

            if (away.sqrMagnitude < 0.01f)
            {
                away = transform.forward;
            }

            away.Normalize();
        }

        // Birkaç adaydan oyuncuya en uzak / away yonune en uygun olani sec.
        Vector3 best = from + away * Random.Range(4f, 8f);
        float bestScore = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate;
            if (wanderZone != null)
            {
                Bounds bounds = wanderZone.bounds;
                candidate = new Vector3(
                    Random.Range(bounds.min.x + zonePadding, bounds.max.x - zonePadding),
                    from.y,
                    Random.Range(bounds.min.z + zonePadding, bounds.max.z - zonePadding));
            }
            else
            {
                Vector2 offset = Random.insideUnitCircle * Random.Range(3f, 9f);
                candidate = from + new Vector3(offset.x, 0f, offset.y);
            }

            float distOwner = ownerTransform != null
                ? (candidate - ownerTransform.position).sqrMagnitude
                : 0f;
            float align = away.sqrMagnitude > 0.01f
                ? Vector3.Dot((candidate - from).normalized, away)
                : 0f;
            float score = distOwner + align * 12f;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        wanderTarget = best;
    }

    void MirrorDuckScale()
    {
        float targetY = 1f;
        if (ownerDuck != null && ownerDuck.IsDucking)
        {
            targetY = ownerDuck.duckHeightScale;
        }

        ApplyScaleY(targetY);
    }

    void ResetDuckScale()
    {
        ApplyScaleY(1f);
    }

    void ApplyScaleY(float targetY)
    {
        Vector3 s = transform.localScale;
        s.y = Mathf.MoveTowards(s.y, baseScale.y * targetY, 10f * Time.deltaTime);
        s.x = baseScale.x;
        s.z = baseScale.z;
        transform.localScale = s;
    }

    Vector3 SlideAlongWalls(Vector3 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude < 0.0001f || capsule == null)
        {
            return desiredVelocity;
        }

        Vector3 dir = desiredVelocity.normalized;
        float dist = desiredVelocity.magnitude * Time.fixedDeltaTime + capsuleCastSkin;
        GetCapsuleWorldEnds(out Vector3 p1, out Vector3 p2, out float radius);

        if (Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.GetComponentInParent<DecoyClone>() == this)
            {
                return desiredVelocity;
            }

            Vector3 slid = Vector3.ProjectOnPlane(desiredVelocity, hit.normal);
            slid.y = 0f;

            if (slid.sqrMagnitude > 0.01f)
            {
                Vector3 slidDir = slid.normalized;
                if (Physics.CapsuleCast(p1, p2, radius, slidDir, out RaycastHit hit2, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    slid = Vector3.ProjectOnPlane(slid, hit2.normal);
                    slid.y = 0f;
                }
            }

            return slid;
        }

        return desiredVelocity;
    }

    void GetCapsuleWorldEnds(out Vector3 p1, out Vector3 p2, out float radius)
    {
        float scaleY = Mathf.Max(0.2f, transform.lossyScale.y);
        radius = Mathf.Max(0.05f, capsule.radius * 0.95f * Mathf.Min(transform.lossyScale.x, transform.lossyScale.z));
        float height = Mathf.Max(capsule.height * scaleY, radius * 2f);
        float half = height * 0.5f - radius;

        Vector3 center = transform.TransformPoint(capsule.center);
        p1 = center + Vector3.up * half;
        p2 = center - Vector3.up * half;
    }

    void ClampPosition(ref Vector3 pos)
    {
        if (wanderZone == null) return;

        Bounds bounds = wanderZone.bounds;
        pos.x = Mathf.Clamp(pos.x, bounds.min.x + zonePadding, bounds.max.x - zonePadding);
        pos.z = Mathf.Clamp(pos.z, bounds.min.z + zonePadding, bounds.max.z - zonePadding);
    }

    public void BindOwnedMaterials(Material[] mats)
    {
        ownedMats = mats;
    }

    public void PopFromBallHit(Vector3 hitPoint, Vector3 hitNormal, Color ballColor)
    {
        Pop(hitPoint, hitNormal, ballColor, playHitFx: true);
    }

    public void Pop(Vector3 hitPoint, Vector3 hitNormal, Color ballColor, bool playHitFx = true)
    {
        if (dead) return;
        dead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playHitFx)
        {
            Vector3 flashPos = transform.position + Vector3.up * 0.9f;
            FlashProjectile.ExplodeAt(
                flashPos,
                flashBlindDuration,
                flashShakeDuration,
                flashShakeStrength);
        }

        if (owner != null)
        {
            owner.NotifyDecoyDestroyed(this);
        }

        PlayerDecoy.Unregister(this);
        Destroy(gameObject);
    }

    public static bool IsDecoy(Component target)
    {
        return target != null && target.GetComponentInParent<DecoyClone>() != null;
    }

    void OnDestroy()
    {
        PlayerDecoy.Unregister(this);

        if (ownedMats == null) return;
        for (int i = 0; i < ownedMats.Length; i++)
        {
            if (ownedMats[i] != null)
            {
                Destroy(ownedMats[i]);
            }
        }
    }
}
