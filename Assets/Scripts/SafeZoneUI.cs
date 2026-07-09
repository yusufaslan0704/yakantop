using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cep (SafeZone) koruma butcesini gosterir.
// Sadece aktif kosucu/saver cepteyken veya butce dolmuyken gorunur.
public class SafeZoneUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerHealth trackedPlayer;

    [Header("UI")]
    public RectTransform widgetRoot;
    public Image barBackground;
    public Image barTrack;
    public Image barFill;
    public TMP_Text labelText;
    public TMP_Text valueText;

    [Header("Style")]
    public Color backgroundColor = new Color(0.04f, 0.08f, 0.12f, 0.75f);
    public Color trackColor = new Color(0.1f, 0.18f, 0.24f, 0.95f);
    public Color fillProtectedColor = new Color(0.25f, 0.75f, 1f, 0.95f);
    public Color fillLowColor = new Color(0.95f, 0.45f, 0.25f, 0.95f);
    public Color fillRechargeColor = new Color(0.35f, 0.85f, 0.55f, 0.9f);
    public Color labelColor = new Color(0.75f, 0.92f, 1f, 1f);

    [Header("Layout")]
    public Vector2 barSize = new Vector2(220f, 12f);
    public float topOffset = 96f;
    public float fillInset = 2f;

    [Header("Animation")]
    public float fillSmoothSpeed = 14f;

    float displayedFill = 1f;
    static Sprite whiteSprite;

    void Awake()
    {
        EnsureWidgetRoot();
        BuildBar();
        SetVisible(false);
    }

    void Start()
    {
        ResolveTrackedPlayer();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            SetVisible(false);
            return;
        }

        ResolveTrackedPlayer();

        if (trackedPlayer == null || trackedPlayer.IsEliminated)
        {
            SetVisible(false);
            return;
        }

        bool inside = SafeZone.IsInsideAny(trackedPlayer);
        float remaining = SafeZone.GetProtectionRemaining(trackedPlayer);
        float percent = SafeZone.GetProtectionPercent(trackedPlayer);
        bool show = inside || percent < 0.999f;

        if (!show)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        displayedFill = Mathf.MoveTowards(displayedFill, percent, fillSmoothSpeed * Time.deltaTime);

        if (barFill != null)
        {
            barFill.fillAmount = displayedFill;

            if (inside)
            {
                barFill.color = Color.Lerp(fillLowColor, fillProtectedColor, displayedFill);
            }
            else
            {
                barFill.color = fillRechargeColor;
            }
        }

        if (labelText != null)
        {
            labelText.text = inside ? "SAFE" : "RECHARGE";
        }

        if (valueText != null)
        {
            valueText.text = remaining.ToString("0.0") + "s";
        }
    }

    void EnsureWidgetRoot()
    {
        if (widgetRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("SafeZoneBar_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(transform, false);

        widgetRoot = root.GetComponent<RectTransform>();
        widgetRoot.anchorMin = new Vector2(0.5f, 1f);
        widgetRoot.anchorMax = new Vector2(0.5f, 1f);
        widgetRoot.pivot = new Vector2(0.5f, 0.5f);

        barBackground = root.GetComponent<Image>();
    }

    void BuildBar()
    {
        if (widgetRoot == null)
        {
            return;
        }

        for (int i = widgetRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(widgetRoot.GetChild(i).gameObject);
        }

        if (labelText != null)
        {
            Destroy(labelText.gameObject);
            labelText = null;
        }

        if (valueText != null)
        {
            Destroy(valueText.gameObject);
            valueText = null;
        }

        widgetRoot.sizeDelta = barSize;
        widgetRoot.anchoredPosition = new Vector2(0f, -topOffset);

        if (barBackground != null)
        {
            barBackground.sprite = GetWhiteSprite();
            barBackground.color = backgroundColor;
            barBackground.raycastTarget = false;
        }

        barTrack = CreateImage("SafeZoneBar_Track", widgetRoot);
        RectTransform trackRect = barTrack.rectTransform;
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = new Vector2(fillInset, fillInset);
        trackRect.offsetMax = new Vector2(-fillInset, -fillInset);
        barTrack.sprite = GetWhiteSprite();
        barTrack.color = trackColor;
        barTrack.raycastTarget = false;

        barFill = CreateImage("SafeZoneBar_Fill", widgetRoot);
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
        barFill.color = fillProtectedColor;
        barFill.raycastTarget = false;

        GameObject labelObject = new GameObject("SafeZoneLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(widgetRoot.parent, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(-70f, -topOffset + 16f);
        labelRect.sizeDelta = new Vector2(100f, 18f);
        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "SAFE";
        labelText.fontSize = 13f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = labelColor;
        labelText.raycastTarget = false;

        GameObject valueObject = new GameObject("SafeZoneValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(widgetRoot.parent, false);
        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.5f, 1f);
        valueRect.anchorMax = new Vector2(0.5f, 1f);
        valueRect.pivot = new Vector2(0.5f, 0.5f);
        valueRect.anchoredPosition = new Vector2(70f, -topOffset + 16f);
        valueRect.sizeDelta = new Vector2(80f, 18f);
        valueText = valueObject.GetComponent<TextMeshProUGUI>();
        valueText.text = "3.0s";
        valueText.fontSize = 12f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.color = labelColor;
        valueText.raycastTarget = false;
    }

    void ResolveTrackedPlayer()
    {
        if (trackedPlayer != null && !trackedPlayer.IsEliminated)
        {
            PlayerRole role = trackedPlayer.GetComponent<PlayerRole>();
            if (role != null && (role.roleType == RoleType.Runner || role.roleType == RoleType.Saver))
            {
                PlayerInputHandler input = trackedPlayer.GetComponent<PlayerInputHandler>();
                if (input != null && input.enabled)
                {
                    return;
                }
            }
        }

        trackedPlayer = null;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Runner && player.roleType != RoleType.Saver)
            {
                continue;
            }

            if (player.Health == null || player.Health.IsEliminated)
            {
                continue;
            }

            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.enabled)
            {
                trackedPlayer = player.Health;
                displayedFill = SafeZone.GetProtectionPercent(trackedPlayer);
                return;
            }

            if (trackedPlayer == null)
            {
                trackedPlayer = player.Health;
            }
        }

        if (trackedPlayer != null)
        {
            displayedFill = SafeZone.GetProtectionPercent(trackedPlayer);
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

        if (valueText != null)
        {
            valueText.gameObject.SetActive(visible);
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
