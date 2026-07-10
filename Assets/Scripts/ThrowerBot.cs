using UnityEngine;

public class ThrowerBot : MonoBehaviour
{
    [Header("Ball Types")]
    public BallData[] ballTypes;

    [Header("Throw Settings")]
    public float baseThrowInterval = 2.5f;
    public float throwIntervalRandomOffset = 0.35f;
    public float aimHeightOffset = 1.0f;

    [Header("Prediction Aim")]
    public bool usePrediction = true;
    public float predictionTime = 0.45f;
    public float maxPredictionDistance = 3.5f;

    [Header("Aim Error")]
    public bool useAimError = true;
    public float aimErrorRadius = 0.45f;

    [Header("Telegraph (Atış Öncesi Uyarı)")]
    [Tooltip("Atıştan önce botun kaç saniye 'yanıp sönerek' uyarı vereceği.")]
    public float telegraphDuration = 0.4f;
    public Color telegraphColor = new Color(1f, 0.85f, 0.2f);
    public float telegraphFlashSpeed = 14f;

    [Header("Decoy")]
    [Range(0f, 1f)]
    [Tooltip("Aktif decoy varken botun ona atma ihtimali.")]
    public float decoyTargetChance = 0.72f;

    private PlayerThrow playerThrow;
    private PlayerHealth playerHealth;

    // Gorsel model calisma aninda degisebildigi icin renderer'lar
    // her telegraf basinda yeniden taranir.
    private Renderer[] telegraphRenderers;
    private Color[] telegraphOriginalColors;
    private bool isTelegraphing;
    private float telegraphEndTime;

    private Transform currentTarget;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private bool lockedOntoDecoy;

    private float nextThrowTime;

    void Awake()
    {
        playerThrow = GetComponent<PlayerThrow>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        SelectTarget();

        if (currentTarget != null)
        {
            lastTargetPosition = currentTarget.position;
        }

        nextThrowTime = Time.time + 1f;
    }

    void Update()
    {
        if (playerThrow == null) return;

        // Round bittiyse bot durur.
        if (!GameManager.RoundIsActive)
        {
            CancelTelegraph(restoreColor: true);
            return;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            // Rengi geri ALMA: elenme rengi (kirmizi) ustte kalmali.
            CancelTelegraph(restoreColor: false);
            return;
        }

        Transform previousTarget = currentTarget;

        SelectTarget();

        if (currentTarget == null)
        {
            return;
        }

        if (previousTarget != currentTarget)
        {
            lastTargetPosition = currentTarget.position;
            targetVelocity = Vector3.zero;
        }

        UpdateTargetVelocity();

        if (isTelegraphing)
        {
            UpdateTelegraphFlash();

            if (Time.time >= telegraphEndTime)
            {
                CancelTelegraph(restoreColor: true);
                ThrowSelectedBall();
            }
        }
        else if (Time.time >= nextThrowTime)
        {
            // Flash: bot atisi geciktirir / isabeti bozar.
            if (PlayerFlash.AreThrowersBlinded())
            {
                nextThrowTime = Time.time + 0.35f;
                return;
            }

            StartTelegraph();
        }
    }

    void StartTelegraph()
    {
        if (telegraphDuration <= 0f)
        {
            ThrowSelectedBall();
            return;
        }

        isTelegraphing = true;
        telegraphEndTime = Time.time + telegraphDuration;

        // Aktif gorsel her ne ise (kapsul veya Mixamo modeli) onu tara.
        telegraphRenderers = GetComponentsInChildren<Renderer>();
        telegraphOriginalColors = new Color[telegraphRenderers.Length];

        for (int i = 0; i < telegraphRenderers.Length; i++)
        {
            if (telegraphRenderers[i] != null)
            {
                telegraphOriginalColors[i] = telegraphRenderers[i].material.color;
            }
        }
    }

    void UpdateTelegraphFlash()
    {
        if (telegraphRenderers == null) return;

        // 0-1 arasi gidip gelen deger ile renk yanip soner.
        float t = Mathf.PingPong(Time.time * telegraphFlashSpeed, 1f);

        for (int i = 0; i < telegraphRenderers.Length; i++)
        {
            if (telegraphRenderers[i] != null)
            {
                telegraphRenderers[i].material.color =
                    Color.Lerp(telegraphOriginalColors[i], telegraphColor, t);
            }
        }
    }

    void CancelTelegraph(bool restoreColor)
    {
        if (!isTelegraphing) return;

        isTelegraphing = false;

        if (restoreColor && telegraphRenderers != null)
        {
            for (int i = 0; i < telegraphRenderers.Length; i++)
            {
                if (telegraphRenderers[i] != null)
                {
                    telegraphRenderers[i].material.color = telegraphOriginalColors[i];
                }
            }
        }
    }

