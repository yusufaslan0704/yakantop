using UnityEngine;

// Test amacli basit kosucu botu: kendi bolgesinde rastgele dolasir.
// Coklu Runner kurallarini (hedefleme, geri sayim, revive) gamepad
// veya ikinci oyuncu olmadan test edebilmek icin var.
[RequireComponent(typeof(Rigidbody))]
public class RunnerBot : MonoBehaviour
{
    [Header("Wander")]
    public Collider wanderZone;
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;

    [Tooltip("Hedef noktaya bu kadar yaklasinca yeni hedef secilir.")]
    public float targetReachDistance = 0.7f;

    [Tooltip("Hedefe ulasilmasa bile en gec bu araliklarla yeni hedef secilir.")]
    public float newTargetIntervalMin = 1.5f;
    public float newTargetIntervalMax = 3f;

    [Tooltip("Bolge kenarlarina bu kadar mesafe birakilir.")]
    public float zonePadding = 1f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;

    private Vector3 wanderTarget;
    private Vector3 moveDirection;
    private float nextTargetTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        PickNewTarget();
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

        Vector3 toTarget = wanderTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= targetReachDistance || Time.time >= nextTargetTime)
        {
            PickNewTarget();

            toTarget = wanderTarget - transform.position;
            toTarget.y = 0f;
        }

        moveDirection = toTarget.normalized;
    }

    void FixedUpdate()
    {
        if (rb.isKinematic)
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

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            ));
        }
    }

    void PickNewTarget()
    {
        nextTargetTime = Time.time + Random.Range(newTargetIntervalMin, newTargetIntervalMax);

        if (wanderZone == null)
        {
            // Bolge atanmadiysa oldugu yerin cevresinde dolassin.
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
