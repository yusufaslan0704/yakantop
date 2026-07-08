using UnityEngine;

public class ThrowerBot : MonoBehaviour
{
    [Header("Targets")]
    public Transform runnerTarget;
    public Transform saverTarget;

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

    private PlayerThrow playerThrow;
    private PlayerHealth playerHealth;

    private PlayerHealth runnerHealth;
    private PlayerHealth saverHealth;

    private Transform currentTarget;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;

    private float nextThrowTime;

    void Awake()
    {
        playerThrow = GetComponent<PlayerThrow>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        if (runnerTarget != null)
        {
            runnerHealth = runnerTarget.GetComponent<PlayerHealth>();
        }

        if (saverTarget != null)
        {
            saverHealth = saverTarget.GetComponent<PlayerHealth>();
        }

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
            return;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
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

            Debug.Log("Bot hedef değiştirdi: " + currentTarget.name);
        }

        UpdateTargetVelocity();

        if (Time.time >= nextThrowTime)
        {
            ThrowSelectedBall();
        }
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

    void SelectTarget()
    {
        bool runnerAvailable = runnerTarget != null &&
                               runnerHealth != null &&
                               !runnerHealth.IsEliminated;

        bool saverAvailable = saverTarget != null &&
                              saverHealth != null &&
                              !saverHealth.IsEliminated;

        if (runnerAvailable)
        {
            currentTarget = runnerTarget;
            return;
        }

        if (saverAvailable)
        {
            currentTarget = saverTarget;
            return;
        }

        currentTarget = null;
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

        if (usePrediction)
        {
            Vector3 predictedOffset = targetVelocity * predictionTime;

            if (predictedOffset.magnitude > maxPredictionDistance)
            {
                predictedOffset = predictedOffset.normalized * maxPredictionDistance;
            }

            baseAimPoint += predictedOffset;
        }

        if (useAimError)
        {
            Vector2 randomCircle = Random.insideUnitCircle * aimErrorRadius;
            Vector3 aimError = new Vector3(randomCircle.x, 0f, randomCircle.y);

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
