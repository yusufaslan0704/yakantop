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

    // Topun his degerleri (shake, knockback, ses...) BallData'dan gelir.
    public void SetData(BallData newData)
    {
        data = newData;
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

        HitPlayer(playerHealth);

        Destroy(gameObject);
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
        if (CameraShake.Instance == null)
        {
            return;
        }

        CameraShake.Instance.Shake(duration, strength);
    }
}
