using UnityEngine;

public class PlayerThrow : MonoBehaviour
{
    [Header("Ball Settings")]
    public BallData ballData;
    public Transform throwPoint;

    [Header("Aim Settings")]
    public Camera playerCamera;
    public float aimDistance = 100f;

    [Header("Throw Force")]
    public float minThrowForce = 15f;
    public float maxThrowForce = 40f;
    public float maxChargeTime = 1.2f;

    [Header("Throw Cooldown")]
    public float cooldown = 0.5f;

    private float chargeStartTime;
    private float nextThrowTime = 0f;
    private bool isCharging = false;

    private PlayerHealth playerHealth;
    private PlayerRole playerRole;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerRole = GetComponent<PlayerRole>();
    }

    void Update()
    {
        // Round bittiyse atış yapılamaz.
        if (!GameManager.RoundIsActive)
        {
            isCharging = false;
            return;
        }

        // Elenen oyuncu top atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            isCharging = false;
            return;
        }

        // Mouse ile sadece Thrower rolündeki oyuncu top atabilir.
        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            isCharging = false;
            return;
        }

        // Cooldown bitmeden yeni atış hazırlanamaz.
        if (Time.time < nextThrowTime)
        {
            return;
        }

        // Sol tıka basınca charge başlar.
        if (Input.GetMouseButtonDown(0))
        {
            StartCharge();
        }

        // Sol tık bırakılınca top atılır.
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            ReleaseThrow();
        }
    }

    void StartCharge()
    {
        isCharging = true;
        chargeStartTime = Time.time;
    }

    void ReleaseThrow()
    {
        isCharging = false;

        float chargeTime = Time.time - chargeStartTime;
        float chargePercent = Mathf.Clamp01(chargeTime / maxChargeTime);

        float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);

        ThrowBall(finalForce);

        nextThrowTime = Time.time + cooldown;
    }

    void ThrowBall(float force)
    {
        if (ballData == null || ballData.prefab == null)
        {
            Debug.LogWarning("Ball Data atanmadı veya prefab eksik!");
            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning("Throw Point atanmadı!");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Player Camera atanmadı!");
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(aimDistance);
        }

        Vector3 throwDirection = (targetPoint - throwPoint.position).normalized;

        SpawnAndThrowBall(ballData, throwDirection, force);

        RotatePlayerToThrowDirection(throwDirection);
    }

    // Bot atışı: atış gücü ve top tipi BallData'dan gelir.
    public void BotThrowAt(Vector3 targetPosition, BallData botBallData)
    {
        // Round bittiyse bot da atış yapamaz.
        if (!GameManager.RoundIsActive)
        {
            return;
        }

        // Elenen bot top atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        // Bot da sadece Thrower rolündeyse top atabilir.
        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            return;
        }

        // Cooldown kontrolü.
        if (Time.time < nextThrowTime)
        {
            return;
        }

        if (botBallData == null || botBallData.prefab == null)
        {
            Debug.LogWarning("Bot atışı için Ball Data eksik!");
            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning("Bot atışı için Throw Point eksik!");
            return;
        }

        Vector3 throwDirection = (targetPosition - throwPoint.position).normalized;

        SpawnAndThrowBall(botBallData, throwDirection, botBallData.throwForce);

        RotatePlayerToThrowDirection(throwDirection);

        nextThrowTime = Time.time + cooldown;
    }

    void SpawnAndThrowBall(BallData dataToThrow, Vector3 throwDirection, float force)
    {
        // Topu karakterin gövdesinin içinde değil, biraz önünde oluşturuyoruz.
        Vector3 spawnPosition = throwPoint.position + throwDirection * 0.6f;

        GameObject ball = Instantiate(dataToThrow.prefab, spawnPosition, Quaternion.identity);

        // Topa atan kişiyi owner olarak veriyoruz.
        // Böylece top, atan kişiye çarpınca onu elemez ve skor atana yazılır.
        Ball ballScript = ball.GetComponent<Ball>();

        if (ballScript != null)
        {
            ballScript.SetOwner(gameObject);
            ballScript.SetData(dataToThrow);
        }

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.AddForce(throwDirection * force, ForceMode.Impulse);
        }

        PlayThrowSound(dataToThrow);
    }

    void PlayThrowSound(BallData dataToThrow)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        // Topun kendi atış sesi varsa onu çal (örn. Fast Ball whoosh), yoksa varsayılanı.
        if (dataToThrow != null && dataToThrow.throwSfx != null)
        {
            AudioManager.Instance.PlayClip(dataToThrow.throwSfx, AudioManager.Instance.throwVolume);
        }
        else
        {
            AudioManager.Instance.PlayThrow();
        }
    }

    void RotatePlayerToThrowDirection(Vector3 throwDirection)
    {
        Vector3 lookDirection = throwDirection;
        lookDirection.y = 0f;

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public bool IsCharging()
    {
        return isCharging;
    }

    public float GetChargePercent()
    {
        if (!isCharging)
        {
            return 0f;
        }

        float chargeTime = Time.time - chargeStartTime;
        return Mathf.Clamp01(chargeTime / maxChargeTime);
    }
}
