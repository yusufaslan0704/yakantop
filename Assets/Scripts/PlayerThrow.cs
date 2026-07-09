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
    public float minThrowForce = 16f;
    public float maxThrowForce = 42f;
    public float maxChargeTime = 0.95f;

    [Header("Throw Cooldown")]
    public float cooldown = 0.42f;

    [Header("Throw Feel")]
    public float throwShakeDuration = 0.06f;
    public float throwShakeStrength = 0.07f;
    public float throwFovPunch = 2.8f;

    [Header("Animation Sync")]
    [Tooltip("Atis animasyonu basladiktan sonra topun cikacagi sure (sn). " +
             "Animasyon bu surede release pozuna yetisecek sekilde hizlanir.")]
    public float ballReleaseDelay = 0.38f;

    // Animasyon baslar baslamaz; top henuz cikmamistir.
    public event System.Action OnThrowStarted;
    // Top gercekten spawn oldugunda.
    public event System.Action OnBallThrown;

    private float chargeStartTime;
    private float nextThrowTime = 0f;
    private bool isCharging = false;
    private bool isThrowInProgress = false;
    private Coroutine pendingThrowRoutine;

    private PlayerHealth playerHealth;
    private PlayerRole playerRole;
    private PlayerInputHandler inputHandler;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerRole = GetComponent<PlayerRole>();
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        // Round bittiyse atış yapılamaz.
        if (!GameManager.RoundIsActive)
        {
            isCharging = false;
            CancelPendingThrow();
            return;
        }

        // Elenen oyuncu top atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            isCharging = false;
            CancelPendingThrow();
            return;
        }

        // Mouse ile sadece Thrower rolündeki oyuncu top atabilir.
        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            isCharging = false;
            return;
        }

        // Cooldown / devam eden atis bitmeden yeni atis hazirlanamaz.
        if (Time.time < nextThrowTime || isThrowInProgress)
        {
            return;
        }

        // Atis girisi: handler varsa oradan (sol tik / gamepad RT), yoksa eski usul fare.
        bool chargeStarted = inputHandler != null
            ? inputHandler.ThrowPressed
            : Input.GetMouseButtonDown(0);

        bool chargeReleased = inputHandler != null
            ? inputHandler.ThrowReleased
            : Input.GetMouseButtonUp(0);

        if (chargeStarted)
        {
            StartCharge();
        }

        if (chargeReleased && isCharging)
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

        BeginThrow(ballData, GetThrowDirection(), finalForce, rotateToAim: true);
    }

    void ThrowBall(float force)
    {
        BeginThrow(ballData, GetThrowDirection(), force, rotateToAim: true);
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
        if (Time.time < nextThrowTime || isThrowInProgress)
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
        BeginThrow(botBallData, throwDirection, botBallData.throwForce, rotateToAim: true);
    }

    void BeginThrow(BallData dataToThrow, Vector3 throwDirection, float force, bool rotateToAim)
    {
        if (dataToThrow == null || dataToThrow.prefab == null)
        {
            Debug.LogWarning("Ball Data atanmadı veya prefab eksik!");
            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning("Throw Point atanmadı!");
            return;
        }

        if (throwDirection == Vector3.zero)
        {
            return;
        }

        if (rotateToAim)
        {
            RotatePlayerToThrowDirection(throwDirection);
        }

        // Animasyon hemen baslar; top release aninda cikar.
        CancelPendingThrow();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(cooldown, ballReleaseDelay);
        OnThrowStarted?.Invoke();

        pendingThrowRoutine = StartCoroutine(ReleaseBallAfterDelay(dataToThrow, throwDirection, force));
    }

    System.Collections.IEnumerator ReleaseBallAfterDelay(BallData dataToThrow, Vector3 throwDirection, float force)
    {
        float delay = Mathf.Max(0f, ballReleaseDelay);

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        pendingThrowRoutine = null;

        if (!GameManager.RoundIsActive ||
            (playerHealth != null && playerHealth.IsEliminated))
        {
            isThrowInProgress = false;
            yield break;
        }

        SpawnAndThrowBall(dataToThrow, throwDirection, force);
        isThrowInProgress = false;
    }

    void CancelPendingThrow()
    {
        if (pendingThrowRoutine != null)
        {
            StopCoroutine(pendingThrowRoutine);
            pendingThrowRoutine = null;
        }

        isThrowInProgress = false;
    }

    void OnDisable()
    {
        CancelPendingThrow();
        isCharging = false;
    }

    Vector3 GetThrowDirection()
    {
        // Gamepad oyuncusu fareyle nisan alamaz: karakterin baktigi yone atar.
        bool useForwardAim = inputHandler != null &&
                             inputHandler.scheme == ControlScheme.Gamepad;

        if (useForwardAim || playerCamera == null)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward == Vector3.zero)
            {
                return Vector3.zero;
            }

            // Hafif yukari egim: top yercekimiyle dusmeden hedefe ulassin.
            return (forward.normalized + Vector3.up * 0.08f).normalized;
        }

        // Klavye+fare: ekranin ortasindaki crosshair'e dogru atis.
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

        return (targetPoint - throwPoint.position).normalized;
    }

    void SpawnAndThrowBall(BallData dataToThrow, Vector3 throwDirection, float force)
    {
        // Topu karakterin gövdesinin içinde değil, biraz önünde oluşturuyoruz.
        Vector3 spawnPosition = throwPoint.position + throwDirection * 0.6f;

        GameObject ball = Instantiate(dataToThrow.prefab, spawnPosition, Quaternion.identity);
        SceneFolders.ParentTo(ball.transform, SceneFolders.RuntimeSpawned);

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
        CameraShake.ShakeAll(throwShakeDuration, throwShakeStrength);
        CameraShake.PunchFovAll(throwFovPunch, 0.12f);
        OnBallThrown?.Invoke();
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
