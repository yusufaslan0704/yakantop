using UnityEngine;

// Egilme: yuksek gelen toplarin altindan kacmak icin.
// Karakter kisalir (gorsel + collider birlikte) ve yavaslar.
[RequireComponent(typeof(Rigidbody))]
public class PlayerDuck : MonoBehaviour
{
    [Header("Duck")]
    [Tooltip("Egilirken boyun kaci kadar kisalacagi (0.55 = %55).")]
    [Range(0.3f, 0.9f)] public float duckHeightScale = 0.55f;

    [Tooltip("Egilirken hareket hizi carpani.")]
    [Range(0.1f, 1f)] public float duckSpeedMultiplier = 0.5f;

    public bool IsDucking { get; private set; }

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;

    private Vector3 standingScale;
    private float standingHalfHeight;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();

        standingScale = transform.localScale;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        standingHalfHeight = capsule != null ? capsule.height * 0.5f : 1f;
    }

    void Update()
    {
        bool wantsToDuck = GameManager.RoundIsActive &&
                           inputHandler != null &&
                           inputHandler.DuckHeld &&
                           (playerHealth == null || !playerHealth.IsEliminated) &&
                           !rb.isKinematic;

        if (wantsToDuck && !IsDucking)
        {
            StartDuck();
        }
        else if (!wantsToDuck && IsDucking)
        {
            StopDuck();
        }
    }

    void StartDuck()
    {
        IsDucking = true;

        Vector3 scale = standingScale;
        scale.y *= duckHeightScale;
        transform.localScale = scale;

        // Kisalinca ayaklar yerde kalsin diye govdeyi asagi indiriyoruz.
        float drop = standingHalfHeight * (1f - duckHeightScale) * standingScale.y;
        rb.position += Vector3.down * drop;
    }

    void StopDuck()
    {
        IsDucking = false;

        transform.localScale = standingScale;

        float rise = standingHalfHeight * (1f - duckHeightScale) * standingScale.y;
        rb.position += Vector3.up * rise;
    }

    void OnDisable()
    {
        // Kontrol degisiminde egik kalmasin.
        if (IsDucking)
        {
            StopDuck();
        }
    }
}
