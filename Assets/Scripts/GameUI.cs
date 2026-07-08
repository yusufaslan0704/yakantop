using TMPro;
using UnityEngine;

// Oyun durumunu (GameManager) okuyup ekrana çizen tek yer burası.
// GameManager UI'ı bilmez; GameUI oyun mantığını değiştirmez.
public class GameUI : MonoBehaviour
{
    public GameManager gameManager;

    [Header("HUD")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text reviveCountdownText;

    [Header("End Game")]
    public GameObject endGamePanel;
    public TMP_Text statusText;

    void Update()
    {
        if (gameManager == null) return;

        UpdateHud();
        UpdateReviveCountdown();
        UpdateEndGamePanel();
    }

    void UpdateHud()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(gameManager.GetCurrentTime());
            timerText.text = seconds.ToString();
        }

        if (scoreText != null)
        {
            scoreText.text = "Skor: " + gameManager.GetScore();
        }
    }

    void UpdateReviveCountdown()
    {
        if (reviveCountdownText == null) return;

        bool active = gameManager.IsReviveCountdownActive() && !gameManager.IsGameEnded();

        reviveCountdownText.gameObject.SetActive(active);

        if (active)
        {
            int seconds = Mathf.CeilToInt(gameManager.GetReviveCountdown());
            reviveCountdownText.text = "Runner'ı kurtar: " + seconds;
        }
    }

    void UpdateEndGamePanel()
    {
        bool ended = gameManager.IsGameEnded();

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(ended);
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(ended);

            if (ended)
            {
                statusText.text = gameManager.DidPlayerWin() ? "KAZANDIN!" : "KAYBETTİN!";
            }
        }
    }
}
