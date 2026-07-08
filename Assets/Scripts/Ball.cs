using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Life")]
    public float lifeTime = 5f;

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    [Header("Camera Shake")]
    public float environmentHitShakeDuration = 0.08f;
    public float environmentHitShakeStrength = 0.07f;
    public float playerHitShakeDuration = 0.16f;
    public float playerHitShakeStrength = 0.22f;

    [Header("Knockback")]
    public bool useKnockback = true;
    public float knockbackForce = 4f;

    private GameObject owner;

    void Start()
    {
        Destroy(gameObject, lifeTime);
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

    void OnCollisionEnter(Collision collision)
    {
        // Topu atan kisiye carparsa hicbir sey yapma.
        if (owner != null && collision.gameObject.transform.root == owner.transform.root)
        {
            return;
        }

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
            Destroy(gameObject);
            return;
        }

        // Oyuncu zaten elenmisse veya round bittiyse tekrar vurma, skor verme.
        if (playerHealth.IsEliminated || !GameManager.RoundIsActive)
        {
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength);
            Destroy(gameObject);
            return;
        }

        ShakeCamera(playerHitShakeDuration, playerHitShakeStrength);

        ApplyKnockback(collision);

        playerHealth.Eliminate();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }

        Destroy(gameObject);
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

        AudioManager.Instance.PlayHit();
    }

    void ShakeCamera(float duration, float strength)
    {
        if (CameraShake.Instance == null)
        {
            return;
        }

        CameraShake.Instance.Shake(duration, strength);
    }

    void ApplyKnockback(Collision collision)
    {
        if (!useKnockback)
        {
            return;
        }

        Rigidbody hitRigidbody = collision.gameObject.GetComponentInParent<Rigidbody>();

        if (hitRigidbody == null)
        {
            return;
        }

        Vector3 knockbackDirection = collision.transform.position - transform.position;
        knockbackDirection.y = 0f;
        knockbackDirection.Normalize();

        hitRigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
    }
}
