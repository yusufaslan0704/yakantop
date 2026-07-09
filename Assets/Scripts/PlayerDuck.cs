using UnityEngine;

// Egilme durumu: yuksek gelen toplarin altindan kacmak icin.
// Kuculme gorseli ve collider degisimi CharacterAnimator'da
// (root scale uzerinden) yumusak sekilde islenir; burasi sadece
// "su an egiliyor mu?" sorusunun cevabini tutar.
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
    private CharacterAnimator characterAnimator;
    private CharacterModelVisual characterModelVisual;

    private bool wasDucking;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        characterAnimator = GetComponent<CharacterAnimator>();
        characterModelVisual = GetComponent<CharacterModelVisual>();
    }

    void Update()
    {
        bool ducking = GameManager.RoundIsActive &&
                       inputHandler != null &&
                       inputHandler.DuckHeld &&
                       (playerHealth == null || !playerHealth.IsEliminated) &&
                       !rb.isKinematic;

        if (wasDucking && !ducking)
        {
            ResetVisualPose();
        }

        wasDucking = ducking;
        IsDucking = ducking;
    }

    void OnDisable()
    {
        IsDucking = false;
        wasDucking = false;
        ResetVisualPose();
    }

    void ResetVisualPose()
    {
        if (characterAnimator != null)
        {
            characterAnimator.ResetPose();
        }

        if (characterModelVisual != null)
        {
            characterModelVisual.ResetPose();
        }
    }
}
