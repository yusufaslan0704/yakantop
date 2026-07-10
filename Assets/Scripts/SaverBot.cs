using UnityEngine;

// Saver botu: insan Saver kontrol etmiyorken elenen Runner'a gider ve revive tutar.
// TeamControlManager Saver'i insan kontrolune alinca bu script kendini kapatir.
[RequireComponent(typeof(Rigidbody))]
public class SaverBot : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 12f;
    public float stopDistance = 1.1f;

    [Header("Threat Avoid (hafif)")]
    public float ballThreatRadius = 6f;
    public float sidestepStrength = 0.55f;

    [Header("Idle")]
    public float idleOrbitRadius = 3.5f;
    public float idleRetargetInterval = 2.2f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerRole playerRole;
    private PlayerRevive playerRevive;
    private PlayerInputHandler inputHandler;
    private PlayerMovement playerMovement;

    private Vector3 moveDirection;
    private Vector3 idleTarget;
    private float nextIdleTargetTime;
    private bool botHoldingRevive;

    public bool IsBotControlling { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        playerRole = GetComponent<PlayerRole>();
        playerRevive = GetComponent<PlayerRevive>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void OnDisable()
    {
        ReleaseBotRevive();
        IsBotControlling = false;
    }

    void Update()
    {
        moveDirection = Vector3.zero;

        if (!ShouldRun())
        {
            ReleaseBotRevive();
            IsBotControlling = false;
            return;
        }

        IsBotControlling = true;

        // Insan hareketi ile cakismasin.
        if (playerMovement != null && playerMovement.enabled)
        {
            playerMovement.enabled = false;
        }

        PlayerHealth downed = FindClosestDownedRunner();

        if (downed != null)
        {
            ChaseAndRevive(downed);
        }
        else
        {
            ReleaseBotRevive();
            IdleNearAliveRunners();
        }
    }

    void FixedUpdate()
    {
        if (!IsBotControlling || rb == null || rb.isKinematic)
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

    bool ShouldRun()
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerRole == null || playerRole.roleType != RoleType.Saver) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;

        // Insan Saver aktif kontrol ediyorsa bot durur.
        if (inputHandler != null && inputHandler.enabled &&
            playerMovement != null && playerMovement.enabled)
        {
            return false;
        }

        return true;
    }

    void ChaseAndRevive(PlayerHealth downed)
    {
        Vector3 toTarget = downed.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance > stopDistance)
        {
            moveDirection = toTarget.normalized;
            moveDirection = BlendThreatSidestep(moveDirection);
            ReleaseBotRevive();
            return;
        }

        // Menzilde: dur ve revive tut.
        moveDirection = Vector3.zero;
        HoldBotRevive(true);

        // PlayerRevive bot hold'u okusun diye component acik olmali.
        if (playerRevive != null && !playerRevive.enabled)
        {
            playerRevive.enabled = true;
        }
    }

    void IdleNearAliveRunners()
    {
        PlayerRole aliveRunner = PlayerManager.GetClosestAlive(RoleType.Runner, transform.position);

        if (aliveRunner == null)
        {
            if (Time.time >= nextIdleTargetTime)
            {
                Vector2 offset = Random.insideUnitCircle * idleOrbitRadius;
                idleTarget = transform.position + new Vector3(offset.x, 0f, offset.y);
                nextIdleTargetTime = Time.time + idleRetargetInterval;
            }
        }
        else if (Time.time >= nextIdleTargetTime)
        {
            Vector2 offset = Random.insideUnitCircle * idleOrbitRadius;
            idleTarget = aliveRunner.transform.position + new Vector3(offset.x, 0f, offset.y);
            nextIdleTargetTime = Time.time + idleRetargetInterval;
        }

        Vector3 toIdle = idleTarget - transform.position;
        toIdle.y = 0f;

        if (toIdle.magnitude > 0.6f)
        {
            moveDirection = BlendThreatSidestep(toIdle.normalized);
        }
    }

    Vector3 BlendThreatSidestep(Vector3 desired)
    {
        Vector3 threat = Vector3.zero;
        Ball[] balls = FindObjectsByType<Ball>(FindObjectsSortMode.None);

        foreach (Ball ball in balls)
        {
            if (ball == null) continue;

            Vector3 toBall = ball.transform.position - transform.position;
            toBall.y = 0f;
            float dist = toBall.magnitude;
            if (dist > ballThreatRadius || dist < 0.05f) continue;

            float weight = 1f - dist / ballThreatRadius;
            threat += toBall.normalized * weight;
        }

        if (threat.sqrMagnitude < 0.01f)
        {
            return desired;
        }

        Vector3 away = -threat.normalized;
        return (desired + away * sidestepStrength).normalized;
    }

    PlayerHealth FindClosestDownedRunner()
    {
        PlayerHealth closest = null;
        float bestSqr = float.MaxValue;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null || player.roleType != RoleType.Runner) continue;
            if (player.Health == null || !player.Health.IsEliminated) continue;
            if (DecoyClone.IsDecoy(player)) continue;

            float sqr = (player.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = player.Health;
            }
        }

        return closest;
    }

    void HoldBotRevive(bool hold)
    {
        botHoldingRevive = hold;
    }

    void ReleaseBotRevive()
    {
        botHoldingRevive = false;
    }

    public bool IsHoldingRevive()
    {
        return IsBotControlling && botHoldingRevive;
    }
}
