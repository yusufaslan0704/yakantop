using UnityEngine;

public class ThrowerBot : MonoBehaviour
{
    [Header("Targets")]
    public Transform runnerTarget;
    public Transform saverTarget;

    [Header("Ball Prefabs")]
    public GameObject normalBallPrefab;
    public GameObject fastBallPrefab;
    public GameObject heavyBallPrefab;

    [Header("Throw Settings")]
    public float baseThrowInterval = 2.5f;
    public float throwIntervalRandomOffset = 0.35f;
    public float aimHeightOffset = 1.0f;

    [Header("Ball Forces")]
    public float normalBallForce = 28f;
    public float fastBallForce = 40f;
    public float heavyBallForce = 22f;

    [Header("Ball Cooldown Multipliers")]
    public float normalCooldownMultiplier = 1.0f;
    public float fastCooldownMultiplier = 1.2f;
    public float heavyCooldownMultiplier = 1.45f;

    [Header("Ball Chances")]
    [Range(0f, 100f)] public float normalBallChance = 60f;
    [Range(0f, 100f)] public float fastBallChance = 25f;
    [Range(0f, 100f)] public float heavyBallChance = 15f;

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
        Vector3 aimPoint = GetAimPoint();

        GameObject selectedBallPrefab = ChooseBallPrefab(
            out float selectedForce,
            out float selectedCooldownMultiplier,
            out string selectedBallName
        );

        if (selectedBallPrefab == null)
        {
            Debug.LogWarning("Bot top atamadı. Seçilen top prefab boş: " + selectedBallName);
            ScheduleNextThrow(1f);
            return;
        }

        playerThrow.BotThrowAt(aimPoint, selectedForce, selectedBallPrefab);

        Debug.Log("Bot top attı: " + selectedBallName);

        ScheduleNextThrow(selectedCooldownMultiplier);
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

    GameObject ChooseBallPrefab(
        out float selectedForce,
        out float selectedCooldownMultiplier,
        out string selectedBallName
    )
    {
        float totalChance = normalBallChance + fastBallChance + heavyBallChance;

        if (totalChance <= 0f)
        {
            selectedForce = normalBallForce;
            selectedCooldownMultiplier = normalCooldownMultiplier;
            selectedBallName = "Normal Ball";
            return normalBallPrefab;
        }

        float randomValue = Random.Range(0f, totalChance);

        if (randomValue <= normalBallChance)
        {
            selectedForce = normalBallForce;
            selectedCooldownMultiplier = normalCooldownMultiplier;
            selectedBallName = "Normal Ball";
            return normalBallPrefab;
        }

        if (randomValue <= normalBallChance + fastBallChance)
        {
            selectedForce = fastBallForce;
            selectedCooldownMultiplier = fastCooldownMultiplier;
            selectedBallName = "Fast Ball";
            return fastBallPrefab;
        }

        selectedForce = heavyBallForce;
        selectedCooldownMultiplier = heavyCooldownMultiplier;
        selectedBallName = "Heavy Ball";
        return heavyBallPrefab;
    }
}