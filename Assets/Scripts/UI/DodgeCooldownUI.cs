using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dash bar'inin altinda dodge cooldown gosterir (Q / LB).
public class DodgeCooldownUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerDodge playerDodge;

    [Header("UI")]
    public RectTransform widgetRoot;
    public Image barBackground;
    public Image barTrack;
    public Image barFill;
    public Image barGlow;
    public TMP_Text labelText;
    public TMP_Text keyHintText;

    [Header("Style")]
    public Color backgroundColor = new Color(0.04f, 0.06f, 0.1f, 0.72f);
    public Color trackColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
    public Color fillCooldownColor = new Color(0.95f, 0.55f, 0.2f, 0.95f);
    public Color fillReadyColor = new Color(1f, 0.78f, 0.35f, 1f);
    public Color fillActiveColor = new Color(1f, 1f, 0.85f, 1f);
    public Color glowReadyColor = new Color(1f, 0.7f, 0.25f, 0.4f);
    public Color labelReadyColor = new Color(1f, 0.9f, 0.7f, 1f);
    public Color labelCooldownColor = new Color(0.55f, 0.62f, 0.7f, 0.9f);

    [Header("Layout")]
    public Vector2 barSize = new Vector2(260f, 12f);
    public float barBottomOffset = 52f;
    public float fillInset = 2f;

    [Header("Animation")]
    public float fillSmoothSpeed = 16f;
    public float readyPulseSpeed = 4.5f;
    public float readyFlashDuration = 0.28f;

    float displayedFill = 1f;
    bool wasReady;
    float readyFlashTimer;
    static Sprite whiteSprite;

    void Awake()
    {
        EnsureWidgetRoot();
        BuildBar();
    }

    void Start()
    {
        ResolveActiveDodge();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            SetVisible(false);
            return;
        }

        ResolveActiveDodge();

        if (playerDodge == null || !playerDodge.enabled)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        float targetFill = playerDodge.GetCooldownPercent();
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, fillSmoothSpeed * Time.deltaTime);

        bool ready = playerDodge.IsReady();
        bool active = playerDodge.IsDodgeActive;

        if (ready && !wasReady)
        {
            readyFlashTimer = readyFlashDuration;
        }

        readyFlashTimer = Mathf.Max(0f, readyFlashTimer - Time.deltaTime);
        wasReady = ready;

        UpdateFill(ready, active);
        UpdateGlow(ready, active);
        UpdateLabels(ready, active);
    }

    void EnsureWidgetRoot()
    {
        if (widgetRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("DodgeBar_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(transform, false);

        widgetRoot = root.GetComponent<RectTransform>();
        widgetRoot.anchorMin = new Vector2(0.5f, 0f);
        widgetRoot.anchorMax = new Vector2(0.5f, 0f);
        widgetRoot.pivot = new Vector2(0.5f, 0.5f);

        barBackground = root.GetComponent<Image>();
    }

    void BuildBar()
    {
        if (widgetRoot == null)
        {
            return;
        }

        ClearRuntimeChildren();

        widgetRoot.sizeDelta = barSize;
        widgetRoot.anchoredPosition = new Vector2(0f, barBottomOffset);

        if (barBackground != null)
        {
            barBackground.sprite = GetWhiteSprite();
            barBackground.type = Image.Type.Simple;
            barBackground.color = backgroundColor;
            barBackground.raycastTarget = false;
        }

        EnsureTrack();
        EnsureFill();
        EnsureGlow();
        EnsureLabel();
        EnsureKeyHint();
    }

    void ClearRuntimeChildren()
    {
        for (int i = widgetRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(widgetRoot.GetChild(i).gameObject);
        }

        if (keyHintText != null)
        {
            Destroy(keyHintText.gameObject);
            keyHintText = null;
        }

        if (labelText != null)
        {
            Destroy(labelText.gameObject);
            labelText = null;
        }

        barTrack = null;
        barFill = null;
        barGlow = null;
    }

    void EnsureTrack()
    {
        barTrack = CreateImage("DodgeBar_Track", widgetRoot);
        RectTransform trackRect = barTrack.rectTransform;
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = new Vector2(fillInset, fillInset);
        trackRect.offsetMax = new Vector2(-fillInset, -fillInset);
        barTrack.sprite = GetWhiteSprite();
        barTrack.color = trackColor;
        barTrack.raycastTarget = false;
    }

    void EnsureFill()
    {
        barFill = CreateImage("DodgeBar_Fill", widgetRoot);
        RectTransform fillRect = barFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(fillInset, fillInset);
        fillRect.offsetMax = new Vector2(-fillInset, -fillInset);

        barFill.sprite = GetWhiteSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 1f;
        barFill.color = fillReadyColor;
        barFill.raycastTarget = false;
    }

    void EnsureGlow()
    {
        barGlow = CreateImage("DodgeBar_Glow", widgetRoot);
        RectTransform glowRect = barGlow.rectTransform;
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-8f, -6f);
        glowRect.offsetMax = new Vector2(8f, 6f);
        barGlow.sprite = GetWhiteSprite();
        barGlow.color = new Color(glowReadyColor.r, glowReadyColor.g, glowReadyColor.b, 0f);
        barGlow.raycastTarget = false;
        barGlow.transform.SetAsFirstSibling();
    }

    void EnsureLabel()
    {
        GameObject labelObject = new GameObject("DodgeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(widgetRoot.parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(-78f, barBottomOffset + 18f);
        labelRect.sizeDelta = new Vector2(90f, 18f);

        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "DODGE";
        labelText.fontSize = 13f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = labelCooldownColor;
        labelText.raycastTarget = false;
    }

    void EnsureKeyHint()
    {
        GameObject hintObject = new GameObject("DodgeKeyHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(widgetRoot.parent, false);

        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(78f, barBottomOffset + 18f);
        hintRect.sizeDelta = new Vector2(90f, 18f);

        keyHintText = hintObject.GetComponent<TextMeshProUGUI>();
        keyHintText.text = "Q";
        keyHintText.fontSize = 12f;
        keyHintText.fontStyle = FontStyles.Bold;
        keyHintText.alignment = TextAlignmentOptions.Right;
        keyHintText.color = labelCooldownColor;
        keyHintText.raycastTarget = false;
    }

    void UpdateFill(bool ready, bool active)
    {
        if (barFill == null)
        {
            return;
        }

        barFill.fillAmount = displayedFill;

        if (active)
        {
            barFill.color = fillActiveColor;
            return;
        }

        barFill.color = ready ? fillReadyColor : Color.Lerp(fillCooldownColor, fillReadyColor, displayedFill);

        if (readyFlashTimer > 0f)
        {
            float flash = readyFlashTimer / readyFlashDuration;
            barFill.color = Color.Lerp(barFill.color, Color.white, flash * 0.7f);
        }
    }

    void UpdateGlow(bool ready, bool active)
    {
        if (barGlow == null)
        {
            return;
        }

        if (!ready || active)
        {
            Color c = barGlow.color;
            c.a = Mathf.MoveTowards(c.a, 0f, 4f * Time.deltaTime);
            barGlow.color = c;
            return;
        }

        float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.time * readyPulseSpeed));
        float flash = readyFlashTimer > 0f ? readyFlashTimer / readyFlashDuration : 0f;
        float alpha = glowReadyColor.a * pulse + flash * 0.35f;

        barGlow.color = new Color(glowReadyColor.r, glowReadyColor.g, glowReadyColor.b, alpha);
    }

    void UpdateLabels(bool ready, bool active)
    {
        Color target = ready && !active ? labelReadyColor : labelCooldownColor;

        if (labelText != null)
        {
            labelText.color = Color.Lerp(labelText.color, target, 10f * Time.deltaTime);
        }

        if (keyHintText != null)
        {
            keyHintText.color = Color.Lerp(keyHintText.color, target, 10f * Time.deltaTime);
        }
    }

    void ResolveActiveDodge()
    {
        if (playerDodge != null && playerDodge.enabled)
        {
            return;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Runner && player.roleType != RoleType.Saver)
            {
                continue;
            }

            PlayerDodge dodge = player.GetComponent<PlayerDodge>();

            if (dodge != null && dodge.enabled)
            {
                playerDodge = dodge;
                displayedFill = dodge.GetCooldownPercent();
                return;
            }
        }
    }

    void SetVisible(bool visible)
    {
        if (widgetRoot != null)
        {
            widgetRoot.gameObject.SetActive(visible);
        }

        if (labelText != null)
        {
            labelText.gameObject.SetActive(visible);
        }

        if (keyHintText != null)
        {
            keyHintText.gameObject.SetActive(visible);
        }
    }

    static Image CreateImage(string name, Transform parent)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}
