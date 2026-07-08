using System.Collections;
using System.Collections.Generic;
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

    private float currentTime;
    private float reviveCountdown;

    private bool roundActive = false;
    private bool gameEnded = false;
    private bool playerWon = false;
    private bool reviveCountdownActive = false;

    private int score = 0;

    // Skor aidiyeti: hangi oyuncu kac eleme yapti.
    // Multiplayer'da "kim kimi vurdu" tablosunun temeli.
    private readonly Dictionary<GameObject, int> scoreByPlayer = new Dictionary<GameObject, int>();

    private Coroutine hitStopRoutine;

    // Oyuncular sahne referansi yerine PlayerManager'dan gelir.
    // Round reset icin herkesin baslangic pozisyonu saklanir.
    private readonly Dictionary<PlayerRole, Vector3> startPositions = new Dictionary<PlayerRole, Vector3>();
    private readonly List<PlayerHealth> subscribedHealths = new List<PlayerHealth>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (PlayerRole player in PlayerManager.All)
        {
            startPositions[player] = player.transform.position;

            if (player.Health != null)
            {
                player.Health.OnEliminated += HandlePlayerEliminated;
                player.Health.OnRevived += HandlePlayerRevived;

                subscribedHealths.Add(player.Health);
            }
        }

        StartRound();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        foreach (PlayerHealth health in subscribedHealths)
        {
            if (health != null)
            {
                health.OnEliminated -= HandlePlayerEliminated;
                health.OnRevived -= HandlePlayerRevived;
            }
        }

        subscribedHealths.Clear();
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

    // Rol tabanli kural: herhangi bir oyuncu elendiginde genel duruma bakilir.
    // Tum Runner'lar elendiyse geri sayim baslar; Saver'lar da elendiyse oyun biter.
    void HandlePlayerEliminated()
    {
        if (gameEnded) return;

        bool anyRunnerAlive = PlayerManager.AnyAlive(RoleType.Runner);
        bool anySaverAlive = PlayerManager.AnyAlive(RoleType.Saver);

        if (!anyRunnerAlive && !anySaverAlive)
        {
            LoseGame();
            return;
        }

        if (!anyRunnerAlive && !reviveCountdownActive)
        {
            StartReviveCountdown();
        }
    }

    void HandlePlayerRevived()
    {
        if (gameEnded) return;

        // En az bir Runner hayattaysa geri sayim durur.
        if (reviveCountdownActive && PlayerManager.AnyAlive(RoleType.Runner))
        {
            StopReviveCountdown();
        }
    }

    void StartRound()
    {
        currentTime = roundDuration;
        reviveCountdown = reviveCountdownDuration;

        score = 0;
        scoreByPlayer.Clear();

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

        // Herkes round basindaki yerine doner.
        foreach (KeyValuePair<PlayerRole, Vector3> entry in startPositions)
        {
            if (entry.Key != null)
            {
                entry.Key.transform.position = entry.Value;
            }
        }

        StartRound();

        Debug.Log("Round resetlendi!");
    }

    // Skor, elemeyi yapan oyuncuya (topu atana) yazılır.
    public void AddScore(GameObject scorer, int amount = 1)
    {
        if (gameEnded) return;

        score += amount;

        if (scorer != null)
        {
            scoreByPlayer.TryGetValue(scorer, out int current);
            scoreByPlayer[scorer] = current + amount;

            Debug.Log(scorer.name + " eleme yaptı! Toplam: " + scoreByPlayer[scorer]);
        }
    }

    public int GetScore()
    {
        return score;
    }

    public int GetScoreOf(GameObject player)
    {
        if (player == null) return 0;

        scoreByPlayer.TryGetValue(player, out int value);
        return value;
    }

    // Çarpma anında oyunu kısacık dondurur ("vuruş oturdu" hissi).
    public void DoHitStop(float duration)
    {
        if (duration <= 0f) return;

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            Time.timeScale = 1f;
        }

        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        hitStopRoutine = null;
    }

    void OnDisable()
    {
        // Hit-stop ortasında oyun kapanırsa zaman donuk kalmasın.
        Time.timeScale = 1f;
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
