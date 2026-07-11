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
    private float curveBias;
    private bool curveBiasOverride;
    private Vector3 lastVelocity;

    Vector3[] followPath;
    int followIndex;
    float followSpeed;
    bool followingPath;
    bool onMirrorReturn;

    bool spawnTrapOnImpact;
    float trapRadius = 3.2f;
    float trapDuration = 4.5f;
    float trapSpeedMultiplier = 0.42f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Bot / override yoksa rastgele yan; insan path follow kullanir.
        curveBias = Random.value < 0.5f ? -1f : 1f;
        curveBiasOverride = false;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return;

        if (followingPath)
        {
            TickFollowPath();
            return;
        }

        // Egri top (bot / path disi): ucus yonune dik yatay kuvvet.
        if (data != null && data.curveForce > 0f && rb.linearVelocity.sqrMagnitude > 1f)
        {
            float bias = curveBiasOverride ? curveBias : Mathf.Sign(curveBias);
            if (Mathf.Abs(bias) >= 0.02f)
            {
                Vector3 sideDirection = Vector3.Cross(Vector3.up, rb.linearVelocity.normalized);
                float strength = data.curveForce * Mathf.Lerp(0.45f, 1.4f, Mathf.Abs(bias));
                rb.AddForce(sideDirection * Mathf.Sign(bias) * strength, ForceMode.Force);
            }
        }

        lastVelocity = rb.linearVelocity;
    }

    // Onizlemede cizilen izi takip et — gosterilen yol = giden yol.
    public void FollowPath(Vector3[] worldPoints, float speed)
    {
        if (worldPoints == null || worldPoints.Length < 2)
        {
            followingPath = false;
            followPath = null;
            return;
        }

        followPath = (Vector3[])worldPoints.Clone();
        followIndex = 1;
        followSpeed = Mathf.Max(4f, speed);
        followingPath = true;
        onMirrorReturn = false;

        if (rb != null)
        {
            rb.position = followPath[0];
            rb.linearVelocity = (followPath[1] - followPath[0]).normalized * followSpeed;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = followPath[0];
        }
    }

    void TickFollowPath()
    {
        if (followPath == null || followIndex >= followPath.Length)
        {
            HandlePathEnd();
            return;
        }

        Vector3 target = followPath[followIndex];
        Vector3 pos = rb.position;
        Vector3 to = target - pos;
        float dist = to.magnitude;
        float step = followSpeed * Time.fixedDeltaTime;

        if (dist <= step || dist < 0.04f)
        {
            rb.MovePosition(target);
            followIndex++;
            if (followIndex >= followPath.Length)
            {
                HandlePathEnd();
                return;
            }

            target = followPath[followIndex];
            to = target - rb.position;
        }

        if (to.sqrMagnitude > 0.0001f)
        {
            rb.linearVelocity = to.normalized * followSpeed;
            lastVelocity = rb.linearVelocity;
        }
    }

    void HandlePathEnd()
    {
        // Ayna: gidis bitti → ayni izi tersine bir kez don.
        if (IsMirrorBall() && !onMirrorReturn && followPath != null && followPath.Length >= 2)
        {
            BeginMirrorPathReturn();
            return;
        }

        // Donus bitti → yok ol.
        if (onMirrorReturn)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 endVel = followPath != null && followPath.Length >= 2
            ? (followPath[followPath.Length - 1] - followPath[followPath.Length - 2])
            : rb.linearVelocity;
        if (endVel.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = endVel.normalized * followSpeed;
        }

        StopFollowPath(keepVelocity: true);
    }

    void BeginMirrorPathReturn()
    {
        onMirrorReturn = true;
        System.Array.Reverse(followPath);
        followIndex = 1;
        followingPath = true;

        if (rb != null && followPath.Length >= 2)
        {
            rb.linearVelocity = (followPath[1] - followPath[0]).normalized * followSpeed;
            lastVelocity = rb.linearVelocity;
        }
    }

    bool IsMirrorBall()
    {
        return data != null && data.mirrorReturn;
    }

    bool TryBeginPhysicsMirrorReturn()
    {
        if (!IsMirrorBall() || onMirrorReturn || rb == null)
        {
            return false;
        }

        onMirrorReturn = true;
        followingPath = false;
        followPath = null;

        Vector3 dir;
        if (lastVelocity.sqrMagnitude > 0.25f)
        {
            dir = -lastVelocity.normalized;
        }
        else if (owner != null)
        {
            dir = owner.transform.position - rb.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = -transform.forward;
            }

            dir.Normalize();
        }
        else
        {
            dir = -transform.forward;
        }

        float speed = Mathf.Max(followSpeed, lastVelocity.magnitude, 12f);
        rb.linearVelocity = dir * speed;
        lastVelocity = rb.linearVelocity;
        return true;
    }

    void StopFollowPath(bool keepVelocity)
    {
        followingPath = false;
        followPath = null;
        if (!keepVelocity && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
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

    // -1 = sola, +1 = saga. |deger| kivrim siddeti. Insan aticida mouse/stick ile set edilir.
    public void SetCurveBias(float bias)
    {
        curveBias = Mathf.Clamp(bias, -1f, 1f);
        curveBiasOverride = true;
    }

    public void EnableTrapOnImpact(float radius, float duration, float speedMultiplier)
    {
        spawnTrapOnImpact = true;
        trapRadius = radius;
        trapDuration = duration;
        trapSpeedMultiplier = speedMultiplier;
        // Trap topu sekmesin; ilk carpista alan biraksin.
        remainingBounces = 0;
    }

    void TrySpawnTrap(Vector3 hitPoint)
    {
        if (!spawnTrapOnImpact) return;
        spawnTrapOnImpact = false;
        TrapZone.Spawn(hitPoint, trapRadius, trapDuration, trapSpeedMultiplier);
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 hitNormal = Vector3.up;
        Vector3 hitPoint = transform.position;

        if (collision.contacts.Length > 0)
        {
            hitPoint = collision.contacts[0].point;
            hitNormal = collision.contacts[0].normal;
        }

        Color ballColor = CombatVfx.ReadBallColor(gameObject, Color.white);
        PlayHitSound();

        // Decoy silueti: oyuncu degil — elenmez, revive edilemez; sadece flash patlar.
        DecoyClone decoy = collision.gameObject.GetComponentInParent<DecoyClone>();
        if (decoy != null)
        {
            GameObject decoyOwnerGo = decoy.Owner != null ? decoy.Owner.gameObject : null;
            KillfeedUI.PushDecoyFlash(owner, decoyOwnerGo);
            decoy.PopFromBallHit(hitPoint, hitNormal, ballColor);
            Destroy(gameObject);
            return;
        }

        PlayerHealth playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();

        // Top yere, duvara veya oyuncu olmayan objeye carptiysa.
        if (playerHealth == null)
        {
            SpawnHitEffect(hitPoint, hitNormal, ballColor, isPlayerHit: false);
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength);

            TrySpawnTrap(hitPoint);

            // Path gidisinde cevre carpismasi ayna/yok etmesin — iz bitsin.
            if (followingPath && !onMirrorReturn)
            {
                return;
            }

            // Ayna: ilk cevre carpismasinda geri don (path yoksa / bot atisi).
            if (TryBeginPhysicsMirrorReturn())
            {
                return;
            }

            // Donus sirasinda tekrar carpti → bitti.
            if (IsMirrorBall() && onMirrorReturn)
            {
                Destroy(gameObject);
                return;
            }

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
            SpawnHitEffect(transform.position, hitNormal, ballColor, isPlayerHit: true);
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength * 0.85f);
            Destroy(gameObject);
            return;
        }

        // Anlik parry: sadece o oyuncu Q'ya tam carpma aninda basarsa.
        PlayerDodge dodge = playerHealth.GetComponent<PlayerDodge>();
        if (dodge != null && dodge.enabled && dodge.TryConsumeParry(this))
        {
            SpawnHitEffect(transform.position, hitNormal, ballColor, isPlayerHit: true);
            ShakeCamera(environmentHitShakeDuration, environmentHitShakeStrength * 0.7f);
            return;
        }

        SpawnHitEffect(hitPoint, hitNormal, ballColor, isPlayerHit: true);
        HitPlayer(playerHealth);
        TrySpawnTrap(playerHealth.transform.position);

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

        string ballLabel = data != null ? data.ballName : null;
        KillfeedUI.PushKill(owner, playerHealth.gameObject, ballLabel);

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

        // Path bitti; sekmeden sonra normal fizik.
        StopFollowPath(keepVelocity: false);

        float speedKeep = data != null ? data.bounceSpeedKeep : 0.8f;

        Vector3 reflected = Vector3.Reflect(lastVelocity, surfaceNormal);

        rb.linearVelocity = reflected * speedKeep;
    }

    void SpawnHitEffect(Vector3 position, Vector3 normal, Color color, bool isPlayerHit)
    {
        if (isPlayerHit)
        {
            CombatVfx.SpawnPlayerHit(position, color);
        }
        else
        {
            CombatVfx.SpawnImpact(position, normal, color);
        }

        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            SceneFolders.ParentTo(fx.transform, SceneFolders.RuntimeSpawned);
        }
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
