using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 12f;

    [Header("Camera")]
    public Transform cameraTransform;

    private Vector3 moveDirection;

    // Dash gibi sistemler oyuncunun o an basmak istedigi yonu okuyabilsin.
    public Vector3 CurrentMoveDirection { get { return moveDirection; } }

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerDash playerDash;
    private PlayerInputHandler inputHandler;
    private PlayerDuck playerDuck;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        playerDash = GetComponent<PlayerDash>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerDuck = GetComponent<PlayerDuck>();
    }

    void Update()
    {
        // Input'u Update'te okuyoruz, fiziksel hareketi FixedUpdate'te uyguluyoruz.
        moveDirection = Vector3.zero;

        // Round bittiyse kimse hareket edemez.
        if (!GameManager.RoundIsActive)
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        if (cameraTransform == null)
        {
            return;
        }

        float horizontal;
        float vertical;

        // Girdi PlayerInputHandler'dan gelir (klavye veya gamepad).
        // Handler yoksa eski usul klavye okunur.
        if (inputHandler != null)
        {
            horizontal = inputHandler.MoveInput.x;
            vertical = inputHandler.MoveInput.y;
        }
        else
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

        if (inputDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
    }

    void FixedUpdate()
    {
        // Elenen oyuncunun rigidbody'si kinematik olur, hız verilemez.
        if (rb.isKinematic)
        {
            return;
        }

        // Elenmiş oyuncunun savrulmasına karışma (fizik yönetiyor).
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        // Dash sırasında hızı PlayerDash yönetiyor.
        if (playerDash != null && playerDash.IsDashing())
        {
            return;
        }

        float speed = moveSpeed;

        // Egilirken yavasla.
        if (playerDuck != null && playerDuck.IsDucking)
        {
            speed *= playerDuck.duckSpeedMultiplier;
        }

        Vector3 velocity = moveDirection * speed;
        velocity.y = rb.linearVelocity.y; // Yerçekimini koru.

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

    void OnDisable()
    {
        // Kontrol başka karaktere geçtiğinde bu karakter kayarak gitmesin.
        moveDirection = Vector3.zero;

        // Elenme savrulması sürüyorsa hıza dokunma.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
    }
}
