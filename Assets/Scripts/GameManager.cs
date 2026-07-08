using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Diğer scriptler "oyun oynanabilir durumda mı?" sorusunu buradan sorar.
    public static bool RoundIsActive
    {
        get { return Instance == null || Instance.roundActive; }
    }

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

    void Awake()
    {
        Instance = this;
    }

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

        if (runnerHealth != null)
        {
            runnerHealth.OnEliminated += HandleRunnerEliminated;
            runnerHealth.OnRevived += HandleRunnerRevived;
        }

        if (saverHealth != null)
        {
            saverHealth.OnEliminated += HandleSaverEliminated;
        }

        StartRound();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (runnerHealth != null)
        {
            runnerHealth.OnEliminated -= HandleRunnerEliminated;
            runnerHealth.OnRevived -= HandleRunnerRevived;
        }

        if (saverHealth != null)
        {
            saverHealth.OnEliminated -= HandleSaverEliminated;
        }
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

        // Süreye bağlı tek iş: revive geri sayımı.
        if (reviveCountdownActive)
        {
            reviveCountdown -= Time.deltaTime;

            if (reviveCountdown <= 0f)
            {
                LoseGame();
            }
        }
    }

    void HandleRunnerEliminated()
    {
        if (gameEnded) return;

        // İkisi de elendiyse oyun biter, geri sayıma gerek yok.
        if (saverHealth != null && saverHealth.IsEliminated)
        {
            LoseGame();
            return;
        }

        StartReviveCountdown();
    }

    void HandleRunnerRevived()
    {
        if (gameEnded) return;

        StopReviveCountdown();
    }

    void HandleSaverEliminated()
    {
        if (gameEnded) return;

        if (runnerHealth != null && runnerHealth.IsEliminated)
        {
            LoseGame();
        }
    }

    void StartRound()
    {
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
    }

    void LoseGame()
    {
        roundActive = false;
        gameEnded = true;
        playerWon = false;

        Debug.Log("Kaybettin!");
    }

    void ResetRound()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (PlayerHealth player in players)
        {
            player.Revive();
        }

        // Havada kalan toplar yeni round'a taşınmasın.
        Ball[] balls = FindObjectsByType<Ball>(FindObjectsSortMode.None);

        foreach (Ball ball in balls)
        {
            Destroy(ball.gameObject);
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