    void OnDisable()
    {
        // Bot kapatilirsa (orn. kontrol insana gecerse) uyari rengi takili kalmasin.
        bool eliminated = playerHealth != null && playerHealth.IsEliminated;
        CancelTelegraph(restoreColor: !eliminated);
    }

    void ThrowSelectedBall()
    {
        BallData selectedBall = ChooseBallData();

        if (selectedBall == null || selectedBall.prefab == null)
        {
            Debug.LogWarning("Bot top atamadı. Ball Types listesi boş veya prefab eksik.");
            ScheduleNextThrow(1f);
            return;
        }

        Vector3 aimPoint = GetAimPoint();

        playerThrow.BotThrowAt(aimPoint, selectedBall);

        ScheduleNextThrow(selectedBall.cooldownMultiplier);
    }

    void ScheduleNextThrow(float cooldownMultiplier)
    {
        float randomOffset = Random.Range(-throwIntervalRandomOffset, throwIntervalRandomOffset);
        float finalInterval = baseThrowInterval * cooldownMultiplier + randomOffset;

        finalInterval = Mathf.Max(0.5f, finalInterval);

        nextThrowTime = Time.time + finalInterval;
    }

    // Hedefler artik sahne referansi degil: PlayerManager'dan dinamik secilir.
    // Once hayattaki en yakin Runner, yoksa en yakin Saver.
    // Gorunmez hedefler atlanir. Aktif decoy varsa siklikla ona kilitlenir.
    void SelectTarget()
    {
        if (isTelegraphing && currentTarget != null)
        {
            return;
        }

        // Decoy'a kilitliyken ayakta kaldigi surece hedefi bozma.
        if (lockedOntoDecoy && currentTarget != null)
        {
            DecoyClone decoyLock = currentTarget.GetComponent<DecoyClone>();
            if (decoyLock != null)
            {
                return;
            }

            lockedOntoDecoy = false;
        }

        Transform decoy = PlayerDecoy.GetClosestDecoy(transform.position);
        if (decoy != null && Random.value <= decoyTargetChance)
        {
            currentTarget = decoy;
            lockedOntoDecoy = true;
            return;
        }

        lockedOntoDecoy = false;

        PlayerRole target = GetClosestVisibleAlive(RoleType.Runner, transform.position);

        if (target == null)
        {
            target = GetClosestVisibleAlive(RoleType.Saver, transform.position);
        }

        currentTarget = target != null ? target.transform : null;
    }

    static PlayerRole GetClosestVisibleAlive(RoleType role, Vector3 from)
    {
        PlayerRole closest = null;
        float closestSqr = float.MaxValue;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != role) continue;
            if (player.Health != null && player.Health.IsEliminated) continue;
            if (PlayerInvisibility.IsPlayerInvisible(player)) continue;

            float sqr = (player.transform.position - from).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = player;
            }
        }

        return closest;
    }

    void UpdateTargetVelocity()
    {
        if (currentTarget == null) return;
        if (Time.deltaTime <= 0f) return;

        targetVelocity = (currentTarget.position - lastTargetPosition) / Time.deltaTime;
        lastTargetPosition = currentTarget.position;
    }

    Vector3 GetAimPoint()
    {
        Vector3 baseAimPoint = currentTarget.position + Vector3.up * aimHeightOffset;
        bool blinded = PlayerFlash.AreThrowersBlinded();

        if (usePrediction && !blinded)
        {
            Vector3 predictedOffset = targetVelocity * predictionTime;

            if (predictedOffset.magnitude > maxPredictionDistance)
            {
                predictedOffset = predictedOffset.normalized * maxPredictionDistance;
            }

            baseAimPoint += predictedOffset;
        }

        if (useAimError || blinded)
        {
            float error = aimErrorRadius;
            if (blinded)
            {
                error = Mathf.Max(error, 4.5f);
            }

            Vector2 randomCircle = Random.insideUnitCircle * error;
            Vector3 aimError = new Vector3(randomCircle.x, Random.Range(-0.4f, 0.8f), randomCircle.y);

            baseAimPoint += aimError;
        }

        return baseAimPoint;
    }

    // Şans değerlerine göre ağırlıklı rastgele top seçimi.
    BallData ChooseBallData()
    {
        if (ballTypes == null || ballTypes.Length == 0)
        {
            return null;
        }

        float totalChance = 0f;

        foreach (BallData ball in ballTypes)
        {
            if (ball != null)
            {
                totalChance += ball.chance;
            }
        }

        if (totalChance <= 0f)
        {
            return ballTypes[0];
        }

        float randomValue = Random.Range(0f, totalChance);
        float cumulative = 0f;

        foreach (BallData ball in ballTypes)
        {
            if (ball == null) continue;

            cumulative += ball.chance;

            if (randomValue <= cumulative)
            {
                return ball;
            }
        }

        return ballTypes[ballTypes.Length - 1];
    }
}
