using UnityEngine;

// Prosedurel karakter animasyonu: model olmadan "canlilik".
// - Kosarken hafif ziplama (bob)
// - Ziplarken uzama, yere inerken yayilma (squash & stretch)
// - Egilince yumusak kuculme
// - Otomatik olusturulan iki goz + goz kirpma
// Ileride gercek 3D model ve Animator gelirse bu script devre disi birakilir.
public class CharacterAnimator : MonoBehaviour
{
    [Header("Eyes")]
    public bool createEyes = true;
    public Color eyeColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    public float eyeSize = 0.16f;
    public float blinkInterval = 3.5f;
    public float blinkDuration = 0.12f;

    [Header("Squash & Stretch")]
    [Tooltip("Ziplarken boyuna uzama carpani.")]
    public float jumpStretch = 1.12f;

    [Tooltip("Yere inis aninda basilma carpani.")]
    public float landSquash = 0.82f;

    [Tooltip("Normale donus hizi.")]
    public float recoverSpeed = 8f;

    [Header("Run Bob")]
    public float bobAmount = 0.045f;
    public float bobSpeed = 11f;

    [Header("Duck")]
    [Tooltip("Egilme/kalkma gecis hizi.")]
    public float duckLerpSpeed = 14f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.15f;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerDuck playerDuck;

    private Vector3 baseScale;
    private float heightFactor = 1f;   // Egilme icin yumusak carpan.
    private float impulseFactor = 1f;  // Zipla/inis squash & stretch carpani.
    private bool wasGrounded = true;

    private Transform leftEye;
    private Transform rightEye;
    private float nextBlinkTime;
    private float blinkEndTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        playerDuck = GetComponent<PlayerDuck>();

        baseScale = transform.localScale;

        if (createEyes)
        {
            CreateEyes();
        }

        nextBlinkTime = Time.time + Random.Range(1f, blinkInterval);
    }

    void LateUpdate()
    {
        // Elenmis karakter ragdoll halinde; animasyona karismiyoruz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            transform.localScale = baseScale;
            heightFactor = 1f;
            impulseFactor = 1f;
            return;
        }

        UpdateSquashStretch();
        UpdateBlink();
    }

    void UpdateSquashStretch()
    {
        // Egilme: hedef boy carpanina yumusak gecis.
        float targetHeight = 1f;

        if (playerDuck != null && playerDuck.IsDucking)
        {
            targetHeight = playerDuck.duckHeightScale;
        }

        heightFactor = Mathf.Lerp(heightFactor, targetHeight, duckLerpSpeed * Time.deltaTime);

        bool grounded = IsGrounded();

        if (rb != null && !rb.isKinematic)
        {
            // Yukselirken uzun ve ince gorun.
            if (!grounded && rb.linearVelocity.y > 0.5f)
            {
                impulseFactor = jumpStretch;
            }

            // Yere temas aninda basil (kisa sureli).
            if (grounded && !wasGrounded)
            {
                impulseFactor = landSquash;
            }
        }

        wasGrounded = grounded;

        impulseFactor = Mathf.Lerp(impulseFactor, 1f, recoverSpeed * Time.deltaTime);

        // Kosarken hafif asagi-yukari zipla.
        float bob = 0f;

        if (grounded && rb != null && !rb.isKinematic)
        {
            Vector3 horizontalVelocity = rb.linearVelocity;
            horizontalVelocity.y = 0f;

            if (horizontalVelocity.magnitude > 1f)
            {
                bob = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmount;
            }
        }

        // Boy kisalirken en (x/z) hafif genislesin: hacim korunuyor hissi.
        float yFactor = heightFactor * impulseFactor + bob;
        float xzFactor = 1f + (1f - impulseFactor) * 0.7f;

        transform.localScale = new Vector3(
            baseScale.x * xzFactor,
            baseScale.y * yFactor,
            baseScale.z * xzFactor
        );
    }

    // Gercek 3D model yuklendiginde kapsul gozlerine gerek kalmaz.
    public void DisableEyes()
    {
        if (leftEye != null) Destroy(leftEye.gameObject);
        if (rightEye != null) Destroy(rightEye.gameObject);

        leftEye = null;
        rightEye = null;
    }

    void CreateEyes()
    {
        leftEye = CreateEye(new Vector3(-0.17f, 0.5f, 0.42f));
        rightEye = CreateEye(new Vector3(0.17f, 0.5f, 0.42f));
    }

    Transform CreateEye(Vector3 localPosition)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";

        // Goz sadece gorsel: fizik carpismasina girmesin.
        Destroy(eye.GetComponent<Collider>());

        eye.transform.SetParent(transform, false);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = Vector3.one * eyeSize;

        Renderer eyeRenderer = eye.GetComponent<Renderer>();
        eyeRenderer.material.color = eyeColor;
        eyeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return eye.transform;
    }

    void UpdateBlink()
    {
        if (leftEye == null || rightEye == null) return;

        if (Time.time >= nextBlinkTime)
        {
            blinkEndTime = Time.time + blinkDuration;
            nextBlinkTime = Time.time + blinkInterval + Random.Range(-1f, 1f);
        }

        bool blinking = Time.time < blinkEndTime;
        float eyeHeight = blinking ? eyeSize * 0.12f : eyeSize;

        Vector3 eyeScale = new Vector3(eyeSize, eyeHeight, eyeSize);

        leftEye.localScale = eyeScale;
        rightEye.localScale = eyeScale;
    }

    bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance * Mathf.Max(0.3f, transform.localScale.y)
        );
    }
}
