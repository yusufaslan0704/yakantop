using UnityEngine;

// Ziplama: alcak gelen toplarin ustunden atlamak icin.
[RequireComponent(typeof(Rigidbody))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump")]
    public float jumpForce = 7f;

    [Tooltip("Karakter merkezinden asagi atilan isinin uzunlugu (zemin kontrolu).")]
    public float groundCheckDistance = 1.15f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;
    private PlayerDuck playerDuck;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerDuck = GetComponent<PlayerDuck>();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive) return;

        if (playerHealth != null && playerHealth.IsEliminated) return;

        if (rb.isKinematic) return;

        if (inputHandler == null || !inputHandler.JumpPressed) return;

        // Egilirken ziplanmaz.
        if (playerDuck != null && playerDuck.IsDucking) return;

        if (!IsGrounded()) return;

        // Dikey hizi sifirlayip ziplariz ki ust uste binmesin.
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public bool IsGrounded()
    {
        // Kendi collider'imizi es gecmek icin merkezden asagi kisa bir isin.
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance * transform.localScale.y
        );
    }
}
