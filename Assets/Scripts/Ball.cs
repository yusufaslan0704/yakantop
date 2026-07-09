using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Life")]
    public float lifeTime = 5f;

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    [Header("Environment Hit")]
    public float environmentHitShakeDuration = 0.08f;
    public float environmentHitShakeStrength = 0.07f;

    [Header("Player Hit (BallData yoksa kullanilir)")]
    public float playerHitShakeDuration = 0.16f;
    public float playerHitShakeStrength = 0.22f;
    public float knockbackForce = 4f;
    public float hitStopDuration = 0.05f;

    private GameObject owner;
    private BallData data;

    private Rigidbody rb;
    private int remainingBounces;
    private float curveSign;
    private Vector3 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Egri top hangi yana kivrilacak? Her atista rastgele.
        curveSign = Random.value < 0.5f ? -1f : 1f;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return;

        // Egri top: ucus yonune dik, yatay bir kuvvetle kivrilir.
        if (data != null && data.curveForce > 0f && rb.linearVelocity.sqrMagnitude > 1f)
        {
            Vector3 sideDirection = Vector3.Cross(Vector3.up, rb.linearVelocity.normalized);
            rb.AddForce(sideDirection * curveSign * data.curveForce, ForceMode.Force);
        }

        // Sekme hesabi icin carpma ANINDAN onceki hizi sakliyoruz
        // (OnCollisionEnter'da rb hizi coktan sonmus oluyor).
        lastVelocity = rb.linearVelocity;
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;

        Collider ballCollider = GetComponent<Collider>();

        if (ballCollider == null || owner == null)
        {
            return;
        }

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(ballCollider, ownerCollider, true);
            }
        }
    }

    // Topun his degerleri (shake, knockback, ses...) BallData'dan gelir.
    public void SetData(BallData newData)
    {
        data = newData;
        remainingBounces = data != null ? data.maxBounces : 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = transform.position;

        if (collision.contacts.Length > 0)
        {
            hitPoint = collision.contacts[0].point;
        }

        SpawnHitEffect(hitPoint);
        PlayHitSound();

        PlayerHealth playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();

        // Top yere, duvara veya oyuncu olmayan objeye carptiysa.
        if (playerHealth == null)
        {
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength);

            // Sekten top: yok olmak yerine yuzeyden seker.
            if (remainingBounces > 0 && collision.contacts.Length > 0)
            {
                remainingBounces--;
                Bounce(collision.contacts[0].normal);
                return;
            }

            Destroy(gameObject);
            return;
        }

        // Atan kisiye carptiysa yok say.
        // NOT: transform.root KULLANMA — tum oyuncular "Players" klasoru altinda,
        // root ayni olunca kimse vurulmuyordu.
        if (IsOwnedBy(playerHealth))
        {
            return;
        }

        // Oyuncu zaten elenmisse veya round bittiyse tekrar vurma, skor verme.
        if (playerHealth.IsEliminated || !GameManager.RoundIsActive)
        {
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength);
            Destroy(gameObject);
            return;
        }

        // Guvenli cepteki oyuncu vurulamaz; top ona carpip soner.
        if (SafeZone.IsProtected(playerHealth))
        {
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength);
            Destroy(gameObject);
            return;
        }

        // Kalkan balonu: 1 kez topu emer, oyuncu elenmez.
        PlayerShield shield = playerHealth.GetComponent<PlayerShield>();
        if (shield != null && shield.enabled && shield.TryConsumeShield(this))
        {
            SpawnHitEffect(transform.position);
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength * 0.85f);
            Destroy(gameObject);
            return;
        }

        // Anlik parry: sadece o oyuncu Q'ya tam carpma aninda basarsa.
        PlayerDodge dodge = playerHealth.GetComponent<PlayerDodge>();
        if (dodge != null && dodge.enabled && dodge.TryConsumeParry(this))
        {
            SpawnHitEffect(transform.position);
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength * 0.7f);
            return;
        }

        HitPlayer(playerHealth);

        Destroy(gameObject);
    }

    bool IsOwnedBy(PlayerHealth playerHealth)
    {
        if (owner == null || playerHealth == null)
        {
            return false;
        }

        Transform hit = playerHealth.transform;
        Transform thrower = owner.transform;

        return hit == thrower ||
               hit.IsChildOf(thrower) ||
               thrower.IsChildOf(hit);
    }

    void HitPlayer(PlayerHealth playerHealth)
    {
        float shakeDuration = data != null ? data.hitShakeDuration : playerHitShakeDuration;
        float shakeStrength = data != null ? data.hitShakeStrength : playerHitShakeStrength;
        float knockback = data != null ? data.knockbackForce : knockbackForce;
        float hitStop = data != null ? data.hitStopDuration : hitStopDuration;

        ShakeCamera(shakeDuration, shakeStrength);

        // Savrulma yonu: toptan oyuncuya dogru, yatay duzlemde.
        Vector3 knockbackDirection = playerHealth.transform.position - transform.position;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.0001f)
        {
            knockbackDirection = transform.forward;
        }

        knockbackDirection.Normalize();

        // Savrulmayi Eliminate'e veriyoruz ki elenme animasyonuyla birlikte islensin.
        playerHealth.Eliminate(knockbackDirection * knockback);

        if (GameManager.Instance != null)
        {
            // Skor, topu atan oyuncuya yazilir.
            GameManager.Instance.AddScore(owner);
            GameManager.Instance.DoHitStop(hitStop);
        }
    }

    void Bounce(Vector3 surfaceNormal)
    {
        if (rb == null) return;

        float speedKeep = data != null ? data.bounceSpeedKeep : 0.8f;

        Vector3 reflected = Vector3.Reflect(lastVelocity, surfaceNormal);

        rb.linearVelocity = reflected * speedKeep;
    }

    void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        Instantiate(hitEffectPrefab, position, Quaternion.identity);
    }

    void PlayHitSound()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        // Topun kendi carpma sesi varsa onu cal, yoksa varsayilani.
        if (data != null && data.hitSfx != null)
        {
            AudioManager.Instance.PlayClip(data.hitSfx, AudioManager.Instance.hitVolume);
        }
        else
        {
            AudioManager.Instance.PlayHit();
        }
    }

    void ShakeCamera(float duration, float strength)
    {
        CameraShake.ShakeAll(duration, strength);
    }
}
