using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerDash playerDash;

    [Header("UI")]
    public RectTransform widgetRoot;
    public Image barBackground;
    public Image barTrack;
    public Image barFill;
    public Image barGlow;
    public Image tipHighlight;
    public TMP_Text labelText;
    public TMP_Text keyHintText;

    [Header("Legacy")]
    public Image dashFillImage;

    [Header("Style")]
    public Color backgroundColor = new Color(0.04f, 0.06f, 0.1f, 0.72f);
    public Color trackColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
    public Color fillCooldownColor = new Color(0.25f, 0.62f, 0.95f, 0.95f);
    public Color fillReadyColor = new Color(0.45f, 0.95f, 1f, 1f);
    public Color fillDashingColor = new Color(1f, 1f, 1f, 1f);
    public Color glowReadyColor = new Color(0.35f, 0.9f, 1f, 0.45f);
    public Color labelReadyColor = new Color(0.8f, 0.98f, 1f, 1f);
    public Color labelCooldownColor = new Color(0.55f, 0.62f, 0.7f, 0.9f);

    [Header("Layout")]
    public Vector2 barSize = new Vector2(260f, 14f);
    public float barBottomOffset = 86f;
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
        BindReferences();
        BuildBar();
    }

    void Start()
    {
        ResolveActiveDash();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            SetVisible(false);
            return;
        }

        ResolveActiveDash();

        if (playerDash == null || !playerDash.enabled)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        float targetFill = playerDash.GetDashCooldownPercent();
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, fillSmoothSpeed * Time.deltaTime);

        bool ready = playerDash.IsDashReady();
        bool dashing = playerDash.IsDashing();

        if (ready && !wasReady)
        {
            readyFlashTimer = readyFlashDuration;
        }

        readyFlashTimer = Mathf.Max(0f, readyFlashTimer - Time.deltaTime);
        wasReady = ready;

        UpdateFill(ready, dashing);
        UpdateGlow(ready, dashing);
        UpdateTip(ready, dashing);
        UpdateLabels(ready, dashing);
    }

    void BindReferences()
    {
        if (barFill == null && dashFillImage != null)
        {
            barFill = dashFillImage;
        }

        if (widgetRoot == null && barFill != null)
        {
            widgetRoot = barFill.transform.parent as RectTransform;
        }

        if (barBackground == null && widgetRoot != null)
        {
            barBackground = widgetRoot.GetComponent<Image>();
        }
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
        EnsureTip();
        EnsureLabel();
        EnsureKeyHint();
    }

    void ClearRuntimeChildren()
    {
        for (int i = widgetRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = widgetRoot.GetChild(i);

            if (barFill != null && child == barFill.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
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
        barGlow = null;
        tipHighlight = null;
    }

    void EnsureTrack()
    {
        barTrack = CreateImage("DashBar_Track", widgetRoot);
        RectTransform trackRect = barTrack.rectTransform;
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = new Vector2(fillInset, fillInset);
        trackRect.offsetMax = new Vector2(-fillInset, -fillInset);
        barTrack.sprite = GetWhiteSprite();
        barTrack.color = trackColor;
        barTrack.raycastTarget = false;
        barTrack.transform.SetAsFirstSibling();
    }

    void EnsureFill()
    {
        if (barFill == null)
        {
            barFill = CreateImage("DashBar_Fill", widgetRoot);
        }

        RectTransform fillRect = barFill.rectTransform;
        fillRect.SetParent(widgetRoot, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(fillInset, fillInset);
        fillRect.offsetMax = new Vector2(-fillInset, -fillInset);
        fillRect.SetAsLastSibling();

        barFill.sprite = GetWhiteSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 1f;
        barFill.color = fillReadyColor;
        barFill.raycastTarget = false;
        barFill.preserveAspect = false;
    }

    void EnsureGlow()
    {
        barGlow = CreateImage("DashBar_Glow", widgetRoot);
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

    void EnsureTip()
    {
        tipHighlight = CreateImage("DashBar_Tip", widgetRoot);
        RectTransform tipRect = tipHighlight.rectTransform;
        tipRect.anchorMin = new Vector2(0f, 0f);
        tipRect.anchorMax = new Vector2(0f, 1f);
        tipRect.pivot = new Vector2(1f, 0.5f);
        tipRect.sizeDelta = new Vector2(4f, -fillInset * 2f);
        tipRect.anchoredPosition = new Vector2(fillInset, 0f);
        tipHighlight.sprite = GetWhiteSprite();
        tipHighlight.color = new Color(1f, 1f, 1f, 0.85f);
        tipHighlight.raycastTarget = false;
    }

    void EnsureLabel()
    {
        GameObject labelObject = new GameObject("DashLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(widgetRoot.parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(-78f, barBottomOffset + 22f);
        labelRect.sizeDelta = new Vector2(90f, 20f);

        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "DASH";
        labelText.fontSize = 14f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = labelCooldownColor;
        labelText.raycastTarget = false;
    }

    void EnsureKeyHint()
    {
        GameObject hintObject = new GameObject("DashKeyHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(widgetRoot.parent, false);

        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(78f, barBottomOffset + 22f);
        hintRect.sizeDelta = new Vector2(90f, 20f);

        keyHintText = hintObject.GetComponent<TextMeshProUGUI>();
        keyHintText.text = "SHIFT";
        keyHintText.fontSize = 13f;
        keyHintText.fontStyle = FontStyles.Bold;
        keyHintText.alignment = TextAlignmentOptions.Right;
        keyHintText.color = labelCooldownColor;
        keyHintText.raycastTarget = false;
    }

    void UpdateFill(bool ready, bool dashing)
    {
        if (barFill == null)
        {
            return;
        }

        barFill.fillAmount = displayedFill;

        if (dashing)
        {
            barFill.color = fillDashingColor;
            return;
        }

        Color target = ready ? fillReadyColor : Color.Lerp(fillCooldownColor, fillReadyColor, displayedFill);
        barFill.color = target;

        if (readyFlashTimer > 0f)
        {
            float flash = readyFlashTimer / readyFlashDuration;
            barFill.color = Color.Lerp(barFill.color, Color.white, flash * 0.7f);
        }
    }

    void UpdateGlow(bool ready, bool dashing)
    {
        if (barGlow == null)
        {
            return;
        }

        if (!ready || dashing)
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

    void UpdateTip(bool ready, bool dashing)
    {
        if (tipHighlight == null || barFill == null)
        {
            return;
        }

        float usableWidth = barSize.x - fillInset * 2f;
        float x = fillInset + usableWidth * displayedFill;

        tipHighlight.rectTransform.anchoredPosition = new Vector2(x, 0f);
        tipHighlight.enabled = displayedFill > 0.02f && displayedFill < 0.995f;

        if (dashing)
        {
            tipHighlight.color = Color.white;
        }
        else if (ready)
        {
            tipHighlight.color = new Color(1f, 1f, 1f, 0.15f);
        }
        else
        {
            tipHighlight.color = new Color(0.85f, 0.95f, 1f, 0.9f);
        }
    }

    void UpdateLabels(bool ready, bool dashing)
    {
        Color target = ready && !dashing ? labelReadyColor : labelCooldownColor;

        if (labelText != null)
        {
            labelText.color = Color.Lerp(labelText.color, target, 10f * Time.deltaTime);
        }

        if (keyHintText != null)
        {
            keyHintText.color = Color.Lerp(keyHintText.color, target, 10f * Time.deltaTime);
        }
    }

    void ResolveActiveDash()
    {
        if (playerDash != null && playerDash.enabled)
        {
            return;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Runner && player.roleType != RoleType.Saver)
            {
                continue;
            }

            PlayerDash dash = player.GetComponent<PlayerDash>();

            if (dash != null && dash.enabled)
            {
                playerDash = dash;
                displayedFill = dash.GetDashCooldownPercent();
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
