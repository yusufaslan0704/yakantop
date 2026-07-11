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
    private SaverBot saverBot;

    void Awake()
    {
        playerRole = GetComponent<PlayerRole>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        saverBot = GetComponent<SaverBot>();
    }

    void Start()
    {
        // Hedefler PlayerManager'dan dinamik okunur.
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

        // Revive girisi: insan (F / gamepad) veya SaverBot hold.
        bool reviveInput = false;

        if (saverBot != null && saverBot.IsHoldingRevive())
        {
            reviveInput = true;
        }
        else if (inputHandler != null)
        {
            reviveInput = inputHandler.ReviveHeld;
        }
        else
        {
            reviveInput = Input.GetKey(KeyCode.F);
        }

        if (targetToRevive != null && reviveInput)
        {
            reviveProgress += Time.deltaTime;

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
        KillfeedUI.PushRevive(gameObject, revivedPlayer.gameObject);

        PlayReviveFeedback(revivedPlayer.transform.position);

        ResetReviveProgress();
    }

    void FindReviveTarget()
    {
        targetToRevive = null;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null || player.Health == null) continue;
            if (player.Health == playerHealth) continue;
            if (!player.Health.IsEliminated) continue;
            // Decoy asla revive hedefi degil (PlayerHealth olmasa da guvenlik).
            if (DecoyClone.IsDecoy(player)) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= reviveRange)
            {
                targetToRevive = player.Health;
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