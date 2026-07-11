using UnityEngine;

// Kosucu botu: tehditten kacar, gerekirse SafeZone'a siginir, yoksa dolasir.
// Solo test ve coklu Runner kurallarini gamepad olmadan denemek icin.
[RequireComponent(typeof(Rigidbody))]
public class RunnerBot : MonoBehaviour
{
    [Header("Wander")]
    public Collider wanderZone;
    public float moveSpeed = 6.5f;
    public float rotationSpeed = 12f;
    public float targetReachDistance = 0.7f;
    public float newTargetIntervalMin = 1.5f;
    public float newTargetIntervalMax = 3f;
    public float zonePadding = 1f;

    [Header("Threat Avoid")]
    [Tooltip("Bu mesafedeki toplar / aticilar kacisi tetikler.")]
    public float ballThreatRadius = 9f;
    public float throwerThreatRadius = 14f;
    public float fleeDistance = 6f;

    [Header("Safe Zone")]
    [Tooltip("Tehdit varken ve koruma butcesi varsa cebe kos.")]
    public float seekPocketThreatRadius = 11f;
    [Range(0f, 1f)] public float minPocketBudgetToEnter = 0.25f;
    [Range(0f, 1f)] public float leavePocketBudget = 0.08f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;

    private Vector3 wanderTarget;
    private Vector3 moveDirection;
    private float nextTargetTime;
    private float nextThreatScanTime;
    private Vector3 cachedThreat;
    private float cachedThreatScore;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        PickWanderTarget();
    }

    void Update()
    {
        moveDirection = Vector3.zero;

        if (!GameManager.RoundIsActive)
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        if (Time.time >= nextThreatScanTime)
        {
            cachedThreat = EvaluateThreat(out cachedThreatScore);
            nextThreatScanTime = Time.time + 0.12f;
        }

        Vector3 threat = cachedThreat;
        float threatScore = cachedThreatScore;
        bool insidePocket = SafeZone.IsInsideAny(playerHealth);
        float pocketBudget = SafeZone.GetProtectionPercent(playerHealth);

        if (insidePocket)
        {
            // Butce bitmek uzereyse veya tehdit yoksa cepten cik.
            if (pocketBudget <= leavePocketBudget || threatScore < 0.15f)
            {
                moveDirection = GetExitPocketDirection(threat);
            }
            else
            {
                // Cepte kal: hafif kayma (kamp gibi durmasin).
                moveDirection = GetSoftStrafe(threat);
            }

            return;
        }

        // Tehdit guclu + butce varsa en yakin cebe kos.
        if (threatScore >= 0.35f &&
            pocketBudget >= minPocketBudgetToEnter &&
            threat.magnitude <= seekPocketThreatRadius &&
            SafeZone.TryGetNearestCenter(transform.position, out Vector3 pocketCenter))
        {
            Vector3 toPocket = pocketCenter - transform.position;
            toPocket.y = 0f;
            if (toPocket.sqrMagnitude > 0.05f)
            {
                moveDirection = toPocket.normalized;
                return;
            }
        }

        // Tehditten kac.
        if (threatScore >= 0.2f && threat.sqrMagnitude > 0.01f)
        {
            moveDirection = (-threat).normalized;
            ClampToWanderZone(ref moveDirection);
            return;
        }

        // Normal dolasma.
        Vector3 toTarget = wanderTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= targetReachDistance || Time.time >= nextTargetTime)
        {
            PickWanderTarget();
            toTarget = wanderTarget - transform.position;
            toTarget.y = 0f;
        }

        if (toTarget.sqrMagnitude > 0.01f)
        {
            moveDirection = toTarget.normalized;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            ));
        }
    }

    // Tehdit vektoru: toplar + aticilar. Buyukluk ~ tehdit siddeti.
    Vector3 EvaluateThreat(out float score)
    {
        Vector3 threat = Vector3.zero;
        score = 0f;
        Vector3 self = transform.position;

        Ball[] balls = FindObjectsByType<Ball>(FindObjectsSortMode.None);
        foreach (Ball ball in balls)
        {
            if (ball == null) continue;

            Vector3 toBall = ball.transform.position - self;
            toBall.y = 0f;
            float dist = toBall.magnitude;
            if (dist > ballThreatRadius || dist < 0.05f) continue;

            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            Vector3 approach = toBall.normalized;

            // Bize dogru gelen toplar daha tehlikeli.
            float closing = 0f;
            if (ballRb != null)
            {
                Vector3 vel = ballRb.linearVelocity;
                vel.y = 0f;
                closing = Mathf.Max(0f, Vector3.Dot(vel.normalized, -approach));
            }

            float weight = (1f - dist / ballThreatRadius) * (0.45f + 0.55f * closing);
            threat += approach * weight;
            score = Mathf.Max(score, weight);
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null || player.roleType != RoleType.Thrower) continue;
            if (player.Health != null && player.Health.IsEliminated) continue;

            Vector3 toThrower = player.transform.position - self;
            toThrower.y = 0f;
            float dist = toThrower.magnitude;
            if (dist > throwerThreatRadius || dist < 0.05f) continue;

            float weight = (1f - dist / throwerThreatRadius) * 0.55f;
            threat += toThrower.normalized * weight;
            score = Mathf.Max(score, weight * 0.85f);
        }

        if (threat.sqrMagnitude > 0.0001f)
        {
            threat = threat.normalized * Mathf.Clamp(threat.magnitude, 0f, 1f) * fleeDistance;
        }

        return threat;
    }

    Vector3 GetExitPocketDirection(Vector3 threat)
    {
        Vector3 away = -threat;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
        {
            // Tehdit yoksa arena merkezine / rastgele yone cik.
            away = -transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                away = Random.insideUnitSphere;
                away.y = 0f;
            }
        }

        return away.normalized;
    }

    Vector3 GetSoftStrafe(Vector3 threat)
    {
        Vector3 side = Vector3.Cross(Vector3.up, -threat);
        if (side.sqrMagnitude < 0.01f)
        {
            side = transform.right;
        }

        side.y = 0f;
        return side.normalized * 0.35f;
    }

    void ClampToWanderZone(ref Vector3 direction)
    {
        if (wanderZone == null || direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Bounds bounds = wanderZone.bounds;
        Vector3 next = transform.position + direction.normalized * 2f;

        if (next.x < bounds.min.x + zonePadding || next.x > bounds.max.x - zonePadding ||
            next.z < bounds.min.z + zonePadding || next.z > bounds.max.z - zonePadding)
        {
            // Bolge disina tasmamak icin hedefi bolge merkezine cevir.
            Vector3 toCenter = bounds.center - transform.position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.01f)
            {
                direction = toCenter.normalized;
            }
        }
    }

    void PickWanderTarget()
    {
        nextTargetTime = Time.time + Random.Range(newTargetIntervalMin, newTargetIntervalMax);

        if (wanderZone == null)
        {
            Vector2 offset = Random.insideUnitCircle * 4f;
            wanderTarget = transform.position + new Vector3(offset.x, 0f, offset.y);
            return;
        }

        Bounds bounds = wanderZone.bounds;
        float x = Random.Range(bounds.min.x + zonePadding, bounds.max.x - zonePadding);
        float z = Random.Range(bounds.min.z + zonePadding, bounds.max.z - zonePadding);
        wanderTarget = new Vector3(x, transform.position.y, z);
    }
}
