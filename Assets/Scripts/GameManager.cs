using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Round Settings")]
    public float roundDuration = 60f;
    public float reviveCountdownDuration = 10f;

    [Header("Players")]
    public GameObject runnerPlayer;
    public GameObject saverPlayer;

    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text statusText;
    public TMP_Text reviveCountdownText;
    public GameObject endGamePanel;

    private float currentTime;
    private float reviveCountdown;

    private bool roundActive = false;
    private bool gameEnded = false;
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
            UpdateUI();
            return;
        }

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            WinGame();
            return;
        }

        CheckReviveState();

        UpdateUI();
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
        reviveCountdownActive = false;

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        if (reviveCountdownText != null)
        {
            reviveCountdownText.gameObject.SetActive(false);
        }

        Debug.Log("Round başladı!");
        UpdateUI();
    }

    void StartReviveCountdown()
    {
        reviveCountdownActive = true;
        reviveCountdown = reviveCountdownDuration;

        if (reviveCountdownText != null)
        {
            reviveCountdownText.gameObject.SetActive(true);
        }

        Debug.Log("Runner elendi! Revive süresi başladı.");
    }

    void StopReviveCountdown()
    {
        reviveCountdownActive = false;
        reviveCountdown = reviveCountdownDuration;

        if (reviveCountdownText != null)
        {
            reviveCountdownText.gameObject.SetActive(false);
        }

        Debug.Log("Runner kurtarıldı! Oyun devam ediyor.");
    }

    void WinGame()
    {
        currentTime = 0f;
        roundActive = false;
        gameEnded = true;

        ShowEndGamePanel("KAZANDIN!");

        if (reviveCountdownText != null)
        {
            reviveCountdownText.gameObject.SetActive(false);
        }

        Debug.Log("Kazandın!");
        Time.timeScale = 0f;
    }

    void LoseGame()
    {
        roundActive = false;
        gameEnded = true;

        ShowEndGamePanel("KAYBETTİN!");

        if (reviveCountdownText != null)
        {
            reviveCountdownText.gameObject.SetActive(false);
        }

        Debug.Log("Kaybettin!");
        Time.timeScale = 0f;
    }

    void ShowEndGamePanel(string message)
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
        }
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

    void UpdateUI()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTime);
            timerText.text = seconds.ToString();
        }

        if (scoreText != null)
        {
            scoreText.text = "Skor: " + score;
        }

        if (reviveCountdownText != null && reviveCountdownActive)
        {
            int seconds = Mathf.CeilToInt(reviveCountdown);
            reviveCountdownText.text = "Runner'ı kurtar: " + seconds;
        }
    }

    public void AddScore(int amount)
    {
        if (gameEnded) return;

        score += amount;
        Debug.Log("Skor arttı: " + score);
        UpdateUI();
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

    public float GetCurrentTime()
    {
        return currentTime;
    }
}