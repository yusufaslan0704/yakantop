using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Optional")]
    public TMP_Text crosshairText;

    bool stylesApplied;
    Image endGameBackdrop;
    Image endGameCard;
    TMP_Text restartTextLegacy;

    void Awake()
    {
        EnsureCanvasScaler();
        ApplyStyles();
    }

    void Update()
    {
        if (gameManager == null) return;

        if (!stylesApplied)
        {
            ApplyStyles();
        }

        if (gameManager.IsInLobby())
        {
            HideHud();
            return;
        }

        UpdateHud();
        UpdateReviveCountdown();
        UpdateEndGamePanel();
    }

    void HideHud()
    {
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (reviveCountdownText != null) reviveCountdownText.gameObject.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (crosshairText != null) crosshairText.gameObject.SetActive(false);
    }

    void UpdateHud()
    {
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (crosshairText != null) crosshairText.gameObject.SetActive(true);

        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(gameManager.GetCurrentTime());
            timerText.text = seconds.ToString();

            Color target = seconds <= 10f ? UIColorPalette.Danger : UIColorPalette.Title;
            if (seconds <= 5)
            {
                float pulse = 0.7f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
                target.a = pulse;
            }

            timerText.color = Color.Lerp(timerText.color, target, 12f * Time.deltaTime);
        }

        if (scoreText != null)
        {
            int runners = gameManager.GetRunnerTeamWins();
            int throwers = gameManager.GetThrowerTeamWins();
            scoreText.text = "<color=#8CA6BF>MAÇ</color>  <color=#73E6FF>" + runners +
                             "</color><color=#8CA6BF> - </color><color=#73E6FF>" + throwers + "</color>";
        }
    }

    void UpdateReviveCountdown()
    {
        if (reviveCountdownText == null) return;

        bool active = gameManager.IsReviveCountdownActive() && !gameManager.IsGameEnded();
        reviveCountdownText.gameObject.SetActive(active);

        if (!active) return;

        int seconds = Mathf.CeilToInt(gameManager.GetReviveCountdown());
        reviveCountdownText.text = "Runner'ı kurtar: " + seconds;

        Color c = seconds <= 5 ? UIColorPalette.Danger : UIColorPalette.Warning;
        if (seconds <= 5)
        {
            float pulse = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f));
            c.a = pulse;
        }

        reviveCountdownText.color = c;
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
                statusText.text = BuildEndText();
                statusText.color = gameManager.DidPlayerWin()
                    ? UIColorPalette.Success
                    : UIColorPalette.Danger;
            }
        }
    }

    string BuildEndText()
    {
        string matchScore = gameManager.GetRunnerTeamWins() + " - " + gameManager.GetThrowerTeamWins();

        if (gameManager.IsMatchEnded())
        {
            string winner = gameManager.DidPlayerWin() ? "KAÇANLAR" : "ATICILAR";
            return "MAÇI " + winner + " KAZANDI!\n" + matchScore + "\n<size=70%><color=#8CA6BF>Yeni maç: R  |  Lobiye dön: L</color></size>";
        }

        string roundResult = gameManager.DidPlayerWin() ? "ROUND KAÇANLARIN!" : "ROUND ATICILARIN!";
        int seconds = Mathf.CeilToInt(gameManager.GetIntermissionRemaining());

        return roundResult + "\n" + matchScore +
               "\n<size=70%><color=#8CA6BF>Yeni round: " + Mathf.Max(0, seconds) + "</color></size>";
    }

    void EnsureCanvasScaler()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    void ApplyStyles()
    {
        StyleText(timerText, 56f, FontStyles.Bold, UIColorPalette.Title);
        StyleText(scoreText, 28f, FontStyles.Bold, UIColorPalette.Body);
        StyleText(reviveCountdownText, 30f, FontStyles.Bold, UIColorPalette.Warning);
        StyleText(statusText, 42f, FontStyles.Bold, UIColorPalette.Title);

        if (crosshairText == null)
        {
            Transform found = transform.Find("Crosshair");
            if (found != null)
            {
                crosshairText = found.GetComponent<TMP_Text>();
            }
        }

        StyleText(crosshairText, 34f, FontStyles.Bold, UIColorPalette.Crosshair);

        StyleEndGamePanel();
        stylesApplied = true;
    }

    void StyleText(TMP_Text text, float size, FontStyles style, Color color)
    {
        if (text == null) return;

        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.richText = true;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        // Bazi TMP fontlari outline desteklemez; NRE olmasin.
        if (text.fontSharedMaterial != null)
        {
            text.outlineWidth = 0.22f;
            text.outlineColor = UIColorPalette.Outline;
        }
    }

    void StyleEndGamePanel()
    {
        if (endGamePanel == null) return;

        RectTransform panelRect = endGamePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        endGameBackdrop = endGamePanel.GetComponent<Image>();
        if (endGameBackdrop != null)
        {
            endGameBackdrop.enabled = true;
            endGameBackdrop.color = new Color(0.02f, 0.04f, 0.08f, 0.72f);
            endGameBackdrop.raycastTarget = false;
        }

        // Eski RestartText sahne metni GameUI ile celisiyor; gizle.
        Transform restart = endGamePanel.transform.Find("RestartText");
        if (restart != null)
        {
            restartTextLegacy = restart.GetComponent<TMP_Text>();
            restart.gameObject.SetActive(false);
        }

        EnsureEndGameCard();

        if (statusText != null)
        {
            RectTransform statusRect = statusText.rectTransform;
            statusRect.SetParent(endGameCard != null ? endGameCard.rectTransform : endGamePanel.transform, false);
            statusRect.anchorMin = new Vector2(0.5f, 0.5f);
            statusRect.anchorMax = new Vector2(0.5f, 0.5f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.anchoredPosition = Vector2.zero;
            statusRect.sizeDelta = new Vector2(720f, 220f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 28f;
            statusText.fontSizeMax = 48f;
        }
    }

    void EnsureEndGameCard()
    {
        Transform existing = endGamePanel.transform.Find("EndGameCard");
        if (existing != null)
        {
            endGameCard = existing.GetComponent<Image>();
            return;
        }

        GameObject card = new GameObject("EndGameCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(endGamePanel.transform, false);
        card.transform.SetAsFirstSibling();

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 280f);
        cardRect.anchoredPosition = Vector2.zero;

        endGameCard = card.GetComponent<Image>();
        endGameCard.color = UIColorPalette.Card;
        endGameCard.raycastTarget = false;
    }
}
