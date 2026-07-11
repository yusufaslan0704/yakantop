using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Revive ilerleme bari: Dash/Charge ile ayni gorsel dil.
public class ReviveProgressUI : MonoBehaviour
{
    public PlayerRevive playerRevive;
    public Image reviveFillImage;
    public GameObject reviveBarRoot;

    [Header("Runtime Style")]
    public Vector2 barSize = new Vector2(240f, 14f);
    public float barBottomOffset = 118f;

    Image barBackground;
    Image barTrack;
    Image barFill;
    TMP_Text labelText;
    float displayedFill;
    static Sprite whiteSprite;
    bool built;

    void Awake()
    {
        ResolvePlayerRevive();
        BuildBar();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            SetVisible(false);
            return;
        }

        ResolvePlayerRevive();

        if (playerRevive == null || !playerRevive.enabled || !playerRevive.IsReviving())
        {
            SetVisible(false);
            displayedFill = 0f;
            return;
        }

        SetVisible(true);

        float target = playerRevive.GetReviveProgressPercent();
        displayedFill = Mathf.MoveTowards(displayedFill, target, 14f * Time.deltaTime);

        if (barFill != null)
        {
            barFill.fillAmount = displayedFill;
            barFill.color = Color.Lerp(UIColorPalette.ReviveFill, Color.white, displayedFill * 0.35f);
        }
    }

    void ResolvePlayerRevive()
    {
        if (playerRevive != null && playerRevive.enabled)
        {
            return;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != RoleType.Saver) continue;

            PlayerRevive revive = player.GetComponent<PlayerRevive>();
            if (revive != null)
            {
                playerRevive = revive;
                return;
            }
        }
    }

    void BuildBar()
    {
        if (built) return;
        built = true;

        RectTransform root;
        if (reviveBarRoot != null)
        {
            root = reviveBarRoot.GetComponent<RectTransform>();
            barBackground = reviveBarRoot.GetComponent<Image>();
        }
        else
        {
            GameObject rootObject = new GameObject("ReviveBar_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootObject.transform.SetParent(transform, false);
            root = rootObject.GetComponent<RectTransform>();
            barBackground = rootObject.GetComponent<Image>();
            reviveBarRoot = rootObject;
        }

        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = barSize;
        root.anchoredPosition = new Vector2(0f, barBottomOffset);

        ClearChildren(root);

        if (barBackground != null)
        {
            barBackground.sprite = GetWhiteSprite();
            barBackground.color = UIColorPalette.BarBackground;
            barBackground.raycastTarget = false;
        }

        barTrack = CreateImage("ReviveBar_Track", root);
        StretchInset(barTrack.rectTransform, 2f);
        barTrack.sprite = GetWhiteSprite();
        barTrack.color = UIColorPalette.BarTrack;
        barTrack.raycastTarget = false;

        if (reviveFillImage != null)
        {
            barFill = reviveFillImage;
            barFill.rectTransform.SetParent(root, false);
        }
        else
        {
            barFill = CreateImage("ReviveBar_Fill", root);
        }

        StretchInset(barFill.rectTransform, 2f);
        barFill.sprite = GetWhiteSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 0f;
        barFill.color = UIColorPalette.ReviveFill;
        barFill.raycastTarget = false;
        reviveFillImage = barFill;

        GameObject labelObject = new GameObject("ReviveLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.parent, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, barBottomOffset + 18f);
        labelRect.sizeDelta = new Vector2(160f, 20f);

        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "REVIVE";
        labelText.fontSize = 13f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = UIColorPalette.Accent;
        labelText.outlineWidth = 0.2f;
        labelText.outlineColor = UIColorPalette.Outline;
        labelText.raycastTarget = false;

        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (reviveBarRoot != null)
        {
            reviveBarRoot.SetActive(visible);
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
