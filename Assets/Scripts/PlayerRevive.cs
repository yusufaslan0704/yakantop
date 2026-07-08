using UnityEngine;

public class PlayerRevive : MonoBehaviour
{
    [Header("Revive Settings")]
    public float reviveRange = 2f;
    public float reviveHoldTime = 2f;

    [Header("Revive VFX")]
    public GameObject reviveCompleteVfxPrefab;
    public Vector3 reviveVfxOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Revive Camera Shake")]
    public float reviveShakeDuration = 0.12f;
    public float reviveShakeStrength = 0.10f;

    private PlayerHealth targetToRevive;
    private float reviveProgress = 0f;

    private PlayerRole playerRole;
    private PlayerHealth playerHealth;
    private PlayerInputHandler inputHandler;

    private PlayerHealth[] allPlayers;

    void Awake()
    {
        playerRole = GetComponent<PlayerRole>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    void Start()
    {
        // Oyuncular oyun sırasında değişmediği için sahneyi bir kez tarıyoruz.
        allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // Round bittiyse revive yapılamaz.
        if (!GameManager.RoundIsActive)
        {
            ResetReviveProgress();
            return;
        }

        // Elenen oyuncu revive yapamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            ResetReviveProgress();
            return;
        }

        // Sadece Saver rolündeki oyuncu revive yapabilir.
        if (playerRole != null && playerRole.roleType != RoleType.Saver)
        {
            ResetReviveProgress();
            return;
        }

        FindReviveTarget();

        // Revive girisi: handler varsa oradan (klavye F / gamepad A), yoksa eski usul.
        bool reviveInput = inputHandler != null
            ? inputHandler.ReviveHeld
            : Input.GetKey(KeyCode.F);

        if (targetToRevive != null && reviveInput)
        {
            reviveProgress += Time.deltaTime;

            Debug.Log("Revive ilerliyor: " + reviveProgress.ToString("F1"));

            if (reviveProgress >= reviveHoldTime)
            {
                CompleteRevive();
            }
        }
        else
        {
            reviveProgress = 0f;
        }
    }

    void CompleteRevive()
    {
        if (targetToRevive == null)
        {
            ResetReviveProgress();
            return;
        }

        PlayerHealth revivedPlayer = targetToRevive;

        revivedPlayer.Revive();

        PlayReviveFeedback(revivedPlayer.transform.position);

        ResetReviveProgress();
    }

    void FindReviveTarget()
    {
        targetToRevive = null;

        if (allPlayers == null) return;

        foreach (PlayerHealth player in allPlayers)
        {
            if (player == null) continue;

            // Kendini revive etmeye çalışma.
            if (player == playerHealth) continue;

            // Sadece elenmiş oyuncular revive edilebilir.
            if (!player.IsEliminated) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= reviveRange)
            {
                targetToRevive = player;
                break;
            }
        }
    }

    void PlayReviveFeedback(Vector3 revivedPlayerPosition)
    {
        // Revive sesi.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayRevive();
        }

        // Revive kamera shake.
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(reviveShakeDuration, reviveShakeStrength);
        }

        // Revive VFX.
        SpawnReviveCompleteVFX(revivedPlayerPosition);
    }

    void SpawnReviveCompleteVFX(Vector3 revivedPlayerPosition)
    {
        if (reviveCompleteVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = revivedPlayerPosition + reviveVfxOffset;

        Instantiate(
            reviveCompleteVfxPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    void ResetReviveProgress()
    {
        reviveProgress = 0f;
        targetToRevive = null;
    }

    public float GetReviveProgressPercent()
    {
        if (reviveHoldTime <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(reviveProgress / reviveHoldTime);
    }

    public bool IsReviving()
    {
        return targetToRevive != null && reviveProgress > 0f;
    }

    public bool HasReviveTarget()
    {
        return targetToRevive != null;
    }
}