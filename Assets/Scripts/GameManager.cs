using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Round Settings")]
    public float roundDuration = 60f;
    public float reviveCountdownDuration = 10f;

    [Header("Players")]
    public GameObject runnerPlayer;
    public GameObject saverPlayer;

    private float currentTime;
    private float reviveCountdown;

    private bool roundActive = false;
    private bool gameEnded = false;
    private bool playerWon = false;
    private bool reviveCountdownActive = false;

    private int score = 0;

    private PlayerHealth runnerHealth;
    private PlayerHealth saverHealth;

    private Vector3 runnerStartPosition;
    private Vector3 saverStartPosition;

    void Start()
    {
        if (runnerPlayer != null)
        {
            runnerHealth = runnerPlayer.GetComponent<PlayerHealth>();
            runnerStartPosition = runnerPlayer.transform.position;
        }

        if (saverPlayer != null)
        {
            saverHealth = saverPlayer.GetComponent<PlayerHealth>();
            saverStartPosition = saverPlayer.transform.position;
        }

        StartRound();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetRound();
            return;
        }

        if (!roundActive || gameEnded)
        {
            return;
        }

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            WinGame();
            return;
        }

        CheckReviveState();
    }

    void CheckReviveState()
    {
        if (runnerHealth == null) return;

        if (runnerHealth.isEliminated && !reviveCountdownActive)
        {
            StartReviveCountdown();
        }

        if (!runnerHealth.isEliminated && reviveCountdownActive)
        {
            StopReviveCountdown();
        }

        if (runnerHealth.isEliminated &&
            saverHealth != null &&
            saverHealth.isEliminated)
        {
            LoseGame();
            return;
        }

        if (reviveCountdownActive)
        {
            reviveCountdown -= Time.deltaTime;

            if (reviveCountdown <= 0f)
            {
                LoseGame();
            }
        }
    }

    void StartRound()
    {
        Time.timeScale = 1f;

        currentTime = roundDuration;
        reviveCountdown = reviveCountdownDuration;

        score = 0;
        roundActive = true;
        gameEnded = false;
        playerWon = false;
        reviveCountdownActive = false;

        Debug.Log("Round başladı!");
    }

    void StartReviveCountdown()
    {
        reviveCountdownActive = true;
        reviveCountdown = reviveCountdownDuration;

        Debug.Log("Runner elendi! Revive süresi başladı.");
    }

    void StopReviveCountdown()
    {
        reviveCountdownActive = false;
        reviveCountdown = reviveCountdownDuration;

        Debug.Log("Runner kurtarıldı! Oyun devam ediyor.");
    }

    void WinGame()
    {
        currentTime = 0f;
        roundActive = false;
        gameEnded = true;
        playerWon = true;

        Debug.Log("Kazandın!");
        Time.timeScale = 0f;
    }

    void LoseGame()
    {
        roundActive = false;
        gameEnded = true;
        playerWon = false;

        Debug.Log("Kaybettin!");
        Time.timeScale = 0f;
    }

    void ResetRound()
    {
        Time.timeScale = 1f;

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (PlayerHealth player in players)
        {
            player.Revive();
        }

        if (runnerPlayer != null)
        {
            runnerPlayer.transform.position = runnerStartPosition;
        }

        if (saverPlayer != null)
        {
            saverPlayer.transform.position = saverStartPosition;
        }

        StartRound();

        Debug.Log("Round resetlendi!");
    }

    public void AddScore(int amount)
    {
        if (gameEnded) return;

        score += amount;
    }

    public int GetScore()
    {
        return score;
    }

    public bool IsRoundActive()
    {
        return roundActive;
    }

    public bool IsGameEnded()
    {
        return gameEnded;
    }

    public bool DidPlayerWin()
    {
        return playerWon;
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public bool IsReviveCountdownActive()
    {
        return reviveCountdownActive;
    }

    public float GetReviveCountdown()
    {
        return reviveCountdown;
    }
}
