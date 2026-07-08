using UnityEngine;

// Ziplama ve egilme: kacis oyununa dikey boyut ekler.
// Atici alcak/yuksek atislar yapinca kacan ziplayarak veya egilerek kurtulur.
[RequireComponent(typeof(Rigidbody))]
public class PlayerJumpDuck : MonoBehaviour
{
    [Header("Jump")]
    public float jumpForce = 7f;

    [Tooltip("Yere bu mesafeden yakinsa 'yerde' sayilir.")]
    public float groundCheckDistance = 1.15f;

    [Header("Duck")]
    [Tooltip("Egilirken karakterin boyu bu orana iner.")]
    [Range(0.3f, 0.9f)] public float duckHeightScale = 0.55f;

    [Tooltip("Egilirken hareket hizi carpani.")]
    [Range(0.1f, 1f)] public float duckSpeedMultiplier = 0.45f;

    public bool IsDucking { get; private set; }

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;

    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();

        originalScale = transform.localScale;
    }

    void Update()
    {
        if (!GameManager.RoundIsActive ||
            (playerHealth != null && playerHealth.IsEliminated) ||
            rb.isKinematic)
        {
            StopDuck();
            return;
        }

        if (inputHandler == null)
        {
            return;
        }

        bool grounded = IsGrounded();

        // Ziplama: yerdeyken ve egilmiyorken.
        if (inputHandler.JumpPressed && grounded && !IsDucking)
        {
            Jump();
        }

        // Egilme: tus basili ve yerdeyken. Havada egilme yok.
        if (inputHandler.DuckHeld && grounded)
        {
            StartDuck();
        }
        else
        {
            StopDuck();
        }
    }

    void Jump()
    {
        // Dikey hizi sifirlayip sabit guclu ziplama (ust uste binmesin).
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }
    }

    void StartDuck()
    {
        if (IsDucking) return;

        IsDucking = true;

        // Boyu kisalt: collider transform'la birlikte kuculur,
        // boylece toplar egilen karakterin ustunden gecebilir.
        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y * duckHeightScale,
            originalScale.z
        );
    }

    void StopDuck()
    {
        if (!IsDucking) return;

        IsDucking = false;
        transform.localScale = originalScale;
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    void OnDisable()
    {
        // Kontrol degisirse egik kalmasin.
        StopDuck();
    }
}
