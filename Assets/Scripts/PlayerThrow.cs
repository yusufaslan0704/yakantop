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

        // Emote cemberi acikken atis baslatma.
        if (EmoteWheelUI.IsOpen)
        {
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

        pendingThrowRoutine = StartCoroutine(ReleaseBallAfterDelay(dataToThrow, throwDirection, force, null));
    }

    // Yetenek atisi (Trap vb.): tek top + loft + spawn sonrasi ayar.
    public bool TryThrowAbilityBall(
        BallData dataToThrow,
        float force,
        float loft,
        System.Action<Ball> configureBall)
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (Time.time < nextThrowTime || isThrowInProgress) return false;
        if (dataToThrow == null || dataToThrow.prefab == null || throwPoint == null) return false;

        Vector3 throwDirection = GetThrowDirection();
        if (throwDirection == Vector3.zero) return false;

        if (loft > 0f)
        {
            throwDirection = (throwDirection + Vector3.up * loft).normalized;
        }

        RotatePlayerToThrowDirection(throwDirection);

        isCharging = false;
        CancelPendingThrow();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(cooldown, 0.35f);
        OnThrowStarted?.Invoke();

        pendingThrowRoutine = StartCoroutine(
            ReleaseBallAfterDelay(dataToThrow, throwDirection, force, configureBall));
        return true;
    }

    System.Collections.IEnumerator ReleaseBallAfterDelay(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        System.Action<Ball> configureBall)
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

        GameObject ballGo = SpawnAndThrowBall(
            dataToThrow, throwDirection, force, playFeedback: true, spawnLateralOffset: Vector3.zero);

        if (configureBall != null && ballGo != null)
        {
            Ball ball = ballGo.GetComponent<Ball>();
            if (ball != null)
            {
                configureBall(ball);
            }
        }

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
        // Gamepad: karakterin baktigi yon.
        bool useForwardAim = inputHandler != null &&
                             inputHandler.scheme == ControlScheme.Gamepad;

        if (useForwardAim || playerCamera == null)
        {
            return GetBodyForwardAim();
        }

        return GetCameraAimDirection();
    }

    Vector3 GetBodyForwardAim()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        // Hafif yukari egim: top hemen yere gomulmesin.
        return (forward.normalized + Vector3.up * 0.12f).normalized;
    }

    Vector3 GetCameraAimDirection()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Varsayilan: kameranin baktigi uzak nokta (yakin zemin isabetini kullanma).
        Vector3 targetPoint = ray.GetPoint(Mathf.Min(aimDistance, 45f));

        RaycastHit[] hits = Physics.RaycastAll(ray, aimDistance, ~0, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null) continue;

            // Kendi collider / cok yakin isabet (ayak alti zemin) atisi asagi ceker.
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < 3f)
            {
                continue;
            }

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                targetPoint = hit.point;
            }
        }

        if (PlayerFlash.AreThrowersBlinded())
        {
            targetPoint += Random.insideUnitSphere * 3.5f;
        }

        Vector3 dir = targetPoint - throwPoint.position;
        if (dir.sqrMagnitude < 0.01f)
        {
            return GetCameraForwardAim();
        }

        // Nisan neredeyse geriye veya dimdik asagiysa kamera ileri yonunu kullan.
        Vector3 flat = dir;
        flat.y = 0f;
        Vector3 bodyFlat = transform.forward;
        bodyFlat.y = 0f;

        if (flat.sqrMagnitude < 0.35f ||
            (bodyFlat.sqrMagnitude > 0.01f && Vector3.Dot(flat.normalized, bodyFlat.normalized) < 0.15f))
        {
            return GetCameraForwardAim();
        }

        // Asiri egimi yumusat.
        Vector3 n = dir.normalized;
        if (n.y < -0.25f)
        {
            n.y = -0.25f;
            n.Normalize();
        }

        return n;
    }

    Vector3 GetCameraForwardAim()
    {
        Vector3 forward = playerCamera.transform.forward;
        forward.y = Mathf.Clamp(forward.y, -0.15f, 0.4f);

        if (forward.sqrMagnitude < 0.0001f)
        {
            return GetBodyForwardAim();
        }

        return forward.normalized;
    }

    // Yetenek: nisan etrafinda yayilan coklu top (orn. 20° / 3 top).
    // overrideBall: verilirse o tip kullanilir (volley = Fast).
    // loft: ekstra yukari egim (menzil).
    public bool TryThrowSpread(
        float totalSpreadDegrees,
        int count,
        float force,
        BallData overrideBall = null,
        float loft = 0f)
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (Time.time < nextThrowTime || isThrowInProgress) return false;

        BallData dataToThrow = overrideBall != null ? overrideBall : ballData;
        if (dataToThrow == null || dataToThrow.prefab == null || throwPoint == null) return false;

        Vector3 centerDir = GetThrowDirection();
        if (centerDir == Vector3.zero) return false;

        RotatePlayerToThrowDirection(centerDir);

        isCharging = false;
        CancelPendingThrow();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(cooldown, 0.35f);
        OnThrowStarted?.Invoke();

        pendingThrowRoutine = StartCoroutine(
            ReleaseSpreadAfterDelay(dataToThrow, centerDir, force, totalSpreadDegrees, count, loft));
        return true;
    }

    System.Collections.IEnumerator ReleaseSpreadAfterDelay(
        BallData dataToThrow,
        Vector3 centerDir,
        float force,
        float totalSpreadDegrees,
        int count,
        float loft)
    {
        float delay = Mathf.Min(0.18f, Mathf.Max(0f, ballReleaseDelay * 0.45f));
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

        count = Mathf.Max(1, count);
        float half = totalSpreadDegrees * 0.5f;

        // Yatay yayilim: up ekseni etrafinda -half .. +half
        Vector3 flat = centerDir;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = transform.forward;
            flat.y = 0f;
        }

        flat.Normalize();
        float pitchY = centerDir.y + loft;

        // Ayni volley'deki toplar birbirine carpmasin / birbirini yok etmesin.
        Collider[] spawnedColliders = new Collider[count];

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float yaw = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * flat;
            dir = (dir + Vector3.up * pitchY).normalized;

            Vector3 lateral = Vector3.Cross(Vector3.up, flat).normalized;
            // Spawn araligi: dar acida bile collider overlap olmasin.
            Vector3 spawnOffset = lateral * Mathf.Lerp(-0.55f, 0.55f, t);

            GameObject ball = SpawnAndThrowBall(
                dataToThrow, dir, force, playFeedback: i == 0, spawnLateralOffset: spawnOffset);

            if (ball != null)
            {
                spawnedColliders[i] = ball.GetComponent<Collider>();
            }
        }

        IgnoreCollisionsBetween(spawnedColliders);

        isThrowInProgress = false;
    }

    static void IgnoreCollisionsBetween(Collider[] colliders)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;

            for (int j = i + 1; j < colliders.Length; j++)
            {
                if (colliders[j] == null) continue;
                Physics.IgnoreCollision(colliders[i], colliders[j], true);
            }
        }
    }

    void SpawnAndThrowBall(BallData dataToThrow, Vector3 throwDirection, float force)
    {
        SpawnAndThrowBall(dataToThrow, throwDirection, force, playFeedback: true, spawnLateralOffset: Vector3.zero);
    }

    GameObject SpawnAndThrowBall(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        bool playFeedback,
        Vector3 spawnLateralOffset)
    {
        Vector3 spawnPosition = throwPoint.position + throwDirection * 0.6f + spawnLateralOffset;

        GameObject ball = Instantiate(dataToThrow.prefab, spawnPosition, Quaternion.identity);
        SceneFolders.ParentTo(ball.transform, SceneFolders.RuntimeSpawned);

        Color trailColor = CombatVfx.ReadBallColor(ball, Color.white);
        CombatVfx.TintBallTrail(ball, trailColor);

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

        if (playFeedback)
        {
            PlayThrowSound(dataToThrow);
            CameraShake.ShakeAll(throwShakeDuration, throwShakeStrength);
            CameraShake.PunchFovAll(throwFovPunch, 0.12f);
            OnBallThrown?.Invoke();
        }

        return ball;
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
