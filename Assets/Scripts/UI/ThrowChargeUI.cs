using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Atis sarj bari: Dash bar ile ayni gorsel dil.
public class ThrowChargeUI : MonoBehaviour
{
    public PlayerThrow playerThrow;
    public Image chargeFillImage;
    public GameObject chargeBarRoot;

    [Header("Runtime Style")]
    public Vector2 barSize = new Vector2(240f, 14f);
    public float barYOffset = -72f;

    Image barBackground;
    Image barTrack;
    Image barFill;
    TMP_Text labelText;
    float displayedFill;
    float fullChargeFlash;
    bool wasNearFull;
    static Sprite whiteSprite;
    bool built;

    void Awake()
    {
        ResolvePlayerThrow();
        BuildBar();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            SetVisible(false);
            return;
        }

        ResolvePlayerThrow();

        if (playerThrow == null || !playerThrow.enabled || !playerThrow.IsCharging())
        {
            SetVisible(false);
            displayedFill = 0f;
            wasNearFull = false;
            fullChargeFlash = 0f;
            return;
        }

        SetVisible(true);

        float target = playerThrow.GetChargePercent();
        displayedFill = Mathf.MoveTowards(displayedFill, target, 18f * Time.deltaTime);

        if (target >= 0.95f && !wasNearFull)
        {
            wasNearFull = true;
            fullChargeFlash = 1f;
        }
        else if (target < 0.9f)
        {
            wasNearFull = false;
        }

        fullChargeFlash = Mathf.MoveTowards(fullChargeFlash, 0f, 3.5f * Time.deltaTime);

        if (barFill != null)
        {
            barFill.fillAmount = displayedFill;
            Color fill = Color.Lerp(UIColorPalette.ChargeFill, UIColorPalette.ChargeFillHot, displayedFill);
            if (fullChargeFlash > 0.01f)
            {
                fill = Color.Lerp(fill, Color.white, fullChargeFlash * 0.55f);
            }

            barFill.color = fill;
        }

        if (chargeBarRoot != null)
        {
            float punch = 1f + fullChargeFlash * 0.08f;
            chargeBarRoot.transform.localScale = new Vector3(punch, punch, 1f);
        }
    }

    void ResolvePlayerThrow()
    {
        if (playerThrow != null && playerThrow.enabled)
        {
            return;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Thrower) continue;

            PlayerThrow throwScript = player.GetComponent<PlayerThrow>();
            if (throwScript != null && throwScript.enabled)
            {
                playerThrow = throwScript;
                return;
            }
        }
    }

    void BuildBar()
    {
        if (built) return;
        built = true;

        RectTransform root;
        if (chargeBarRoot != null)
        {
            root = chargeBarRoot.GetComponent<RectTransform>();
            barBackground = chargeBarRoot.GetComponent<Image>();
        }
        else
        {
            GameObject rootObject = new GameObject("ChargeBar_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootObject.transform.SetParent(transform, false);
            root = rootObject.GetComponent<RectTransform>();
            barBackground = rootObject.GetComponent<Image>();
            chargeBarRoot = rootObject;
        }

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = barSize;
        root.anchoredPosition = new Vector2(0f, barYOffset);

        ClearChildren(root);

        if (barBackground != null)
        {
            barBackground.sprite = GetWhiteSprite();
            barBackground.color = UIColorPalette.BarBackground;
            barBackground.raycastTarget = false;
        }

        barTrack = CreateImage("ChargeBar_Track", root);
        StretchInset(barTrack.rectTransform, 2f);
        barTrack.sprite = GetWhiteSprite();
        barTrack.color = UIColorPalette.BarTrack;
        barTrack.raycastTarget = false;

        if (chargeFillImage != null)
        {
            barFill = chargeFillImage;
            barFill.rectTransform.SetParent(root, false);
        }
        else
        {
            barFill = CreateImage("ChargeBar_Fill", root);
        }

        StretchInset(barFill.rectTransform, 2f);
        barFill.sprite = GetWhiteSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 0f;
        barFill.color = UIColorPalette.ChargeFill;
        barFill.raycastTarget = false;
        chargeFillImage = barFill;

        GameObject labelObject = new GameObject("ChargeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.parent, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, barYOffset + 18f);
        labelRect.sizeDelta = new Vector2(160f, 20f);

        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "CHARGE";
        labelText.fontSize = 13f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = UIColorPalette.Muted;
        labelText.outlineWidth = 0.2f;
        labelText.outlineColor = UIColorPalette.Outline;
        labelText.raycastTarget = false;

        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (chargeBarRoot != null)
        {
            chargeBarRoot.SetActive(visible);
        }

        if (labelText != null)
        {
            labelText.gameObject.SetActive(visible);
        }
    }

    static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }

    static void StretchInset(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    static Image CreateImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D texture = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}
